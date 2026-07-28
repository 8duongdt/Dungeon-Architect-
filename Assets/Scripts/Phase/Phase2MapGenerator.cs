using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Sinh map Room-First NGẪU NHIÊN cho Phase 2 thay cho map tĩnh vẽ tay: mỗi lần vào trận là một
/// bố cục phòng + hành lang khác nhau. Người chơi xuất phát ở tâm phòng đầu tiên; các cổng sinh
/// quái được dời vào tâm những phòng XA người chơi nhất - quái phải hành quân qua hành lang mới
/// tới nơi, cho người chơi thời gian thở giữa các đợt.
///
/// Dùng lại trọn pipeline có sẵn: <see cref="RoomFirstGenerator"/> (layout),
/// <see cref="TilemapVisualizer"/> + <see cref="WallGeneration"/> (vẽ),
/// <see cref="PathfindingGraphBuilder.RebuildFor"/> (dựng lại graph A* cho quái tìm đường).
/// Null-safe: thiếu theme/visualizer thì giữ nguyên map tĩnh trong scene, chỉ cảnh báo.
/// </summary>
public class Phase2MapGenerator : MonoBehaviour
{
    [Header("Cấu hình sinh map")]
    [Tooltip("Theme lấy bộ tile sàn/tường.")]
    [SerializeField] private MapThemeSO theme;

    [Tooltip("DungeonData riêng cho Phase 2 - để trống sẽ dùng dungeonData của theme.")]
    [SerializeField] private DungeonData overrideData;

    [Header("Tham chiếu vẽ")]
    [SerializeField] private TilemapVisualizer tilemapVisualizer;

    [Tooltip("Các tilemap vẽ tay của map tĩnh cũ cần xóa trước khi sinh (vd WalkBehind).")]
    [SerializeField] private Tilemap[] extraTilemapsToClear;

    [Header("Bố trí người chơi & cổng quái")]
    [Tooltip("Người chơi - được dời về tâm phòng đầu tiên sau khi sinh map.")]
    [SerializeField] private Transform player;

    [Tooltip("Các cổng sinh quái - dời vào tâm những phòng xa người chơi nhất.")]
    [SerializeField] private List<EnemySpawner> gates = new List<EnemySpawner>();

    [Header("A* Pathfinding")]
    [Tooltip("Layer chứa collider tường - node chạm layer này bị coi là không đi được.")]
    [SerializeField] private LayerMask obstacleMask = 1 << 6;

    [Tooltip("Đường kính vòng kiểm tra va chạm mỗi node (theo đơn vị node).")]
    [SerializeField] private float collisionDiameter = 0.9f;

    private void Start()
    {
        if (theme == null || tilemapVisualizer == null)
        {
            Debug.LogWarning("[Phase2MapGenerator] Thiếu theme hoặc TilemapVisualizer - giữ map tĩnh của scene.");
            return;
        }

        DungeonData data = overrideData != null ? overrideData : theme.dungeonData;
        if (data == null)
        {
            Debug.LogWarning("[Phase2MapGenerator] Không có DungeonData - giữ map tĩnh của scene.");
            return;
        }

        GenerateAndArrange(data);
    }

    private void GenerateAndArrange(DungeonData data)
    {
        ClearOldMap();

        var generator = new RoomFirstGenerator(data);
        TileType[,] map = generator.Generate();

        PaintMap(map);

        Vector2Int playerCell = PlacePlayerInFirstRoom(generator.Rooms);
        PlaceGatesInFarthestRooms(generator.Rooms, playerCell);

        PathfindingGraphBuilder.RebuildFor(map, tilemapVisualizer, obstacleMask, collisionDiameter);
    }

    private void ClearOldMap()
    {
        tilemapVisualizer.SetTheme(theme);
        tilemapVisualizer.Clear();

        if (extraTilemapsToClear == null)
        {
            return;
        }

        foreach (Tilemap tilemap in extraTilemapsToClear)
        {
            if (tilemap != null)
            {
                tilemap.ClearAllTiles();
            }
        }
    }

    private void PaintMap(TileType[,] map)
    {
        HashSet<Vector2Int> floorPositions = CollectFloorPositions(map);
        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGeneration.CreateWalls(floorPositions, tilemapVisualizer);
    }

    private static HashSet<Vector2Int> CollectFloorPositions(TileType[,] map)
    {
        var positions = new HashSet<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == TileType.Floor)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
        }

        return positions;
    }

    // Dời người chơi về tâm phòng đầu tiên (ô luôn là sàn); trả về ô đó làm mốc đo khoảng cách.
    private Vector2Int PlacePlayerInFirstRoom(IReadOnlyList<RectInt> rooms)
    {
        if (rooms.Count == 0)
        {
            return Vector2Int.zero;
        }

        Vector2Int spawnCell = RoomCenter(rooms[0]);
        if (player != null)
        {
            Vector3 spawnWorld = tilemapVisualizer.CellToWorldCenter(spawnCell);
            player.position = new Vector3(spawnWorld.x, spawnWorld.y, player.position.z);
        }

        return spawnCell;
    }

    // Mỗi cổng một phòng riêng: cổng đầu vào phòng xa người chơi nhất, các cổng sau chọn phòng
    // (trong nửa xa) TỎA XA các cổng đã đặt nhất (greedy max-min) - quái đổ về từ nhiều hướng
    // nhưng vẫn phải hành quân một quãng, cho người chơi thời gian thở. Ít phòng hơn số cổng thì
    // cổng thừa dùng lại phòng xa nhất.
    private void PlaceGatesInFarthestRooms(IReadOnlyList<RectInt> rooms, Vector2Int playerCell)
    {
        List<Vector2Int> candidates = FarHalfCenters(rooms, playerCell);
        if (candidates.Count == 0)
        {
            return;
        }

        var chosen = new List<Vector2Int>();
        for (int i = 0; i < gates.Count; i++)
        {
            EnemySpawner gate = gates[i];
            if (gate == null)
            {
                continue;
            }

            Vector2Int cell = PickDispersedCell(candidates, chosen);
            chosen.Add(cell);
            gate.transform.position = tilemapVisualizer.CellToWorldCenter(cell);
        }
    }

    // Tâm các phòng thuộc "nửa xa" (khoảng cách tới người chơi >= 1/2 khoảng cách xa nhất, bỏ phòng
    // xuất phát), xếp giảm dần theo khoảng cách - đảm bảo không cổng nào mở sát người chơi.
    private static List<Vector2Int> FarHalfCenters(IReadOnlyList<RectInt> rooms, Vector2Int playerCell)
    {
        var centers = new List<Vector2Int>();
        for (int i = 0; i < rooms.Count; i++)
        {
            bool isPlayerRoom = i == 0 && rooms.Count > 1;
            if (!isPlayerRoom)
            {
                centers.Add(RoomCenter(rooms[i]));
            }
        }

        centers.Sort((a, b) =>
            ((Vector2)(b - playerCell)).sqrMagnitude.CompareTo(((Vector2)(a - playerCell)).sqrMagnitude));

        if (centers.Count == 0)
        {
            return centers;
        }

        float maxDistance = ((Vector2)(centers[0] - playerCell)).magnitude;
        float minAllowed = maxDistance * 0.5f;
        List<Vector2Int> farHalf = centers.FindAll(
            center => ((Vector2)(center - playerCell)).magnitude >= minAllowed);
        return farHalf.Count > 0 ? farHalf : centers;
    }

    // Chưa có cổng nào -> lấy phòng xa nhất; có rồi -> lấy phòng chưa dùng có khoảng cách tới cổng
    // GẦN NHẤT là lớn nhất (max-min) để các cổng tỏa đều. Hết phòng trống thì dùng lại phòng xa nhất.
    private static Vector2Int PickDispersedCell(List<Vector2Int> candidates, List<Vector2Int> chosen)
    {
        if (chosen.Count == 0)
        {
            return candidates[0];
        }

        Vector2Int best = candidates[0];
        float bestScore = float.MinValue;
        foreach (Vector2Int candidate in candidates)
        {
            if (chosen.Contains(candidate))
            {
                continue;
            }

            float minDistanceToChosen = float.MaxValue;
            foreach (Vector2Int gateCell in chosen)
            {
                minDistanceToChosen = Mathf.Min(
                    minDistanceToChosen, ((Vector2)(candidate - gateCell)).sqrMagnitude);
            }

            if (minDistanceToChosen > bestScore)
            {
                bestScore = minDistanceToChosen;
                best = candidate;
            }
        }

        return best;
    }

    private static Vector2Int RoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }
}
