using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sổ tra cứu toàn cục các CÔNG TRÌNH phe người chơi CÓ THỂ BỊ PHÁ (có UnitHealth), để AI Human
/// biết chỗ mà kéo quân tới đập sau khi chiếm được tinh thể. Đăng ký trong Start / gỡ trong
/// OnDestroy của <see cref="BuildingCombatDeath"/> (phải là Start vì UnitFaction/PlacedObject gắn
/// runtime SAU Instantiate); công trình không bị bật/tắt trong trận nên không cần OnEnable. Chỉ chứa công trình
/// thật sự phá được (Void_Obsidian_Obelisk chỉ có BuildingDurability, không UnitHealth, nên KHÔNG
/// vào đây - tránh việc quái kéo tới đứng đập một công trình bất tử).
/// </summary>
public static class PlayerBuildingRegistry
{
    private static readonly List<Transform> buildings = new List<Transform>();

    public static IReadOnlyList<Transform> All => buildings;

    public static void Register(Transform building)
    {
        if (building != null && !buildings.Contains(building))
        {
            buildings.Add(building);
        }
    }

    public static void Unregister(Transform building)
    {
        buildings.Remove(building);
    }

    /// <summary>Công trình người chơi gần vị trí này nhất; null nếu không còn công trình nào.</summary>
    public static Transform FindNearest(Vector3 fromPosition)
    {
        Transform nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (Transform building in buildings)
        {
            if (building == null)
            {
                continue;
            }

            float sqr = ((Vector2)building.position - (Vector2)fromPosition).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = building;
            }
        }
        return nearest;
    }
}
