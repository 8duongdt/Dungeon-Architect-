using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Rải vật/quái gameplay (rương, quái) lên ô đi-lại-được theo bốn luật spawn:
///   - Vùng an toàn: cấm spawn trong bán kính quanh điểm xuất phát người chơi.
///   - Trọng số (rarity): chọn prefab theo loot table (<see cref="WeightedTable"/>).
///   - Giới hạn số lượng: cap mỗi phòng và cap toàn map.
///   - Gom cụm Perlin: chỉ spawn ở ô có nhiễu Perlin vượt ngưỡng -> vật ra thành cụm tự nhiên.
/// Dùng chung hạt nhân <see cref="MapPlacement"/> với decorator nên mọi vật đều snap đúng tâm ô và
/// không bao giờ kẹt vào tường. Kế thừa <see cref="DungeonDecoratorBase"/> để dùng lại spawn/cleanup.
/// </summary>
public class WeightedScatterSpawner : DungeonDecoratorBase
{
    [Header("Bảng loot (rương / vật phẩm) theo trọng số")]
    [SerializeField] private WeightedPrefab[] lootTable;

    [Header("Bảng quái theo trọng số")]
    [SerializeField] private WeightedPrefab[] enemyTable;

    /// <summary>
    /// Rải loot rồi rải quái theo bốn luật, dùng chung một hạt nhân đặt vật (loot và quái không
    /// chồng ô nhau). Đọc các tham số luật từ <paramref name="data"/>.
    /// </summary>
    public void Scatter(
        TileType[,] map,
        IReadOnlyList<RectInt> rooms,
        Vector2Int playerSpawnCell,
        TilemapVisualizer visualizer,
        DungeonData data)
    {
        if (map == null || data == null || visualizer == null)
        {
            return;
        }

        BeginPlacement(new MapPlacement(map, visualizer));

        var noiseOffset = new Vector2(
            Random.value * NoiseOffsetRange, Random.value * NoiseOffsetRange);

        ScatterTable(lootTable, map, rooms, playerSpawnCell, data, noiseOffset);
        ScatterTable(enemyTable, map, rooms, playerSpawnCell, data, noiseOffset);
    }

    // Biên độ dịch gốc Perlin để mỗi lần sinh cho cụm ở vị trí khác nhau.
    private const float NoiseOffsetRange = 1000f;

    // Rải một bảng prefab: gom ô ứng viên -> trộn -> đặt cho tới khi chạm cap phòng/toàn map.
    private void ScatterTable(
        WeightedPrefab[] table,
        TileType[,] map,
        IReadOnlyList<RectInt> rooms,
        Vector2Int playerSpawnCell,
        DungeonData data,
        Vector2 noiseOffset)
    {
        if (table == null || table.Length == 0)
        {
            return;
        }

        List<Vector2Int> candidates = CollectCandidates(map, playerSpawnCell, data, noiseOffset);
        Shuffle(candidates);

        int[] roomCounts = new int[rooms != null ? rooms.Count : 0];
        int globalCount = 0;

        foreach (Vector2Int cell in candidates)
        {
            if (globalCount >= data.spawnGlobalCap)
            {
                break;
            }

            int roomIndex = FindRoomIndex(rooms, cell);
            if (IsRoomCapped(roomCounts, roomIndex, data.spawnPerRoomCap))
            {
                continue;
            }

            WeightedPrefab? entry = WeightedTable.PickEntry(table);
            if (entry == null)
            {
                continue;
            }

            GameObject instance = SpawnAndOccupy(entry.Value.prefab, cell);
            if (instance == null)
            {
                continue;
            }

            if (entry.Value.isObstacle && instance.GetComponent<Collider2D>() == null)
            {
                var col = instance.AddComponent<BoxCollider2D>();
                SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    col.size = sr.bounds.size;
                }
            }

            globalCount++;
            if (roomIndex >= 0)
            {
                roomCounts[roomIndex]++;
            }
        }
    }

    // Ô ứng viên = còn trống + ngoài vùng an toàn + nhiễu Perlin vượt ngưỡng (gom cụm).
    private List<Vector2Int> CollectCandidates(
        TileType[,] map, Vector2Int playerSpawnCell, DungeonData data, Vector2 noiseOffset)
    {
        var candidates = new List<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsValidSpawnCell(cell, playerSpawnCell, data, noiseOffset))
                {
                    candidates.Add(cell);
                }
            }
        }

        return candidates;
    }

    private bool IsValidSpawnCell(
        Vector2Int cell, Vector2Int playerSpawnCell, DungeonData data, Vector2 noiseOffset)
    {
        if (!Placement.IsFree(cell))
        {
            return false;
        }

        if (MapPlacement.WithinSafeRadius(cell, playerSpawnCell, data.spawnSafeRadius))
        {
            return false;
        }

        return MapPlacement.Perlin(cell, data.spawnPerlinScale, noiseOffset) > data.spawnPerlinThreshold;
    }

    private static bool IsRoomCapped(int[] roomCounts, int roomIndex, int perRoomCap)
    {
        return roomIndex >= 0 && roomCounts[roomIndex] >= perRoomCap;
    }

    // Chỉ số phòng chứa ô (RectInt.Contains); -1 nếu không thuộc phòng nào.
    private static int FindRoomIndex(IReadOnlyList<RectInt> rooms, Vector2Int cell)
    {
        if (rooms == null)
        {
            return -1;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Contains(cell))
            {
                return i;
            }
        }

        return -1;
    }

    private static void Shuffle(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }
    }
}
