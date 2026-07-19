using UnityEngine;

/// <summary>
/// Suy ra CỠ MAP Phase 1 (1 = nhỏ, 2 = vừa, 3 = to) từ tiến độ cây kỹ năng của người chơi: càng gần
/// "nâng full" cây skill thì map càng to (và càng nhiều cổng). Thuần tính toán, không giữ state riêng -
/// đọc bậc đã đầu tư ở <see cref="PlayerProgression"/> và số node của cây kỹ năng truyền vào. Degrade
/// an toàn về cỡ 1 khi chưa có cây/chưa mở skill nào.
/// </summary>
public static class MapSizeProgression
{
    public const int MinLevel = 1;
    public const int MaxLevel = 3;

    // Ngưỡng tỉ lệ hoàn thành cây (bậc đã đầu tư / bậc tối đa) để lên cỡ map. ~1 nửa cây -> vừa; gần full -> to.
    private const float MediumRatioThreshold = 0.45f;
    private const float LargeRatioThreshold = 0.8f;

    /// <summary>Cỡ map ứng với tiến độ hiện tại của cây kỹ năng <paramref name="skillTree"/>.</summary>
    public static int LevelFor(SkillTreeSO skillTree)
    {
        if (skillTree == null)
        {
            return MinLevel;
        }

        int maxLevels = skillTree.Nodes.Count * PlayerProgression.MaxSkillLevel;
        if (maxLevels <= 0)
        {
            return MinLevel;
        }

        float completion = (float)PlayerProgression.GetTotalSkillLevels() / maxLevels;
        if (completion >= LargeRatioThreshold)
        {
            return MaxLevel;
        }
        if (completion >= MediumRatioThreshold)
        {
            return 2;
        }
        return MinLevel;
    }
}
