using UnityEngine;

/// <summary>
/// Hệ thống hiệu ứng module hóa, gắn lên MỘT unit/nhân vật bất kỳ trong Phase 1 (RTS).
/// Khi Awake nó tự gom mọi component có cài <see cref="ISpeedModifiable"/> (tốc độ) trên cùng
/// GameObject rồi scale đồng loạt. Nhờ vậy một hiệu ứng áp dụng cho CẢ di chuyển thủ công lẫn lúc
/// combat, và dùng được cho mọi loại unit (kể cả Player) mà không cần hardcode.
/// <see cref="ApplySpeedPenalty"/>/<see cref="ClearSpeedPenalty"/> chỉ chạm tốc độ (dùng cho hazard nước);
/// <see cref="ApplyBuff"/>/<see cref="ClearBuff"/> tăng sát thương/máu/tốc-đánh (buff aura portal), có
/// REF-COUNT nên đứng trong tầm nhiều cổng cùng lúc không xung đột khi rời từng cổng.
/// </summary>
public class UnitEffectModifier : MonoBehaviour
{
    // Hệ số khi không có penalty nào (giữ nguyên tốc độ gốc).
    private const float NoPenaltyMultiplier = 1f;

    // Mọi nguồn tốc độ của unit này (di chuyển tay + AI). Có thể rỗng nếu unit chưa có component di chuyển.
    private ISpeedModifiable[] movementComponents;

    // Kênh nhân chỉ số combat cho buff (có thể null nếu unit không đánh nhau).
    private AttackState attackState;
    private UnitHealth unitHealth;

    // Số nguồn buff (aura) đang phủ lên unit - chỉ áp khi 0->1, chỉ gỡ khi về 0.
    private int buffSourceCount;

    private void Awake()
    {
        movementComponents = GetComponents<ISpeedModifiable>();
        attackState = GetComponent<AttackState>();
        unitHealth = GetComponent<UnitHealth>();

        if (movementComponents.Length == 0)
        {
            Debug.LogWarning($"[UnitEffectModifier] '{name}' không có component di chuyển nào cài ISpeedModifiable.");
        }
    }

    /// <summary>
    /// Bật buff (một nguồn aura vào tầm): tăng sát thương/máu tối đa/tốc độ đánh. Ref-count nên nhiều
    /// aura chồng nhau chỉ áp một lần; mọi hệ số tính TỪ gốc nên không cộng dồn.
    /// </summary>
    public void ApplyBuff(float damageMultiplier, float maxHealthMultiplier, float attackCooldownMultiplier)
    {
        buffSourceCount++;
        if (buffSourceCount != 1)
        {
            return;
        }

        attackState?.SetDamageMultiplier(damageMultiplier);
        attackState?.SetAttackCooldownMultiplier(attackCooldownMultiplier);
        unitHealth?.SetMaxHealthMultiplier(maxHealthMultiplier);
    }

    /// <summary>Tắt buff của MỘT nguồn aura; chỉ thực sự gỡ về gốc khi không còn aura nào phủ.</summary>
    public void ClearBuff()
    {
        if (buffSourceCount == 0)
        {
            return;
        }

        buffSourceCount--;
        if (buffSourceCount > 0)
        {
            return;
        }

        attackState?.SetDamageMultiplier(NoPenaltyMultiplier);
        attackState?.SetAttackCooldownMultiplier(NoPenaltyMultiplier);
        unitHealth?.SetMaxHealthMultiplier(NoPenaltyMultiplier);
    }

    /// <summary>
    /// Áp một penalty làm chậm. Penalty luôn tính từ tốc độ gốc nên gọi nhiều lần không
    /// bị cộng dồn: ApplySpeedPenalty(0.5f) luôn cho ra nửa tốc độ gốc.
    /// </summary>
    /// <param name="multiplier">Hệ số nhân (ví dụ 0.5f để còn một nửa tốc độ).</param>
    public void ApplySpeedPenalty(float multiplier)
    {
        SetSpeedMultiplier(multiplier);
    }

    /// <summary>
    /// Gỡ mọi penalty và đưa tốc độ về đúng giá trị gốc.
    /// </summary>
    public void ClearSpeedPenalty()
    {
        SetSpeedMultiplier(NoPenaltyMultiplier);
    }

    private void SetSpeedMultiplier(float multiplier)
    {
        foreach (ISpeedModifiable movement in movementComponents)
        {
            movement.SpeedMultiplier = multiplier;
        }
    }
}
