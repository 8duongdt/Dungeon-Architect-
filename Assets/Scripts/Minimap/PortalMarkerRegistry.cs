using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sổ đăng ký toàn cục vị trí các cổng sinh quái đang hoạt động để mini-map vẽ icon cổng.
/// Mỗi <see cref="EnemySpawner"/> tự đăng ký/hủy đăng ký trong OnEnable/OnDisable, theo đúng mẫu
/// <see cref="MinimapRegistry"/> - mini-map KHÔNG phải quét FindObjectsByType mỗi khi map sinh lại.
/// </summary>
public static class PortalMarkerRegistry
{
    private static readonly List<Transform> portals = new();

    /// <summary>Danh sách cổng đang hoạt động (chỉ đọc) cho mini-map duyệt.</summary>
    public static IReadOnlyList<Transform> Portals => portals;

    public static void Register(Transform portal)
    {
        if (portal != null && !portals.Contains(portal))
        {
            portals.Add(portal);
        }
    }

    public static void Unregister(Transform portal)
    {
        portals.Remove(portal);
    }
}
