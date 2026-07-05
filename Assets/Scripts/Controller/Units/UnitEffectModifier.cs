using UnityEngine;

/// <summary>
/// Hệ thống hiệu ứng module hóa, gắn lên MỘT unit/nhân vật bất kỳ trong Phase 1 (RTS).
/// Khi Awake nó tự gom mọi component có cài <see cref="ISpeedModifiable"/> (tốc độ),
/// <see cref="IMaxHealthModifiable"/> (máu tối đa) hoặc <see cref="IDamageOutputModifiable"/>
/// (sát thương gây ra) trên cùng GameObject rồi scale đồng loạt. Nhờ vậy một hiệu ứng áp dụng cho
/// CẢ di chuyển thủ công lẫn lúc combat, và dùng được cho mọi loại unit (kể cả Player) mà không
/// cần hardcode. <see cref="ApplySpeedPenalty"/>/<see cref="ClearSpeedPenalty"/> chỉ chạm tốc độ
/// (dùng cho hazard nước); <see cref="ApplyStatPenalty"/>/<see cref="ClearStatPenalty"/> chạm cả
/// ba (dùng cho vùng lãnh thổ pha lê đen).
/// </summary>
public class UnitEffectModifier : MonoBehaviour
{
    // Hệ số khi không có penalty nào (giữ nguyên tốc độ gốc).
    private const float NoPenaltyMultiplier = 1f;

    // Mọi nguồn tốc độ của unit này (di chuyển tay + AI). Có thể rỗng nếu unit chưa có component di chuyển.
    private ISpeedModifiable[] movementComponents;

    // Nguồn máu tối đa (UnitHealth) và sát thương gây ra (AttackState) - có thể rỗng, vd nhân vật
    // người chơi hiện chưa có component gây sát thương nào cài IDamageOutputModifiable.
    private IMaxHealthModifiable[] healthComponents;
    private IDamageOutputModifiable[] damageComponents;

    private void Awake()
    {
        movementComponents = GetComponents<ISpeedModifiable>();
        healthComponents = GetComponents<IMaxHealthModifiable>();
        damageComponents = GetComponents<IDamageOutputModifiable>();

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

    /// <summary>
    /// Áp một penalty lên TOÀN BỘ chỉ số (tốc độ + máu tối đa + sát thương gây ra) cùng lúc - dùng
    /// cho vùng lãnh thổ pha lê đen (<see cref="TerritoryZone"/>). Cũng luôn tính từ gốc như
    /// <see cref="ApplySpeedPenalty"/> nên không cộng dồn.
    /// </summary>
    public void ApplyStatPenalty(float multiplier)
    {
        SetSpeedMultiplier(multiplier);
        SetMaxHealthMultiplier(multiplier);
        SetDamageOutputMultiplier(multiplier);
    }

    /// <summary>Gỡ penalty toàn bộ chỉ số, đưa mọi thứ về gốc.</summary>
    public void ClearStatPenalty()
    {
        SetSpeedMultiplier(NoPenaltyMultiplier);
        SetMaxHealthMultiplier(NoPenaltyMultiplier);
        SetDamageOutputMultiplier(NoPenaltyMultiplier);
    }

    private void SetMaxHealthMultiplier(float multiplier)
    {
        foreach (IMaxHealthModifiable health in healthComponents)
        {
            health.MaxHealthMultiplier = multiplier;
        }
    }

    private void SetDamageOutputMultiplier(float multiplier)
    {
        foreach (IDamageOutputModifiable damage in damageComponents)
        {
            damage.DamageOutputMultiplier = multiplier;
        }
    }
}
