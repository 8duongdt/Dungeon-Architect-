using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lớp thực thi kỹ năng dùng chung, tách khỏi chính sách cast (cooldown/gate).
/// Caster chỉ cần dựng <see cref="SkillCastContext"/> rồi gọi Execute -
/// mỗi <see cref="SkillMechanic"/> có một nhánh thực thi riêng.
/// </summary>
public static class SkillExecutor
{
    public static void Execute(SkillDefinitionSO skill, in SkillCastContext context)
    {
        switch (skill.Mechanic)
        {
            case SkillMechanic.Projectile:
                ExecuteProjectile(skill, context);
                break;
            case SkillMechanic.ChainStrike:
                ExecuteChainStrike(skill, context);
                break;
            case SkillMechanic.AreaStrike:
                ExecuteAreaStrike(skill, context);
                break;
            case SkillMechanic.DamageZone:
                SpawnZoneObject<SkillDamageZone>(skill.ZonePrefab, skill, context);
                break;
            case SkillMechanic.Trap:
                SpawnZoneObject<SkillTrap>(skill.TrapPrefab, skill, context);
                break;
            case SkillMechanic.PullVortex:
                SpawnZoneObject<SkillBlackHole>(skill.VortexPrefab, skill, context);
                break;
            case SkillMechanic.SelfShield:
                ExecuteSelfShield(skill, context);
                break;
            case SkillMechanic.ExecuteStrike:
                ExecuteExecuteStrike(skill, context);
                break;
            default:
                ExecuteInstantStrike(skill, context);
                break;
        }
    }

    /// <summary>Đánh tức thời: VFX + sát thương ngay trên mục tiêu (hành vi gốc).</summary>
    private static void ExecuteInstantStrike(SkillDefinitionSO skill, in SkillCastContext context)
    {
        SpawnVfx(skill.VfxPrefab, context.TargetPoint, ResolveSpawnRotation(skill, context));

        // Cast tự do không có mục tiêu (nhánh fallback prefab-null) -> chỉ có VFX, không sát thương.
        if (context.PrimaryTarget == null)
        {
            return;
        }

        ApplyDamage(context.PrimaryTarget, context.Damage, skill.ImpactDelay, context.CoroutineHost);
    }

    /// <summary>Bắn đạn bay thẳng về phía mục tiêu; đạn tự xử lý va chạm và phát nổ.</summary>
    private static void ExecuteProjectile(SkillDefinitionSO skill, in SkillCastContext context)
    {
        if (skill.ProjectilePrefab == null)
        {
            ExecuteInstantStrike(skill, context);
            return;
        }

        GameObject projectileObject = Object.Instantiate(
            skill.ProjectilePrefab, context.CasterTransform.position, Quaternion.identity);
        var projectile = projectileObject.GetComponent<SkillProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(context, skill);
        }
    }

    /// <summary>Đánh mục tiêu chính rồi nảy tức thời sang các kẻ địch gần (cùng frame).</summary>
    private static void ExecuteChainStrike(SkillDefinitionSO skill, in SkillCastContext context)
    {
        ExecuteInstantStrike(skill, context);

        float bounceDamage = context.Damage * skill.ChainDamageFactor;
        Vector3 primaryPosition = context.PrimaryTarget.transform.position;
        foreach (UnitHealth bounceTarget in FindChainTargets(
            primaryPosition, context.PrimaryTarget, context.CasterFaction, skill.ChainRadius, skill.MaxChainTargets))
        {
            SpawnVfx(skill.VfxPrefab, bounceTarget.transform.position);
            ApplyDamage(bounceTarget, bounceDamage, skill.ImpactDelay, context.CoroutineHost);
        }
    }

    /// <summary>
    /// Nổ diện rộng quanh vị trí mục tiêu (chốt lúc cast): sát thương mọi kẻ địch
    /// trong bán kính + đẩy lùi khỏi tâm. impactDelay > 0 = nổ trễ tại đúng điểm đã
    /// chọn (SunStrike - kẻ địch kịp chạy thoát nếu di chuyển).
    /// </summary>
    private static void ExecuteAreaStrike(SkillDefinitionSO skill, in SkillCastContext context)
    {
        Vector3 impactCenter = context.TargetPoint;
        bool canDelay = skill.ImpactDelay > 0f && CanHostCoroutine(context.CoroutineHost);
        if (canDelay)
        {
            context.CoroutineHost.StartCoroutine(
                DetonateAfterDelay(skill, impactCenter, context.Damage, context.CasterFaction));
            return;
        }

        Detonate(skill, impactCenter, context.Damage, context.CasterFaction);
    }

    private static IEnumerator DetonateAfterDelay(SkillDefinitionSO skill, Vector3 center, float damage, UnitFaction casterFaction)
    {
        yield return new WaitForSeconds(skill.ImpactDelay);
        Detonate(skill, center, damage, casterFaction);
    }

    private static void Detonate(SkillDefinitionSO skill, Vector3 center, float damage, UnitFaction casterFaction)
    {
        SpawnVfx(skill.VfxPrefab, center);
        foreach (UnitHealth victim in SkillTargeting.FindAttackableInRadius(center, skill.AreaRadius, casterFaction))
        {
            victim.TakeDamage(damage);
            Vector2 awayFromCenter = victim.transform.position - center;
            UnitKnockback.Push(victim.gameObject, awayFromCenter, skill.KnockbackDistance);
        }
    }

    /// <summary>Đặt vùng/bẫy/xoáy tại vị trí mục tiêu; component trên prefab tự vận hành.</summary>
    private static void SpawnZoneObject<TComponent>(GameObject prefab, SkillDefinitionSO skill, in SkillCastContext context)
        where TComponent : MonoBehaviour
    {
        if (prefab == null)
        {
            ExecuteInstantStrike(skill, context);
            return;
        }

        Vector3 position = context.TargetPoint;
        GameObject zoneObject = Object.Instantiate(prefab, position, ResolveSpawnRotation(skill, context));
        var zone = zoneObject.GetComponent<TComponent>();
        if (zone is SkillDamageZone damageZone) damageZone.Initialize(context, skill);
        else if (zone is SkillTrap trap) trap.Initialize(context, skill);
        else if (zone is SkillBlackHole blackHole) blackHole.Initialize(context, skill);
    }

    /// <summary>Khiên máu ảo bao bọc CHÍNH người thi triển (không cần mục tiêu địch).</summary>
    private static void ExecuteSelfShield(SkillDefinitionSO skill, in SkillCastContext context)
    {
        var casterHealth = context.CasterTransform.GetComponent<UnitHealth>();
        if (casterHealth == null)
        {
            return;
        }

        casterHealth.AddShield(skill.ShieldAmount, skill.ShieldDuration);
        SpawnVfx(skill.VfxPrefab, context.CasterTransform.position);
    }

    /// <summary>
    /// Đòn kết liễu: mục tiêu dưới ngưỡng máu bị hạ gục ngay + thưởng vàng (chỉ phe Player);
    /// trên ngưỡng thì chỉ nhận sát thương thường.
    /// </summary>
    private static void ExecuteExecuteStrike(SkillDefinitionSO skill, in SkillCastContext context)
    {
        UnitHealth target = context.PrimaryTarget;
        SpawnVfx(skill.VfxPrefab, target.transform.position);

        bool isBelowExecuteThreshold = target.CurrentHealth <= target.MaxHealth * skill.ExecuteHealthThreshold;
        if (!isBelowExecuteThreshold)
        {
            ApplyDamage(target, context.Damage, skill.ImpactDelay, context.CoroutineHost);
            return;
        }

        // Vượt qua mọi giáp/khiên để chắc chắn kết liễu.
        target.TakeDamage(target.CurrentHealth + target.Defense + target.CurrentShield);
        RewardExecuteGold(skill, context.CasterFaction);
    }

    private static void RewardExecuteGold(SkillDefinitionSO skill, UnitFaction casterFaction)
    {
        bool isPlayerCaster = casterFaction != null && casterFaction.Faction == FactionType.Player;
        if (isPlayerCaster && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddGold(skill.ExecuteGoldReward);
        }
    }

    private static List<UnitHealth> FindChainTargets(Vector3 origin, UnitHealth primaryTarget,
        UnitFaction casterFaction, float radius, int maxTargets)
    {
        return SkillTargeting.FindAttackableInRadius(origin, radius, casterFaction)
            .Where(target => target != primaryTarget)
            .OrderBy(target => ((Vector2)target.transform.position - (Vector2)origin).sqrMagnitude)
            .Take(maxTargets)
            .ToList();
    }

    private static void ApplyDamage(UnitHealth target, float damage, float impactDelay, MonoBehaviour host)
    {
        bool canDelay = impactDelay > 0f && CanHostCoroutine(host);
        if (canDelay)
        {
            host.StartCoroutine(ApplyDamageAfterDelay(target, damage, impactDelay));
            return;
        }

        target.TakeDamage(damage);
    }

    private static bool CanHostCoroutine(MonoBehaviour host)
    {
        return host != null && host.isActiveAndEnabled;
    }

    private static IEnumerator ApplyDamageAfterDelay(UnitHealth target, float damage, float delay)
    {
        yield return new WaitForSeconds(delay);

        bool targetStillAlive = target != null && !target.IsDead;
        if (targetStillAlive)
        {
            target.TakeDamage(damage);
        }
    }

    private static void SpawnVfx(GameObject vfxPrefab, Vector3 position)
    {
        SpawnVfx(vfxPrefab, position, Quaternion.identity);
    }

    private static void SpawnVfx(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
    {
        if (vfxPrefab != null)
        {
            Object.Instantiate(vfxPrefab, position, rotation);
        }
    }

    /// <summary>
    /// Góc xoay lúc spawn theo hướng ngắm (điểm rơi - vị trí caster), cùng pattern
    /// với <see cref="SkillProjectile"/>. Phép không định hướng hoặc self-cast
    /// (hướng gần bằng 0) giữ nguyên hướng mặc định của prefab.
    /// </summary>
    private static Quaternion ResolveSpawnRotation(SkillDefinitionSO skill, in SkillCastContext context)
    {
        if (!skill.OrientToAim)
        {
            return Quaternion.identity;
        }

        Vector2 aimDirection = context.TargetPoint - context.CasterTransform.position;
        bool hasUsableDirection = aimDirection.sqrMagnitude > 0.0001f;
        if (!hasUsableDirection)
        {
            return Quaternion.identity;
        }

        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, aimAngle + skill.AimRotationOffsetDegrees);
    }
}
