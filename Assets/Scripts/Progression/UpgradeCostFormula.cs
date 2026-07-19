using UnityEngine;

/// <summary>
/// Công thức giá nâng cấp dùng chung cho cây KỸ NĂNG và cây CÔNG TRÌNH ở sảnh chờ:
/// lợi ích tăng tuyến tính nhưng chi phí tăng lũy tiến (Cost = ceil(BaseCost × GrowthRate^n))
/// để tạo "hố đen tài nguyên" - càng lên cao càng phải cân nhắc kỹ trước khi tiêu điểm.
/// Vd: bậc 0→1 = 1, 1→2 = 1, 2→3 = 2, 3→4 = 4, 4→5 = 8.
/// </summary>
public static class UpgradeCostFormula
{
    public const int BaseCost = 1;
    public const float GrowthRate = 2f;

    /// <summary>Giá để nâng từ <paramref name="currentLevel"/> lên bậc kế tiếp.</summary>
    public static int NextLevelCost(int currentLevel)
    {
        int growthSteps = Mathf.Max(0, currentLevel - 1);
        return Mathf.CeilToInt(BaseCost * Mathf.Pow(GrowthRate, growthSteps));
    }
}
