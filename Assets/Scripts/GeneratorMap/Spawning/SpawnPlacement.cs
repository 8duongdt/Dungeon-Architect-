using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper chung cho các spawner rải unit quanh một tâm (cổng, tinh thể bị chiếm): roll offset ngẫu
/// nhiên trong bán kính, ƯU TIÊN vị trí vừa rơi trên ô đi lại được (Floor/SwampWater/Gate) vừa
/// không đè lên unit khác - tránh quái spawn kẹt trong tường hoặc chồng cục lên nhau. Null-safe:
/// scene không có DungeonManager/map (vd Phase 2) thì mọi điểm đều coi là nền hợp lệ như hành vi cũ.
/// </summary>
public static class SpawnPlacement
{
    // Số lần thử roll offset trước khi bỏ cuộc và spawn ngay tại tâm.
    private const int OffsetRollTries = 12;

    // Bán kính kiểm tra chỗ trống quanh điểm rơi - có unit trong vòng này coi như chỗ đã chật.
    private const float UnitClearanceRadius = 0.6f;

    // Bộ đệm quét vật lý dùng lại giữa các lần roll - không cấp phát rác mỗi lần spawn.
    private static readonly List<Collider2D> overlapResults = new List<Collider2D>(32);
    private static readonly ContactFilter2D anyColliderFilter = CreateAnyColliderFilter();

    private static DungeonManager dungeonManager;
    private static TilemapVisualizer visualizer;

    /// <summary>
    /// Vị trí spawn quanh tâm: ưu tiên ô đi được còn trống; mọi ô đi được đều chật thì chấp nhận
    /// chen chúc còn hơn rơi vào tường; hết lượt thử thì trả về đúng tâm (tâm cổng/tinh thể luôn
    /// nằm trên đất theo generator).
    /// </summary>
    public static Vector3 RollSpawnPosition(Vector3 center, float radius)
    {
        if (radius <= 0f)
        {
            return center;
        }

        ResolveSceneReferences();
        TileType[,] map = dungeonManager != null ? dungeonManager.CurrentMap : null;

        Vector3 crowdedFallback = center;
        bool hasCrowdedFallback = false;
        for (int attempt = 0; attempt < OffsetRollTries; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);
            if (!IsUsableGround(map, candidate))
            {
                continue;
            }

            if (IsClearOfUnits(candidate))
            {
                return candidate;
            }

            if (!hasCrowdedFallback)
            {
                crowdedFallback = candidate;
                hasCrowdedFallback = true;
            }
        }

        return crowdedFallback;
    }

    // Tham chiếu scene cache tĩnh - tự tìm lại khi null (lần đầu hoặc sau khi đổi scene).
    private static void ResolveSceneReferences()
    {
        if (dungeonManager == null)
        {
            dungeonManager = Object.FindAnyObjectByType<DungeonManager>();
        }
        if (visualizer == null)
        {
            visualizer = Object.FindAnyObjectByType<TilemapVisualizer>();
        }
    }

    // Scene không có map/visualizer -> mọi điểm đều là nền hợp lệ (giữ hành vi cũ của Phase 2).
    private static bool IsUsableGround(TileType[,] map, Vector3 candidate)
    {
        if (map == null || visualizer == null)
        {
            return true;
        }

        return IsWalkable(map, visualizer.WorldToCell(candidate));
    }

    private static bool IsClearOfUnits(Vector3 candidate)
    {
        int hitCount = Physics2D.OverlapCircle(candidate, UnitClearanceRadius, anyColliderFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            bool isOccupiedByUnit = overlapResults[i].GetComponentInParent<UnitHealth>() != null;
            if (isOccupiedByUnit)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWalkable(TileType[,] map, Vector2Int cell)
    {
        bool inBounds = cell.x >= 0 && cell.y >= 0
            && cell.x < map.GetLength(0) && cell.y < map.GetLength(1);
        if (!inBounds)
        {
            return false;
        }

        TileType tile = map[cell.x, cell.y];
        return tile == TileType.Floor || tile == TileType.SwampWater || tile == TileType.Gate;
    }

    private static ContactFilter2D CreateAnyColliderFilter()
    {
        var filter = new ContactFilter2D();
        filter.NoFilter();
        return filter;
    }
}
