using UnityEngine;

/// <summary>
/// Luật ràng buộc "không đặt quá gần cổng sinh quái" cho công trình lãnh thổ (pha lê đen -
/// <see cref="TerritoryController"/>). Nhu cầu công trình suy ra trực tiếp từ prefab như
/// <see cref="CrystalBuildRestriction"/> - không cần field dữ liệu trùng lặp trên
/// <see cref="PlacedObjectTypeSO"/>. Vị trí cổng lấy từ <see cref="PortalMarkerRegistry"/>
/// (mỗi <see cref="EnemySpawner"/> tự đăng ký) nên không phải quét FindObjectsByType mỗi lần xây.
/// </summary>
public static class PortalBuildRestriction
{
    // Bán kính cấm đặt quanh cổng - đủ xa để vùng lãnh thổ không "ôm" trọn điểm sinh quái.
    private const float MinPortalDistance = 12f;

    /// <summary>true nếu công trình này không phải công trình lãnh thổ, hoặc
    /// <paramref name="worldOrigin"/> cách mọi cổng sinh quái tối thiểu <see cref="MinPortalDistance"/>.</summary>
    public static bool IsSatisfied(PlacedObjectTypeSO type, Vector3 worldOrigin)
    {
        if (!RequiresPortalDistance(type))
        {
            return true;
        }

        foreach (Transform portal in PortalMarkerRegistry.Portals)
        {
            if (portal != null && Vector2.Distance(portal.position, worldOrigin) < MinPortalDistance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool RequiresPortalDistance(PlacedObjectTypeSO type)
    {
        return type != null
            && type.prefab != null
            && type.prefab.GetComponent<TerritoryController>() != null;
    }
}
