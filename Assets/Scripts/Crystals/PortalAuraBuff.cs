using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aura tiếp sức của một CỔNG (portal): unit phe ENEMY (Human) đứng trong bán kính được buff
/// sát thương + máu tối đa + tốc độ đánh; rời xa thì về bình thường. Thay cho debuff người chơi cũ
/// (đã gỡ) để cân bằng - tạo thế giằng co: người chơi nên đánh nhau ở xa cổng.
///
/// Không dùng trigger collider (khỏi phải sửa collider prefab cổng): poll <see cref="UnitFaction.ActiveUnits"/>
/// theo nhịp, giống <see cref="FogOfWarController"/>. Buff đi qua <see cref="UnitEffectModifier"/> có
/// ref-count nên đứng trong tầm nhiều cổng cùng lúc không bị gỡ nhầm khi rời một cổng.
/// </summary>
[DisallowMultipleComponent]
public class PortalAuraBuff : MonoBehaviour
{
    [Tooltip("Bán kính aura buff quanh cổng (đơn vị world).")]
    [SerializeField] private float auraRadius = 6f;

    [Tooltip("Hệ số nhân sát thương cho Human trong tầm (1.5 = +50%).")]
    [SerializeField] private float damageMultiplier = 1.5f;

    [Tooltip("Hệ số nhân máu tối đa cho Human trong tầm (1.4 = +40%).")]
    [SerializeField] private float maxHealthMultiplier = 1.4f;

    [Tooltip("Hệ số nhân thời gian hồi đòn (0.75 = đánh nhanh hơn 25%).")]
    [SerializeField] private float attackCooldownMultiplier = 0.75f;

    [Tooltip("Chu kỳ (giây) quét lại danh sách unit trong/ngoài tầm.")]
    [SerializeField] private float refreshInterval = 0.2f;

    // Các unit hiện đang được cổng NÀY buff (để biết ai vừa vào/vừa ra tầm).
    private readonly HashSet<UnitEffectModifier> buffedUnits = new HashSet<UnitEffectModifier>();
    private readonly List<UnitEffectModifier> stillInRange = new List<UnitEffectModifier>();
    private float refreshTimer;

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = refreshInterval;
        RefreshAura();
    }

    private void OnDisable()
    {
        // Cổng biến mất (sinh lại map): gỡ buff khỏi mọi unit còn sống đang chịu ảnh hưởng.
        foreach (UnitEffectModifier modifier in buffedUnits)
        {
            if (modifier != null)
            {
                modifier.ClearBuff();
            }
        }
        buffedUnits.Clear();
    }

    private void RefreshAura()
    {
        stillInRange.Clear();
        float radiusSqr = auraRadius * auraRadius;
        Vector2 origin = transform.position;

        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction != FactionType.Enemy)
            {
                continue;
            }

            if (((Vector2)faction.transform.position - origin).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            UnitEffectModifier modifier = faction.GetComponentInParent<UnitEffectModifier>();
            if (modifier == null)
            {
                continue;
            }

            stillInRange.Add(modifier);
            // Unit MỚI vào tầm cổng này -> bật thêm một nguồn buff.
            if (buffedUnits.Add(modifier))
            {
                modifier.ApplyBuff(damageMultiplier, maxHealthMultiplier, attackCooldownMultiplier);
            }
        }

        RemoveUnitsThatLeft();
    }

    // Gỡ buff cho unit đã rời tầm (hoặc đã chết/despawn -> modifier fake-null).
    private void RemoveUnitsThatLeft()
    {
        buffedUnits.RemoveWhere(modifier =>
        {
            if (modifier == null)
            {
                return true;
            }

            if (!stillInRange.Contains(modifier))
            {
                modifier.ClearBuff();
                return true;
            }

            return false;
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
