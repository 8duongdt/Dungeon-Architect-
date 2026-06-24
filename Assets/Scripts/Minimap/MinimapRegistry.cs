using System.Collections.Generic;

/// <summary>
/// Sổ đăng ký toàn cục các <see cref="UnitFaction"/> đang sống để mini-map vẽ chấm unit.
/// Mỗi unit tự đăng ký/hủy đăng ký trong OnEnable/OnDisable nên mini-map KHÔNG phải quét
/// <c>FindObjectsByType</c> mỗi frame, cũng không cần sửa prefab.
/// </summary>
public static class MinimapRegistry
{
    private static readonly List<UnitFaction> units = new();

    /// <summary>Danh sách unit đang hoạt động (chỉ đọc) cho mini-map duyệt.</summary>
    public static IReadOnlyList<UnitFaction> Units => units;

    public static void Register(UnitFaction unit)
    {
        if (unit != null && !units.Contains(unit))
        {
            units.Add(unit);
        }
    }

    public static void Unregister(UnitFaction unit)
    {
        units.Remove(unit);
    }
}
