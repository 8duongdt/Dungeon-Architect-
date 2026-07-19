using UnityEngine;

/// <summary>
/// Helper chung cho các spawner rải unit quanh một tâm (cổng, tinh thể bị chiếm): roll offset ngẫu
/// nhiên trong bán kính nhưng CHỈ nhận vị trí rơi trên ô đi lại được (Floor/SwampWater/Gate) của map
/// hiện tại - tránh quái spawn kẹt trong tường (map quần đảo có tường/nước sát cổng). Null-safe:
/// scene không có DungeonManager/map (vd Phase 2) thì trả offset thuần như hành vi cũ.
/// </summary>
public static class SpawnPlacement
{
    // Số lần thử roll offset trước khi bỏ cuộc và spawn ngay tại tâm.
    private const int OffsetRollTries = 8;

    private static DungeonManager dungeonManager;
    private static TilemapVisualizer visualizer;

    /// <summary>Vị trí spawn quanh tâm: offset ngẫu nhiên đầu tiên rơi trên ô đi được; hết lượt
    /// thử thì trả về đúng tâm (tâm cổng/tinh thể luôn nằm trên đất theo generator).</summary>
    public static Vector3 RollSpawnPosition(Vector3 center, float radius)
    {
        if (radius <= 0f)
        {
            return center;
        }

        ResolveSceneReferences();
        TileType[,] map = dungeonManager != null ? dungeonManager.CurrentMap : null;
        if (map == null || visualizer == null)
        {
            Vector2 rawOffset = Random.insideUnitCircle * radius;
            return center + new Vector3(rawOffset.x, rawOffset.y, 0f);
        }

        for (int attempt = 0; attempt < OffsetRollTries; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);
            if (IsWalkable(map, visualizer.WorldToCell(candidate)))
            {
                return candidate;
            }
        }

        return center;
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
}
