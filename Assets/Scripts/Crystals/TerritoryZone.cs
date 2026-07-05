using UnityEngine;

/// <summary>
/// Vùng lãnh thổ pha lê đen: unit phe người chơi RA khỏi vùng bị giảm 50% toàn bộ chỉ số (tốc độ,
/// máu tối đa, sát thương gây ra), VÀO lại thì hồi bình thường. Ngược lại với
/// <see cref="UndeadWaterHazard"/> (ở trong mới bị phạt) và chỉ áp dụng cho phe Player (unit phe
/// Enemy không thuộc lãnh thổ người chơi nên không liên quan).
/// </summary>
[RequireComponent(typeof(TerritoryController))]
public class TerritoryZone : MonoBehaviour
{
    [Tooltip("Hệ số chỉ số còn lại khi ở ngoài lãnh thổ (0.5 = còn một nửa).")]
    [Range(0f, 1f)]
    [SerializeField] private float debuffMultiplier = 0.5f;

    private void OnTriggerExit2D(Collider2D other)
    {
        if (TryGetPlayerFactionModifier(other, out UnitEffectModifier modifier))
        {
            modifier.ApplyStatPenalty(debuffMultiplier);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryGetPlayerFactionModifier(other, out UnitEffectModifier modifier))
        {
            modifier.ClearStatPenalty();
        }
    }

    private static bool TryGetPlayerFactionModifier(Collider2D other, out UnitEffectModifier modifier)
    {
        modifier = other.GetComponentInParent<UnitEffectModifier>();
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        return modifier != null && faction != null && faction.Faction == FactionType.Player;
    }
}
