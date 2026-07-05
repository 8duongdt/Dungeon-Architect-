using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sổ đăng ký tĩnh mọi <see cref="CrystalNode"/> đang hoạt động trong scene, theo đúng mẫu
/// <c>MinimapRegistry</c> (tự đăng ký qua OnEnable/OnDisable). Mọi hệ thống cần tra cứu tinh thể
/// (giới hạn xây dựng, kích hoạt tinh thể gần điểm xuất phát, AI tìm tinh thể để chiếm) đi qua đây
/// thay vì FindObjectsByType mỗi khung hình.
/// </summary>
public static class CrystalNodeRegistry
{
    private static readonly List<CrystalNode> nodes = new List<CrystalNode>();

    public static IReadOnlyList<CrystalNode> All => nodes;

    public static void Register(CrystalNode node)
    {
        if (!nodes.Contains(node))
        {
            nodes.Add(node);
        }
    }

    public static void Unregister(CrystalNode node)
    {
        nodes.Remove(node);
    }

    /// <summary>Tinh thể gần nhất thuộc loại này, bất kể trạng thái - dùng để kích hoạt tinh thể
    /// Gold gần điểm xuất phát của người chơi khi map vừa sinh.</summary>
    public static CrystalNode FindNearestOfType(Vector3 position, CrystalType type)
    {
        return FindNearest(position, node => node.Type == type);
    }

    /// <summary>Tinh thể Active gần nhất, bất kể loại - dùng cho AI quái tìm mục tiêu để chiếm.</summary>
    public static CrystalNode FindNearestActive(Vector3 position)
    {
        return FindNearest(position, node => node.State == CrystalState.Active);
    }

    private static CrystalNode FindNearest(Vector3 position, Func<CrystalNode, bool> predicate)
    {
        CrystalNode nearest = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (CrystalNode node in nodes)
        {
            if (node == null || !predicate(node))
            {
                continue;
            }

            float sqrDistance = (node.transform.position - position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = node;
            }
        }

        return nearest;
    }
}
