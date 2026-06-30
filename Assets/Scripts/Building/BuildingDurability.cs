using System;
using UnityEngine;

/// <summary>
/// Độ bền (HP) của một công trình. Hiện chưa có nguồn gây sát thương nào nhưng hệ thống là thật:
/// hỗ trợ nhận sát thương, sửa chữa và đổi trần HP khi nâng cấp. Cửa sổ Trạng thái đọc
/// <see cref="Fraction"/> để vẽ thanh máu và <see cref="CurrentDurability"/>/<see cref="MaxDurability"/>
/// để hiển thị số liệu.
/// </summary>
public class BuildingDurability : MonoBehaviour
{
    [SerializeField] [Min(1f)] private float maxDurability = 1000f;
    [SerializeField] [Min(0f)] private float currentDurability = 1000f;

    /// <summary>Bắn khi HP hoặc trần HP thay đổi (để HUD cập nhật thanh máu).</summary>
    public event Action DurabilityChanged;

    public float MaxDurability => maxDurability;
    public float CurrentDurability => currentDurability;

    /// <summary>Tỉ lệ HP hiện tại (0..1) để điều khiển fillAmount của thanh máu.</summary>
    public float Fraction => maxDurability > 0f ? Mathf.Clamp01(currentDurability / maxDurability) : 0f;

    private void OnValidate()
    {
        currentDurability = Mathf.Clamp(currentDurability, 0f, maxDurability);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentDurability = Mathf.Max(0f, currentDurability - amount);
        DurabilityChanged?.Invoke();
    }

    public void Repair(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
        DurabilityChanged?.Invoke();
    }

    /// <summary>Đặt trần HP mới (dùng khi nâng cấp). <paramref name="refillToFull"/> hồi đầy luôn.</summary>
    public void SetMax(float newMax, bool refillToFull)
    {
        maxDurability = Mathf.Max(1f, newMax);
        currentDurability = refillToFull ? maxDurability : Mathf.Min(currentDurability, maxDurability);
        DurabilityChanged?.Invoke();
    }
}
