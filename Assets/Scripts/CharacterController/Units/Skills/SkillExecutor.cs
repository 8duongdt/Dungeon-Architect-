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
        AudioManager.Instance.PlayOneShot(skill.CastSfx);

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
            case SkillMechanic.ChargeDash:
                ExecuteChargeDash(skill, context);
                break;
            default:
                ExecuteInstantStrike(skill, context);
                break;
        }
    }

    /// <summary>Đánh tức thời: VFX + sát thương ngay trên mục tiêu (hành vi gốc).</summary>
    private static void ExecuteInstantStrike(SkillDefinitionSO skill, in SkillCastContext context)
    {
        Vector2 aimDirection = context.TargetPoint - context.CasterTransform.position;
        SpawnVfx(skill.VfxPrefab, context.TargetPoint, aimDirection);

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
            // Tia nảy chuỗi hướng từ mục tiêu chính sang mục tiêu bị nảy cho tự nhiên.
            Vector2 bounceDirection = bounceTarget.transform.position - primaryPosition;
            SpawnVfx(skill.VfxPrefab, bounceTarget.transform.position, bounceDirection);
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
        GameObject zoneObject = Object.Instantiate(prefab, position, Quaternion.identity);
        ApplyDirection(zoneObject, context.TargetPoint - context.CasterTransform.position);
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

        // Bậc nâng cấp nhân cả lượng khiên lẫn thời gian tồn tại của khiên.
        float shieldDuration = skill.ShieldDuration * context.UpgradeMultiplier;
        casterHealth.AddShield(skill.ShieldAmount * context.UpgradeMultiplier, shieldDuration);
        SpawnAttachedVfx(skill.VfxPrefab, context.CasterTransform, shieldDuration);
    }

    /// <summary>
    /// VFX bám theo người thi triển suốt duration (con của caster) - dùng cho khiên/buff bản thân.
    /// SetParent giữ nguyên world scale nên VFX không bị phóng to theo localScale chuẩn hóa của unit.
    /// </summary>
    private static void SpawnAttachedVfx(GameObject vfxPrefab, Transform caster, float duration)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        GameObject vfxObject = Object.Instantiate(vfxPrefab, caster.position, Quaternion.identity);
        vfxObject.transform.SetParent(caster, worldPositionStays: true);

        var oneShot = vfxObject.GetComponent<SkillVfxOneShot>();
        if (oneShot != null)
        {
            oneShot.OverrideLifetime(duration);
        }
        else
        {
            Object.Destroy(vfxObject, duration);
        }
    }

    /// <summary>
    /// Lao thẳng tới điểm mục tiêu (chốt lúc cast): dời caster bằng Rigidbody2D theo nhịp
    /// FixedUpdate cho tới khi hết quãng lao hoặc chạm gần điểm rơi, rồi nổ sát thương +
    /// đẩy lùi quanh điểm dừng. Không có host chạy coroutine thì rơi về đánh tức thời.
    /// </summary>
    private static void ExecuteChargeDash(SkillDefinitionSO skill, in SkillCastContext context)
    {
        if (!CanHostCoroutine(context.CoroutineHost))
        {
            ExecuteInstantStrike(skill, context);
            return;
        }

        context.CoroutineHost.StartCoroutine(ChargeRoutine(
            skill, context.CasterTransform, context.TargetPoint, context.Damage, context.CasterFaction));
    }

    private static IEnumerator ChargeRoutine(
        SkillDefinitionSO skill, Transform caster, Vector3 targetPoint, float damage, UnitFaction casterFaction)
    {
        Vector2 chargeDirection = targetPoint - caster.position;
        if (chargeDirection.sqrMagnitude < 0.0001f)
        {
            yield break;
        }

        chargeDirection.Normalize();
        var casterBody = caster.GetComponent<Rigidbody2D>();
        var casterHealth = caster.GetComponent<UnitHealth>();
        float traveledDistance = 0f;
        float impactRadiusSqr = skill.ChargeImpactRadius * skill.ChargeImpactRadius;

        while (traveledDistance < skill.ChargeMaxDistance)
        {
            yield return new WaitForFixedUpdate();

            bool casterGone = caster == null || (casterHealth != null && casterHealth.IsDead);
            if (casterGone)
            {
                yield break;
            }

            float stepDistance = skill.ChargeSpeed * Time.fixedDeltaTime;
            Vector2 nextPosition = (Vector2)caster.position + chargeDirection * stepDistance;
            if (casterBody != null)
            {
                casterBody.MovePosition(nextPosition);
            }
            else
            {
                caster.position = nextPosition;
            }

            traveledDistance += stepDistance;
            bool reachedTargetPoint = ((Vector2)targetPoint - nextPosition).sqrMagnitude <= impactRadiusSqr;
            if (reachedTargetPoint)
            {
                break;
            }
        }

        if (caster == null)
        {
            yield break;
        }

        SpawnVfx(skill.VfxPrefab, caster.position);
        foreach (UnitHealth victim in SkillTargeting.FindAttackableInRadius(
            caster.position, skill.ChargeImpactRadius, casterFaction))
        {
            victim.TakeDamage(damage);
            Vector2 awayFromCaster = victim.transform.position - caster.position;
            UnitKnockback.Push(victim.gameObject, awayFromCaster, skill.ChargeKnockbackDistance);
        }
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

        // Xuyên giáp để chắc chắn kết liễu; cộng khiên để bào hết lớp khiên ảo trước máu.
        target.TakeTrueDamage(target.CurrentHealth + target.CurrentShield);
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
        SpawnVfx(vfxPrefab, position, Vector2.zero);
    }

    private static void SpawnVfx(GameObject vfxPrefab, Vector3 position, Vector2 aimDirection)
    {
        if (vfxPrefab == null)
        {
            return;
        }

        GameObject vfxObject = Object.Instantiate(vfxPrefab, position, Quaternion.identity);
        ApplyDirection(vfxObject, aimDirection);
    }

    /// <summary>
    /// Xoay object theo hướng thi triển nếu prefab có gắn <see cref="SkillDirection"/>;
    /// prefab không gắn (phép nổ tròn, buff) thì giữ nguyên hướng mặc định.
    /// </summary>
    private static void ApplyDirection(GameObject spawnedObject, Vector2 aimDirection)
    {
        var skillDirection = spawnedObject.GetComponent<SkillDirection>();
        if (skillDirection != null)
        {
            skillDirection.SetupDirection(aimDirection);
        }
    }
}
