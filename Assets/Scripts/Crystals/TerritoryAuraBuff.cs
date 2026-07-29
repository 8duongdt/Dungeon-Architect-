using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Aura buff của Obelisk (<see cref="TerritoryController"/>): unit phe PLAYER đứng trong bán kính
/// lãnh thổ được buff sát thương + máu tối đa + tốc độ đánh + tốc độ chạy; rời xa thì về bình
/// thường. Hệ số buff đọc từ <see cref="BuildingProgressionEffects.GetObeliskMultipliers"/> (20%
/// gốc + tối đa 20% cộng thêm từ nâng cấp Building Points ở Lobby, trần cứng 40%).
///
/// Không dùng trigger collider: poll <see cref="UnitFaction.ActiveUnits"/> theo nhịp, cùng pattern
/// với <see cref="PortalAuraBuff"/>. Buff đi qua <see cref="UnitEffectModifier"/> có ref-count nên
/// đứng trong tầm nhiều Obelisk cùng lúc không bị gỡ nhầm khi rời một cái.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TerritoryController))]
public class TerritoryAuraBuff : MonoBehaviour
{
    [Tooltip("Chu kỳ (giây) quét lại danh sách unit trong/ngoài lãnh thổ.")]
    [SerializeField] private float refreshInterval = 0.2f;

    private TerritoryController territory;

    // Các unit hiện đang được Obelisk NÀY buff (để biết ai vừa vào/vừa ra tầm).
    private readonly HashSet<UnitEffectModifier> buffedUnits = new HashSet<UnitEffectModifier>();
    private readonly List<UnitEffectModifier> stillInRange = new List<UnitEffectModifier>();
    private float refreshTimer;

    private void Awake()
    {
        territory = GetComponent<TerritoryController>();
    }

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
        // Obelisk bị phá/biến mất: gỡ buff khỏi mọi unit còn sống đang chịu ảnh hưởng.
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
        float radiusSqr = territory.Radius * territory.Radius;
        Vector2 origin = territory.Origin.position;

        foreach (UnitFaction faction in UnitFaction.ActiveUnits)
        {
            if (faction == null || faction.Faction != FactionType.Player)
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
            // Unit MỚI vào tầm Obelisk này -> bật thêm một nguồn buff.
            if (buffedUnits.Add(modifier))
            {
                (float damage, float maxHealth, float attackCooldown, float moveSpeed) =
                    BuildingProgressionEffects.GetObeliskMultipliers();
                modifier.ApplyBuff(damage, maxHealth, attackCooldown, moveSpeed);
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
        // Awake (nơi gán territory) không chạy ở Edit Mode ngoài Play - tự resolve tại đây để
        // gizmo vẫn hiện đúng bán kính khi chọn Obelisk trong Scene view lúc chưa bấm Play.
        TerritoryController controller = territory != null ? territory : GetComponent<TerritoryController>();
        Gizmos.color = new Color(0.4f, 0.1f, 0.6f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, controller != null ? controller.Radius : 0f);
    }
}
