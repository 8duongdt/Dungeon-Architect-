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

    // Rải vật/quái gameplay theo trọng số + luật spawn (an toàn/cap/Perlin). Có thể bỏ trống.
    [SerializeField]
    private WeightedScatterSpawner scatterSpawner;

    // Rải tinh thể tài nguyên (Gold/Mana/Progress) theo số lượng cố định mỗi loại. Có thể bỏ trống.
    [SerializeField]
    private CrystalScatterSpawner crystalSpawner;

    [Header("Cỡ map theo tiến độ kỹ năng")]
    // Cây kỹ năng - dùng suy ra cỡ map (càng gần full càng to). Bỏ trống -> luôn cỡ 1 (nhỏ).
    [SerializeField]
    private SkillTreeSO skillTree;

    // Hệ số nhân cạnh map theo cỡ (index = cỡ-1): cỡ 1 = gốc, cỡ 2/3 to dần.
    [SerializeField]
    private float[] mapSizeFactorByLevel = { 1f, 1.3f, 1.7f };

    // Hệ số nhân số đảo theo cỡ (giữ mật độ đảo khi map to ra ~ theo diện tích).
    [SerializeField]
    private float[] islandCountFactorByLevel = { 1f, 1.7f, 2.9f };

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

        DungeonData baseData = theme.dungeonData;
        if (baseData == null)
        {
            Debug.LogError("[DungeonManager] Theme chưa gán DungeonData.");
            return;
        }

        PrepareForGeneration(theme);

        // Cỡ map + số cổng suy ra từ tiến độ cây kỹ năng. Làm việc trên BẢN SAO DungeonData (không
        // mutate asset gốc - sửa runtime sẽ ghi đè asset trong editor); hủy bản sao sau khi sinh xong.
        int sizeLevel = MapSizeProgression.LevelFor(skillTree);
        DungeonData data = CreateSizedData(baseData, sizeLevel);
        int portalCount = sizeLevel;

        switch (data.generatorType)
        {
            case DungeonGeneratorType.RoomFirst:
                GenerateRoomFirst(theme, data, portalCount);
                break;
            case DungeonGeneratorType.UndeadBigRoom:
                GenerateUndead(theme, data, portalCount);
                break;
        }

        Destroy(data);
        DungeonGenerated?.Invoke();
    }

    // Bản sao DungeonData đã scale cạnh map + số đảo theo cỡ (1..3). Bản sao là instance runtime nên
    // sửa thoải mái không đụng asset gốc.
    private DungeonData CreateSizedData(DungeonData baseData, int sizeLevel)
    {
        DungeonData sized = Instantiate(baseData);
        int index = Mathf.Clamp(sizeLevel - 1, 0, MapSizeProgression.MaxLevel - 1);
        float sizeFactor = FactorAt(mapSizeFactorByLevel, index);
        float islandFactor = FactorAt(islandCountFactorByLevel, index);

        sized.squareMapSize = Mathf.Max(DungeonData.MinRoomDimension, Mathf.RoundToInt(baseData.squareMapSize * sizeFactor));
        sized.islandCount = Mathf.Max(1, Mathf.RoundToInt(baseData.islandCount * islandFactor));
        sized.mapWidth = Mathf.Max(DungeonData.MinRoomDimension, Mathf.RoundToInt(baseData.mapWidth * sizeFactor));
        sized.mapHeight = Mathf.Max(DungeonData.MinRoomDimension, Mathf.RoundToInt(baseData.mapHeight * sizeFactor));
        return sized;
    }

    // Đọc hệ số tại cỡ; mảng thiếu phần tử thì coi như 1 (không scale) để không văng.
    private static float FactorAt(float[] factors, int index)
    {
        if (factors == null || index < 0 || index >= factors.Length)
        {
            return 1f;
        }
        return factors[index];
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
        if (scatterSpawner != null)
        {
            scatterSpawner.ClearSpawned();
        }
        if (crystalSpawner != null)
        {
            crystalSpawner.ClearSpawned();
        }
        ClearPortals();
    }

    private void GenerateRoomFirst(MapThemeSO theme, DungeonData data, int portalCount)
    {
        var generator = new RoomFirstGenerator(data);
        TileType[,] map = generator.Generate();
        CurrentMap = map;

        HashSet<Vector2Int> floorPositions = CollectPositions(map, TileType.Floor);
        PaintFloorAndWalls(floorPositions);

        dungeonDecorator.Decorate(map, data, tilemapVisualizer);
        SpawnPortals(theme, generator.Rooms, portalCount);

        Vector2Int spawnCell = PlacePlayerAndCameraAtSpawn(generator.Rooms);
        ScatterGameplayObjects(map, generator.Rooms, spawnCell, data);
        ScatterCrystalsAndActivateNearestGold(map, generator.Rooms, spawnCell, data);
    }

    private void GenerateUndead(MapThemeSO theme, DungeonData data, int portalCount)
    {
        var generator = new UndeadGenerator(data);
        TileType[,] map = generator.Generate();
        CurrentMap = map;

        PaintUndeadTerrain(map);

        undeadDecorator.Decorate(map, generator.Rooms, data, tilemapVisualizer);
        SpawnPortals(theme, generator.Rooms, portalCount);

        Vector2Int spawnCell = PlacePlayerAndCameraAtSpawn(generator.Rooms);
        ScatterGameplayObjects(map, generator.Rooms, spawnCell, data);
        ScatterCrystalsAndActivateNearestGold(map, generator.Rooms, spawnCell, data);
    }

    // Vẽ địa hình map Undead: sàn + tường, tile nước, và viền bờ bao quanh nước.
    private void PaintUndeadTerrain(TileType[,] map)
    {
        // Sàn vẽ dưới MỌI ô đi lại được (gồm cả ô nước - nước phủ tile riêng đè lên sàn).
        HashSet<Vector2Int> floorPaintPositions =
            CollectPositions(map, TileType.Floor, TileType.SwampWater, TileType.Gate);
        tilemapVisualizer.PaintFloorTiles(floorPaintPositions);

        // Tường ring ngoài: coi nước/cổng là "trong" để chỉ dựng tường ở viền ngoài map.
        WallGeneration.CreateWalls(floorPaintPositions, tilemapVisualizer);

        // Nước = tile riêng phủ trên sàn (vật cản mềm: làm chậm qua hazard prefab, không có collider).
        HashSet<Vector2Int> waterCells = CollectPositions(map, TileType.SwampWater);
        tilemapVisualizer.PaintWaterTiles(waterCells);

        // Viền bờ bao quanh nước - vẽ trên layer trang trí nên không chặn di chuyển.
        ShoreGeneration.CreateShoreTiles(CollectShoreCells(map), waterCells, tilemapVisualizer);
    }

    // Viền bờ = ô Floor có ÍT NHẤT một trong 8 hàng xóm là SwampWater -> vẽ tile bờ cho mép nước tự nhiên.
    private HashSet<Vector2Int> CollectShoreCells(TileType[,] map)
    {
        var shore = new HashSet<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == TileType.Floor && HasSwampNeighbor(map, x, y, width, height))
                {
                    shore.Add(new Vector2Int(x, y));
                }
            }
        }

        return shore;
    }

    private static bool HasSwampNeighbor(TileType[,] map, int x, int y, int width, int height)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nx = x + dx;
                int ny = y + dy;
                bool inBounds = nx >= 0 && ny >= 0 && nx < width && ny < height;
                if (inBounds && map[nx, ny] == TileType.SwampWater)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ScatterGameplayObjects(
        TileType[,] map, IReadOnlyList<RectInt> rooms, Vector2Int spawnCell, DungeonData data)
    {
        if (scatterSpawner == null)
        {
            return;
        }

        scatterSpawner.Scatter(map, rooms, spawnCell, tilemapVisualizer, data);
    }

    // Rải tinh thể tài nguyên rồi kích hoạt sẵn tinh thể Gold gần điểm xuất phát nhất, để người
    // chơi có thể xây máy đào ngay mà không phải tự tìm/kích hoạt tinh thể đầu tiên.
    private void ScatterCrystalsAndActivateNearestGold(
        TileType[,] map, IReadOnlyList<RectInt> rooms, Vector2Int spawnCell, DungeonData data)
    {
        if (crystalSpawner == null)
        {
            return;
        }

        crystalSpawner.Scatter(map, rooms, spawnCell, tilemapVisualizer, data);

        Vector3 spawnWorld = tilemapVisualizer.CellToWorldCenter(spawnCell);
        CrystalNode nearestGold = CrystalNodeRegistry.FindNearestOfType(spawnWorld, CrystalType.Gold);
        nearestGold?.Activate();
    }

    // Sau khi sinh map ngẫu nhiên, dời người chơi (và camera) về tâm phòng đầu tiên - ô luôn
    // đi được (sàn hoặc cổng) - để tránh avatar bị kẹt trong tường ở vị trí cố định cũ.
    // Trả về ô xuất phát (làm tâm vùng an toàn cho luật spawn).
    private Vector2Int PlacePlayerAndCameraAtSpawn(IReadOnlyList<RectInt> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            return Vector2Int.zero;
        }

        Vector2Int spawnCell = RoomCenter(rooms[0]);
        Vector3 spawnWorld = tilemapVisualizer.CellToWorldCenter(spawnCell);

        PlayerControll player = FindAnyObjectByType<PlayerControll>();
        if (player != null)
        {
            player.transform.position = new Vector3(spawnWorld.x, spawnWorld.y, player.transform.position.z);
        }

        RTSCamera camera = FindAnyObjectByType<RTSCamera>();
        if (camera != null)
        {
            camera.MoveTo(spawnWorld);
        }

        return spawnCell;
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

    // ----- Cổng sinh quái (mọi pipeline có phòng: Room-First + Undead) -----

    // Nhịp spawn TOÀN bản đồ: con đầu tiên xuất hiện sau 60s, sau đó cứ 25s một cổng ra đợt.
    private const float FirstSpawnDelaySeconds = 60f;
    private const float MapWideWaveIntervalSeconds = 25f;

    // Rải cổng vào tâm một vài phòng ngẫu nhiên (số lượng lấy từ theme.portalCount).
    private void SpawnPortals(MapThemeSO theme, IReadOnlyList<RectInt> rooms, int portalCount)
    {
        if (theme.portalPrefab == null || portalCount <= 0 || rooms == null || rooms.Count == 0)
        {
            return;
        }

        EnsurePortalParent();

        List<Vector2Int> centers = ShuffledRoomCenters(rooms);
        int portalsToSpawn = Mathf.Min(portalCount, centers.Count);
        for (int i = 0; i < portalsToSpawn; i++)
        {
            Vector3 worldPosition = tilemapVisualizer.CellToWorldCenter(centers[i]);
            GameObject portal = Instantiate(theme.portalPrefab, worldPosition, Quaternion.identity, portalParent);
            StaggerPortalSchedule(portal, portalIndex: i, portalTotal: portalsToSpawn);
            spawnedPortals.Add(portal);
        }
    }

    // So le lịch các cổng theo round-robin: cổng i chờ thêm i chu kỳ rồi lặp với chu kỳ
    // (interval x số cổng) - nhờ vậy toàn bản đồ mỗi 25s chỉ đúng một cổng ra đợt.
    private void StaggerPortalSchedule(GameObject portal, int portalIndex, int portalTotal)
    {
        var spawner = portal.GetComponentInChildren<EnemySpawner>();
        if (spawner == null)
        {
            return;
        }

        float firstWaveDelay = FirstSpawnDelaySeconds + MapWideWaveIntervalSeconds * portalIndex;
        spawner.OverrideSchedule(firstWaveDelay, MapWideWaveIntervalSeconds * portalTotal);
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
