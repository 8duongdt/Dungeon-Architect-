using UnityEngine;

/// <summary>
/// Công thức chiến đấu dùng chung. Tách riêng để mọi nguồn sát thương tính giống nhau và dễ kiểm thử.
/// </summary>
public static class CombatFormulas
{
    // Đòn đánh luôn gây tối thiểu 1 sát thương dù giáp cao hơn - tránh unit bất tử trước đòn yếu.
    private const float MinimumDamage = 1f;

    // Hằng số giáp (K): DEF = K thì giảm đúng 50% sát thương. Càng lớn thì giáp càng "loãng".
    private const float DefenseConstant = 50f;

    /// <summary>
    /// Giảm-trừ theo PHẦN TRĂM: sát thương nhận = incoming x (1 - DEF/(DEF+K)).
    /// Nhờ dạng phần trăm, DEF cao không khiến unit bất tử và ATK thấp vẫn luôn xuyên được (sàn 1).
    /// </summary>
    public static float MitigateByDefense(float incoming, float defense)
    {
        if (incoming <= 0f)
        {
            return 0f;
        }

        float safeDefense = Mathf.Max(0f, defense);
        float damageAfterDefense = incoming * (1f - safeDefense / (safeDefense + DefenseConstant));
        return Mathf.Max(MinimumDamage, damageAfterDefense);
    }
}
