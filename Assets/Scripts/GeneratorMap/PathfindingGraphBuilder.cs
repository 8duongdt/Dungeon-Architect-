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

    [Tooltip("Layer chứa collider tường - node chạm collider layer này bị coi là không đi được.")]
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
        TileType[,] map = dungeonManager.CurrentMap;
        bool hasDependencies = map != null && tilemapVisualizer != null && AstarPath.active != null;
        if (!hasDependencies)
        {
            return;
        }

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        GridGraph graph = GetOrCreateGridGraph();
        graph.is2D = true;
        graph.SetDimensions(width, height, tilemapVisualizer.CellSize);
        graph.center = MapCenterWorld(width, height);
        graph.collision.use2D = true;
        // A* 4.2 không có ColliderType.Circle - Sphere ở chế độ use2D chính là vòng tròn 2D.
        graph.collision.type = ColliderType.Sphere;
        graph.collision.diameter = collisionDiameter;
        graph.collision.mask = obstacleMask;

        AstarPath.active.Scan(graph);
    }

    private GridGraph GetOrCreateGridGraph()
    {
        GridGraph existing = AstarPath.active.data.gridGraph;
        return existing != null ? existing : AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
    }

    // Tâm graph = trung điểm giữa tâm ô (0,0) và tâm ô (w-1,h-1) - khớp chính xác lưới tile.
    private Vector3 MapCenterWorld(int width, int height)
    {
        Vector3 min = tilemapVisualizer.CellToWorldCenter(Vector2Int.zero);
        Vector3 max = tilemapVisualizer.CellToWorldCenter(new Vector2Int(width - 1, height - 1));
        return (min + max) * 0.5f;
    }
}
