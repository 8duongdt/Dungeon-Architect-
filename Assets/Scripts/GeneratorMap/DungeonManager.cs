using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Điều phối sinh map: chọn một <see cref="MapThemeSO"/> -> theo generatorType của theme mà chạy
/// generator phù hợp (Room-First chia phòng / Undead một khu vực vuông to kín) -> vẽ sàn/tường
/// (TilemapVisualizer lấy tile từ theme) -> rải vật trang trí bằng decorator tương ứng -> rải cổng
/// cho map Room-First.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    [Header("Themes (DungeonManager tự chọn 1)")]
    // Mỗi theme gom bộ tile + DungeonData (đã chứa generatorType). DungeonManager chọn 1 để sinh.
    [SerializeField]
    private List<MapThemeSO> themes = new List<MapThemeSO>();

    // -1 = chọn ngẫu nhiên mỗi lần sinh; >=0 = luôn dùng theme tại chỉ số này.
    [SerializeField]
    private int themeIndex = -1;

    [Header("Tham chiếu vẽ & trang trí")]
    [SerializeField]
    private TilemapVisualizer tilemapVisualizer;

    // Trang trí cho map Undead (hazard nước, bộ xương, bàn thờ...).
    [SerializeField]
    private UndeadDecorator undeadDecorator;

    // Trang trí cho map Room-First (đuốc trên tường, rương trên sàn).
    [SerializeField]
    private DungeonDecorator dungeonDecorator;

    [Header("Cổng sinh quái")]
    // Vật chứa các cổng đã sinh (để Hierarchy gọn). Bỏ trống sẽ tự tạo.
    [SerializeField]
    private Transform portalParent;

    private readonly List<GameObject> spawnedPortals = new List<GameObject>();

    /// <summary>Bắn ra sau mỗi lần sinh map xong (để mini-map dựng lại nền từ <see cref="CurrentMap"/>).</summary>
    public event Action DungeonGenerated;

    /// <summary>Lưới ô của lần sinh gần nhất; null nếu chưa sinh. Mini-map đọc để dựng texture nền.</summary>
    public TileType[,] CurrentMap { get; private set; }

    // Tự sinh một dungeon mới mỗi khi vào Phase 1 (Play). Đây cũng là thứ khởi động mini-map:
    // sinh xong sẽ set CurrentMap và bắn DungeonGenerated cho mini-map dựng nền.
    private void Start()
    {
        GenerateDungeon();
    }

    /// <summary>Sinh lại map: chọn theme -> chạy đúng generator -> vẽ -> trang trí -> cổng.</summary>
    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        MapThemeSO theme = PickTheme();
        if (theme == null || !HasRequiredReferences())
        {
            return;
        }

        DungeonData data = theme.dungeonData;
        if (data == null)
        {
            Debug.LogError("[DungeonManager] Theme chưa gán DungeonData.");
            return;
        }

        PrepareForGeneration(theme);

        switch (data.generatorType)
        {
            case DungeonGeneratorType.RoomFirst:
                GenerateRoomFirst(theme, data);
                break;
            case DungeonGeneratorType.UndeadBigRoom:
                GenerateUndead(data);
                break;
        }

        DungeonGenerated?.Invoke();
    }

    // Giữ tên ContextMenu quen thuộc; trỏ về luồng sinh thống nhất.
    [ContextMenu("Start Undead Phase")]
    public void StartUndeadPhase()
    {
        GenerateDungeon();
    }

    // Áp theme cho visualizer rồi dọn tile + vật + cổng của lần sinh trước.
    private void PrepareForGeneration(MapThemeSO theme)
    {
        tilemapVisualizer.SetTheme(theme);
        tilemapVisualizer.Clear();
        undeadDecorator.ClearSpawned();
        dungeonDecorator.ClearSpawned();
        ClearPortals();
    }

    private void GenerateRoomFirst(MapThemeSO theme, DungeonData data)
    {
        var generator = new RoomFirstGenerator(data);
        TileType[,] map = generator.Generate();
        CurrentMap = map;

        HashSet<Vector2Int> floorPositions = CollectPositions(map, TileType.Floor);
        PaintFloorAndWalls(floorPositions);

        dungeonDecorator.Decorate(map, data, tilemapVisualizer);
        SpawnPortals(theme, generator.Rooms);
        PlacePlayerAndCameraAtSpawn(generator.Rooms);
    }

    private void GenerateUndead(DungeonData data)
    {
        var generator = new UndeadGenerator(data);
        TileType[,] map = generator.Generate();
        CurrentMap = map;

        // Ô nước/cổng vẫn tính là sàn để có nền đi lại và tính tường đúng quanh toàn vùng.
        HashSet<Vector2Int> walkablePositions =
            CollectPositions(map, TileType.Floor, TileType.SwampWater, TileType.Gate);
        PaintFloorAndWalls(walkablePositions);

        undeadDecorator.Decorate(map, generator.Rooms, data, tilemapVisualizer);
        PlacePlayerAndCameraAtSpawn(generator.Rooms);
    }

    // Sau khi sinh map ngẫu nhiên, dời người chơi (và camera) về tâm phòng đầu tiên - ô luôn
    // đi được (sàn hoặc cổng) - để tránh avatar bị kẹt trong tường ở vị trí cố định cũ.
    private void PlacePlayerAndCameraAtSpawn(IReadOnlyList<RectInt> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return;
        }

        Vector2Int spawnCell = RoomCenter(rooms[0]);
        Vector3 spawnWorld = tilemapVisualizer.CellToWorldCenter(spawnCell);

        PlayerControll player = FindFirstObjectByType<PlayerControll>();
        if (player != null)
        {
            player.transform.position = new Vector3(spawnWorld.x, spawnWorld.y, player.transform.position.z);
        }

        RTSCamera camera = FindFirstObjectByType<RTSCamera>();
        if (camera != null)
        {
            camera.MoveTo(spawnWorld);
        }
    }

    private static Vector2Int RoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }

    private MapThemeSO PickTheme()
    {
        if (themes == null || themes.Count == 0)
        {
            Debug.LogError("[DungeonManager] Chưa gán theme nào trong danh sách 'themes'.");
            return null;
        }

        if (themeIndex >= 0 && themeIndex < themes.Count)
        {
            return themes[themeIndex];
        }

        return themes[Random.Range(0, themes.Count)];
    }

    private bool HasRequiredReferences()
    {
        if (tilemapVisualizer == null || undeadDecorator == null || dungeonDecorator == null)
        {
            Debug.LogError(
                "[DungeonManager] Thiếu tham chiếu TilemapVisualizer / UndeadDecorator / DungeonDecorator.");
            return false;
        }

        return true;
    }

    // TileType[,] -> HashSet<Vector2Int> các ô thuộc wantedTypes (định dạng visualizer/WallGeneration cần).
    private HashSet<Vector2Int> CollectPositions(TileType[,] map, params TileType[] wantedTypes)
    {
        var positions = new HashSet<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (System.Array.IndexOf(wantedTypes, map[x, y]) >= 0)
                    positions.Add(new Vector2Int(x, y));
            }
        }

        return positions;
    }

    // Vẽ sàn rồi dựng tường quanh vùng đi lại qua pipeline vẽ có sẵn.
    private void PaintFloorAndWalls(HashSet<Vector2Int> floorPositions)
    {
        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGeneration.CreateWalls(floorPositions, tilemapVisualizer);
    }

    // ----- Cổng sinh quái (chỉ map Room-First) -----

    // Rải cổng vào tâm một vài phòng ngẫu nhiên (số lượng lấy từ theme.portalCount).
    private void SpawnPortals(MapThemeSO theme, IReadOnlyList<RectInt> rooms)
    {
        if (theme.portalPrefab == null || theme.portalCount <= 0 || rooms == null || rooms.Count == 0)
        {
            return;
        }

        EnsurePortalParent();

        List<Vector2Int> centers = ShuffledRoomCenters(rooms);
        int portalsToSpawn = Mathf.Min(theme.portalCount, centers.Count);
        for (int i = 0; i < portalsToSpawn; i++)
        {
            Vector3 worldPosition = tilemapVisualizer.CellToWorldCenter(centers[i]);
            GameObject portal = Instantiate(theme.portalPrefab, worldPosition, Quaternion.identity, portalParent);
            spawnedPortals.Add(portal);
        }
    }

    private void EnsurePortalParent()
    {
        if (portalParent == null)
        {
            portalParent = new GameObject("Portals").transform;
            portalParent.SetParent(transform, false);
        }
    }

    // Tâm các phòng, đã trộn ngẫu nhiên -> mỗi phòng tối đa một cổng, vị trí ngẫu nhiên.
    private List<Vector2Int> ShuffledRoomCenters(IReadOnlyList<RectInt> rooms)
    {
        var centers = new List<Vector2Int>(rooms.Count);
        foreach (RectInt room in rooms)
        {
            centers.Add(RoomCenter(room));
        }

        for (int i = centers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (centers[i], centers[j]) = (centers[j], centers[i]);
        }

        return centers;
    }

    private void ClearPortals()
    {
        foreach (GameObject portal in spawnedPortals)
        {
            if (portal == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(portal);
            }
            else
            {
                DestroyImmediate(portal);
            }
        }

        spawnedPortals.Clear();
    }
}
