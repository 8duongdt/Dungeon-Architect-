using UnityEngine;

/// <summary>
/// Hệ thống hiệu ứng module hóa, gắn lên MỘT unit/nhân vật bất kỳ trong Phase 1 (RTS).
/// Khi Awake nó tự gom mọi component di chuyển có cài <see cref="ISpeedModifiable"/> trên
/// cùng GameObject - ví dụ <c>Unit</c> (lệnh di chuyển tay) và <c>UnitMovement</c> (AI đuổi đánh) -
/// rồi scale tốc độ đồng loạt. Nhờ vậy một hiệu ứng làm chậm áp dụng cho CẢ di chuyển thủ công
/// lẫn lúc combat, và dùng được cho mọi loại unit (kể cả Player) mà không cần hardcode.
/// </summary>
public class UnitEffectModifier : MonoBehaviour
{
    // Hệ số khi không có penalty nào (giữ nguyên tốc độ gốc).
    private const float NoPenaltyMultiplier = 1f;

    // Mọi nguồn tốc độ của unit này (di chuyển tay + AI). Có thể rỗng nếu unit chưa có component di chuyển.
    private ISpeedModifiable[] movementComponents;

    private void Awake()
    {
        movementComponents = GetComponents<ISpeedModifiable>();
        if (movementComponents.Length == 0)
        {
            Debug.LogWarning($"[UnitEffectModifier] '{name}' không có component di chuyển nào cài ISpeedModifiable.");
        }
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
