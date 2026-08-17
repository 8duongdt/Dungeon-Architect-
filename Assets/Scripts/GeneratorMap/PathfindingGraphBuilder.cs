using Pathfinding;
using UnityEngine;

/// <summary>
/// Dựng lại GridGraph của A* Pathfinding Project mỗi lần dungeon sinh xong - map là thủ tục nên
/// không thể bake graph sẵn trong scene. Kích thước/tâm graph lấy từ <see cref="DungeonManager.CurrentMap"/>
/// + <see cref="TilemapVisualizer"/>; vật cản nhận diện bằng physics 2D trên layer của tilemap
/// Collision (tường). Quân Human dùng graph này để hành quân tới tinh thể (CrystalCampaignAgent).
/// </summary>
public class PathfindingGraphBuilder : MonoBehaviour
{
    [Tooltip("Nguồn sự kiện DungeonGenerated + map hiện tại.")]
    [SerializeField] private DungeonManager dungeonManager;

    [Tooltip("Quy đổi ô lưới -> tọa độ world cho tâm graph.")]
    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    [Tooltip("Layer chứa collider tường (Obstacle) - node chạm collider layer này bị coi là không đi được.")]
    [SerializeField] private LayerMask obstacleMask = 1 << 6;

    [Tooltip("Đường kính vòng kiểm tra va chạm mỗi node (theo đơn vị node) - nhỏ hơn 1 để cửa rộng 1 ô vẫn đi lọt.")]
    [SerializeField] private float collisionDiameter = 0.9f;

    private void OnEnable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated += RebuildGraph;
        }
    }

    private void OnDisable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated -= RebuildGraph;
        }
    }

    private void RebuildGraph()
    {
        RebuildFor(dungeonManager.CurrentMap, tilemapVisualizer, obstacleMask, collisionDiameter);
    }

    /// <summary>
    /// Dựng lại GridGraph khớp một map vừa sinh - dùng chung cho Phase 1 (qua sự kiện
    /// DungeonGenerated) lẫn Phase 2 (Phase2MapGenerator gọi thẳng sau khi sinh map riêng).
    /// Null-safe: thiếu map/visualizer/AstarPath thì bỏ qua.
    /// </summary>
    public static void RebuildFor(
        TileType[,] map, TilemapVisualizer visualizer, LayerMask obstacleMask, float collisionDiameter)
    {
        bool hasDependencies = map != null && visualizer != null && AstarPath.active != null;
        if (!hasDependencies)
        {
            return;
        }

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        GridGraph graph = GetOrCreateGridGraph();
        graph.is2D = true;
        graph.SetDimensions(width, height, visualizer.CellSize);
        graph.center = MapCenterWorld(visualizer, width, height);
        graph.collision.use2D = true;
        // A* 4.2 không có ColliderType.Circle - Sphere ở chế độ use2D chính là vòng tròn 2D.
        graph.collision.type = ColliderType.Sphere;
        graph.collision.diameter = collisionDiameter;
        graph.collision.mask = obstacleMask;

        // Tile vừa vẽ xong ngay trên stack này - phải ép collider tường thành hình trước khi quét,
        // nếu không graph sẽ lấy mẫu vật lý của map cũ và coi mọi ô đều đi được.
        visualizer.RefreshWallCollider();
        AstarPath.active.Scan(graph);
    }

    /// <summary>
    /// Lấy mẫu vật lý lại một vùng nhỏ của graph - dùng khi vừa DỰNG thêm vật cản (công trình).
    /// Rẻ hơn Scan toàn map rất nhiều. Null-safe: chưa có AstarPath thì bỏ qua.
    /// </summary>
    public static void UpdateArea(Bounds area)
    {
        if (AstarPath.active == null)
        {
            return;
        }

        // Collider của công trình vừa sinh chưa chắc đã vào không gian vật lý - ép đồng bộ trước.
        Physics2D.SyncTransforms();
        AstarPath.active.UpdateGraphs(area);
    }

    /// <summary>
    /// Mở lại một vùng thành đi được mà KHÔNG lấy mẫu vật lý - dùng khi PHÁ công trình, vì
    /// Destroy chỉ có hiệu lực cuối frame nên quét lại ngay sẽ vẫn thấy collider cũ. An toàn vì
    /// công trình chỉ được đặt trên ô sàn (GridBuildingSystem.IsFloorCell).
    /// </summary>
    public static void MarkAreaWalkable(Bounds area)
    {
        if (AstarPath.active == null)
        {
            return;
        }

        AstarPath.active.UpdateGraphs(new GraphUpdateObject(area)
        {
            modifyWalkability = true,
            setWalkability = true
        });
    }

    private static GridGraph GetOrCreateGridGraph()
    {
        GridGraph existing = AstarPath.active.data.gridGraph;
        return existing != null ? existing : AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
    }

    // Tâm graph = trung điểm giữa tâm ô (0,0) và tâm ô (w-1,h-1) - khớp chính xác lưới tile.
    private static Vector3 MapCenterWorld(TilemapVisualizer visualizer, int width, int height)
    {
        Vector3 min = visualizer.CellToWorldCenter(Vector2Int.zero);
        Vector3 max = visualizer.CellToWorldCenter(new Vector2Int(width - 1, height - 1));
        return (min + max) * 0.5f;
    }
}
