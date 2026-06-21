using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Điều phối toàn bộ quá trình sinh dungeon cho Phase 1:
/// sinh dữ liệu (DungeonGenerator) -> vẽ map (TilemapVisualizer cũ) -> rải vật (DungeonDecorator).
/// Tái sử dụng pipeline vẽ tường có sẵn (WallGeneration) để giữ tương thích ngược.
/// </summary>
public class DungeonManager : MonoBehaviour
{
    // Danh sách "thể loại map". Mỗi lần sinh sẽ random chọn 1 theme để ra map khác nhau.
    [SerializeField]
    private List<MapThemeSO> mapThemes = new List<MapThemeSO>();

    // DungeonData dự phòng: dùng khi danh sách theme trống hoặc theme không gán DungeonData.
    [SerializeField]
    private DungeonData fallbackDungeonData;

    // Visualizer cũ - KHÔNG chỉnh sửa, chỉ gọi qua các public method có sẵn.
    [SerializeField]
    private TilemapVisualizer tilemapVisualizer;

    [SerializeField]
    private DungeonDecorator dungeonDecorator;

    [Header("Undead Swamp Phase")]
    // Cấu hình riêng cho phase Undead Swamp (kích thước, phòng, tỉ lệ nước...). Bỏ trống thì dùng fallback.
    [SerializeField]
    private DungeonData undeadDungeonData;

    [SerializeField]
    private UndeadDecorator undeadDecorator;

    // Lớp Tilemap riêng để phủ nước đầm lầy - tách khỏi floor/wall của TilemapVisualizer (không sửa visualizer).
    [SerializeField]
    private Tilemap swampTilemap;

    [SerializeField]
    private TileBase swampTile;

    /// <summary>
    /// Sinh lại dungeon cho Phase 1: random 1 theme, dọn map+vật cũ, sinh mảng mới, vẽ và rải vật.
    /// </summary>
    [ContextMenu("Start Phase One")]
    public void StartPhaseOne()
    {
        if (tilemapVisualizer == null || dungeonDecorator == null)
        {
            Debug.LogError("[DungeonManager] Thiếu tham chiếu TilemapVisualizer / DungeonDecorator.");
            return;
        }

        // 1. Random chọn 1 thể loại map và xác định DungeonData sẽ dùng.
        MapThemeSO theme = PickRandomTheme();
        DungeonData data = ResolveDungeonData(theme);
        if (data == null)
        {
            Debug.LogError("[DungeonManager] Không có DungeonData (theme lẫn fallback đều trống).");
            return;
        }

        // 2. Dọn tile và vật trang trí của lần trước.
        tilemapVisualizer.Clear();
        dungeonDecorator.ClearDecorations();

        // 3. Áp dụng diện mạo + prefab của theme (nếu có).
        if (theme != null)
        {
            tilemapVisualizer.ApplyTheme(theme);
            dungeonDecorator.SetDecorationPrefabs(theme.torchPrefab, theme.chestPrefab);
        }

        // 4. Sinh mảng TileType[,] mới.
        var generator = new DungeonGenerator(data);
        TileType[,] map = generator.Generate();

        // 5. Chuyển sang tập floor và vẽ qua visualizer cũ (vẽ sàn + tường).
        HashSet<Vector2Int> floorPositions = CollectFloorPositions(map);
        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGeneration.CreateWalls(floorPositions, tilemapVisualizer);

        // 6. Rải vật trang trí.
        dungeonDecorator.Decorate(map, data, tilemapVisualizer);
    }

    // Random 1 theme còn hợp lệ; trả null nếu danh sách trống (sẽ dùng fallback).
    private MapThemeSO PickRandomTheme()
    {
        var validThemes = mapThemes.FindAll(t => t != null);
        if (validThemes.Count == 0)
            return null;
        return validThemes[Random.Range(0, validThemes.Count)];
    }

    private DungeonData ResolveDungeonData(MapThemeSO theme)
    {
        if (theme != null && theme.dungeonData != null)
            return theme.dungeonData;
        return fallbackDungeonData;
    }

    // Bộ chuyển đổi: TileType[,] -> HashSet<Vector2Int> các ô sàn,
    // đúng định dạng mà TilemapVisualizer/WallGeneration cũ mong đợi.
    private HashSet<Vector2Int> CollectFloorPositions(TileType[,] map)
    {
        var floorPositions = new HashSet<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == TileType.Floor)
                    floorPositions.Add(new Vector2Int(x, y));
            }
        }

        return floorPositions;
    }

    // ===================== Undead Swamp Phase =====================

    /// <summary>
    /// Sinh map Undead Swamp (Room-First, vuông, thoáng cho nhân vật tầm xa):
    /// dọn map+vật cũ -> sinh layout -> vẽ sàn/tường (visualizer cũ) + phủ nước -> rải hazard/vật/cổng.
    /// Không đụng tới pipeline Cellular Automata của StartPhaseOne.
    /// </summary>
    [ContextMenu("Start Undead Swamp Phase")]
    public void StartUndeadSwampPhase()
    {
        if (tilemapVisualizer == null || undeadDecorator == null)
        {
            Debug.LogError("[DungeonManager] Thiếu tham chiếu TilemapVisualizer / UndeadDecorator.");
            return;
        }

        DungeonData data = undeadDungeonData != null ? undeadDungeonData : fallbackDungeonData;
        if (data == null)
        {
            Debug.LogError("[DungeonManager] Không có DungeonData cho Undead Swamp (cả undeadDungeonData lẫn fallback đều trống).");
            return;
        }

        // 1. Dọn tile cũ (floor/wall qua visualizer cũ + lớp nước) và vật của cả hai pipeline.
        tilemapVisualizer.Clear();
        ClearSwampTiles();
        dungeonDecorator?.ClearDecorations();
        undeadDecorator.ClearProps();

        // 2. Sinh ma trận layout kiểu Room-First.
        var generator = new UndeadSwampGenerator(data);
        TileType[,] map = generator.Generate();

        // 3. Vẽ sàn + tường qua visualizer cũ. Ô nước/cổng vẫn tính là sàn để có nền đi lại
        //    và để tường được tính đúng quanh toàn bộ vùng walkable.
        HashSet<Vector2Int> floorPositions = CollectWalkablePositions(map);
        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGeneration.CreateWalls(floorPositions, tilemapVisualizer);

        // 4. Phủ tile nước lên các ô SwampWater (lớp Tilemap riêng).
        PaintSwampTiles(map);

        // 5. Rải hazard nước, cây/sọ trên sàn, và cổng ở phòng cuối.
        undeadDecorator.Decorate(map, tilemapVisualizer);
    }

    // Vùng đi lại được = Floor + SwampWater + Gate (nước và cổng đều nằm trên nền sàn).
    private HashSet<Vector2Int> CollectWalkablePositions(TileType[,] map)
    {
        var positions = new HashSet<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType tile = map[x, y];
                if (tile == TileType.Floor || tile == TileType.SwampWater || tile == TileType.Gate)
                    positions.Add(new Vector2Int(x, y));
            }
        }

        return positions;
    }

    // Phủ swampTile lên đúng các ô SwampWater. Dùng cùng quy ước tọa độ như visualizer cũ.
    private void PaintSwampTiles(TileType[,] map)
    {
        if (swampTilemap == null || swampTile == null)
            return;

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] != TileType.SwampWater)
                    continue;

                var worldCell = new Vector3Int(x, y, 0);
                swampTilemap.SetTile(swampTilemap.WorldToCell(worldCell), swampTile);
            }
        }
    }

    private void ClearSwampTiles()
    {
        if (swampTilemap != null)
            swampTilemap.ClearAllTiles();
    }
}
