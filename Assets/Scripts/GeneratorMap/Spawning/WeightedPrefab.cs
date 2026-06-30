using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Một mục trong "loot table": prefab kèm trọng số xuất hiện. Vật hiếm để trọng số nhỏ.
/// Ví dụ: Rương gỗ (70) / Rương sắt (25) / Rương vàng (5).
/// </summary>
[Serializable]
public struct WeightedPrefab
{
    public GameObject prefab;

    [Min(0)]
    public int weight;

    [Tooltip("true = vật cản vật lý (tự thêm BoxCollider2D nếu prefab chưa có); false = trang trí thuần (cây cỏ nhỏ).")]
    public bool isObstacle;
}

/// <summary>Chọn ngẫu nhiên theo trọng số từ một bảng <see cref="WeightedPrefab"/>.</summary>
public static class WeightedTable
{
    /// <summary>
    /// Trả về prefab được chọn theo trọng số tích luỹ; null nếu bảng rỗng hoặc tổng trọng số = 0.
    /// </summary>
    public static GameObject Pick(IReadOnlyList<WeightedPrefab> table)
    {
        WeightedPrefab? entry = PickEntry(table);
        return entry.HasValue ? entry.Value.prefab : null;
    }

    /// <summary>
    /// Trả về toàn bộ entry được chọn (kèm <see cref="WeightedPrefab.isObstacle"/>);
    /// null nếu bảng rỗng hoặc tổng trọng số = 0.
    /// </summary>
    public static WeightedPrefab? PickEntry(IReadOnlyList<WeightedPrefab> table)
    {
        if (table == null || table.Count == 0)
        {
            return null;
        }

        int totalWeight = TotalWeight(table);
        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (WeightedPrefab entry in table)
        {
            cumulative += entry.weight;
            if (roll < cumulative)
            {
                return entry;
            }
        }

        return null;
    }

    private static int TotalWeight(IReadOnlyList<WeightedPrefab> table)
    {
        int total = 0;
        foreach (WeightedPrefab entry in table)
        {
            total += Mathf.Max(0, entry.weight);
        }
        return total;
    }
}
