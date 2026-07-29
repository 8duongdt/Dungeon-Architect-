using UnityEngine;

/// <summary>
/// Hệ thống hiệu ứng module hóa, gắn lên MỘT unit/nhân vật bất kỳ trong Phase 1 (RTS).
/// Khi Awake nó tự gom mọi component có cài <see cref="ISpeedModifiable"/> (tốc độ) trên cùng
/// GameObject rồi scale đồng loạt. Nhờ vậy một hiệu ứng áp dụng cho CẢ di chuyển thủ công lẫn lúc
/// combat, và dùng được cho mọi loại unit (kể cả Player) mà không cần hardcode.
/// <see cref="ApplySpeedPenalty"/>/<see cref="ClearSpeedPenalty"/> chỉ chạm tốc độ (dùng cho hazard nước/
/// hiệu ứng làm chậm), set TUYỆT ĐỐI trên một kênh riêng; <see cref="ApplyBuff"/>/<see cref="ClearBuff"/>
/// tăng sát thương/máu/tốc-đánh/tốc-chạy (buff aura portal/lãnh thổ), REF-COUNT trên một kênh tốc độ
/// riêng khác. Tốc độ cuối cùng áp lên unit là TÍCH của 2 kênh, nên một unit vừa được buff (Obelisk)
/// vừa dính penalty (nước) không bị kênh này ghi đè mất kênh kia.
/// </summary>
public class UnitEffectModifier : MonoBehaviour
{
    // Hệ số khi không có penalty/buff nào (giữ nguyên tốc độ gốc).
    private const float NoPenaltyMultiplier = 1f;

    // Mọi nguồn tốc độ của unit này (di chuyển tay + AI). Có thể rỗng nếu unit chưa có component di chuyển.
    private ISpeedModifiable[] movementComponents;

    // Kênh nhân chỉ số combat cho buff (có thể null nếu unit không đánh nhau).
    private AttackState attackState;
    private UnitHealth unitHealth;

    // Số nguồn buff (aura) đang phủ lên unit - chỉ áp khi 0->1, chỉ gỡ khi về 0.
    private int buffSourceCount;

    // 2 kênh tốc độ độc lập, nhân với nhau ra tốc độ cuối - xem doc class.
    private float buffSpeedMultiplier = NoPenaltyMultiplier;
    private float penaltySpeedMultiplier = NoPenaltyMultiplier;

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
    /// Bật buff (một nguồn aura vào tầm): tăng sát thương/máu tối đa/tốc độ đánh/tốc độ chạy. Ref-count
    /// nên nhiều aura chồng nhau chỉ áp một lần; mọi hệ số tính TỪ gốc nên không cộng dồn.
    /// </summary>
    /// <param name="speedMultiplier">Hệ số tốc chạy (mặc định 1 = không đổi, vd buff Enemy quanh cổng
    /// không cần chạm tốc độ).</param>
    public void ApplyBuff(float damageMultiplier, float maxHealthMultiplier, float attackCooldownMultiplier,
        float speedMultiplier = NoPenaltyMultiplier)
    {
        buffSourceCount++;
        if (buffSourceCount != 1)
        {
            return;
        }

        attackState?.SetDamageMultiplier(damageMultiplier);
        attackState?.SetAttackCooldownMultiplier(attackCooldownMultiplier);
        unitHealth?.SetMaxHealthMultiplier(maxHealthMultiplier);
        buffSpeedMultiplier = speedMultiplier;
        ApplyCombinedSpeedMultiplier();
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
        buffSpeedMultiplier = NoPenaltyMultiplier;
        ApplyCombinedSpeedMultiplier();
    }

    /// <summary>
    /// Áp một penalty làm chậm. Penalty luôn tính từ tốc độ gốc nên gọi nhiều lần không
    /// bị cộng dồn: ApplySpeedPenalty(0.5f) luôn cho ra nửa tốc độ gốc (nhân thêm với buff hiện có).
    /// </summary>
    /// <param name="multiplier">Hệ số nhân (ví dụ 0.5f để còn một nửa tốc độ).</param>
    public void ApplySpeedPenalty(float multiplier)
    {
        penaltySpeedMultiplier = multiplier;
        ApplyCombinedSpeedMultiplier();
    }

    /// <summary>
    /// Gỡ mọi penalty và đưa tốc độ về đúng giá trị gốc (vẫn giữ nguyên buff aura nếu có).
    /// </summary>
    public void ClearSpeedPenalty()
    {
        penaltySpeedMultiplier = NoPenaltyMultiplier;
        ApplyCombinedSpeedMultiplier();
    }

    private void ApplyCombinedSpeedMultiplier()
    {
        SetSpeedMultiplier(buffSpeedMultiplier * penaltySpeedMultiplier);
    }

    private void SetSpeedMultiplier(float multiplier)
    {
        foreach (ISpeedModifiable movement in movementComponents)
        {
            movement.SpeedMultiplier = multiplier;
        }
    }
}
