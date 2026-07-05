using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Rải tinh thể tài nguyên (Gold/Mana/Progress) lên map theo SỐ LƯỢNG CỐ ĐỊNH mỗi loại - khác
/// <see cref="WeightedScatterSpawner"/> (rải theo trọng số/rarity) vì tinh thể cần đúng số lượng
/// từng loại thay vì rút thăm ngẫu nhiên. Dùng chung hạt nhân đặt vật MapPlacement/Placement với
/// decorator nên không bao giờ chồng ô hay kẹt tường. Kế thừa <see cref="DungeonDecoratorBase"/>
/// để dùng lại spawn/cleanup.
/// </summary>
public class CrystalScatterSpawner : DungeonDecoratorBase
{
    [Header("Prefab theo loại (config/art nằm sẵn trên từng prefab, chỉ kích cỡ gán qua Configure() sau khi spawn)")]
    [SerializeField] private GameObject goldCrystalPrefab;
    [SerializeField] private GameObject manaCrystalPrefab;
    [SerializeField] private GameObject progressCrystalPrefab;

    [Header("Số lượng rải mỗi loại")]
    [Min(0)] [SerializeField] private int goldNodeCount = 3;
    [Min(0)] [SerializeField] private int manaNodeCount = 2;
    [Min(0)] [SerializeField] private int progressNodeCount = 1;

    public void Scatter(
        TileType[,] map,
        IReadOnlyList<RectInt> rooms,
        Vector2Int playerSpawnCell,
        TilemapVisualizer visualizer,
        DungeonData data)
    {
        bool hasAllPrefabs = goldCrystalPrefab != null && manaCrystalPrefab != null && progressCrystalPrefab != null;
        if (map == null || data == null || visualizer == null || !hasAllPrefabs)
        {
            return;
        }

        BeginPlacement(new MapPlacement(map, visualizer));

        List<Vector2Int> candidates = CollectCandidates(map, playerSpawnCell, data);
        Shuffle(candidates);

        int nextCandidateIndex = 0;
        nextCandidateIndex = SpawnType(candidates, nextCandidateIndex, goldCrystalPrefab, goldNodeCount);
        nextCandidateIndex = SpawnType(candidates, nextCandidateIndex, manaCrystalPrefab, manaNodeCount);
        SpawnType(candidates, nextCandidateIndex, progressCrystalPrefab, progressNodeCount);
    }

    // Rải một loại tinh thể, bắt đầu từ startIndex trong danh sách ứng viên đã trộn - mỗi loại
    // dùng một đoạn riêng của danh sách nên không bao giờ chọn trùng ô giữa các loại.
    private int SpawnType(List<Vector2Int> candidates, int startIndex, GameObject prefab, int count)
    {
        int spawnedCount = 0;
        int index = startIndex;
        while (spawnedCount < count && index < candidates.Count)
        {
            Vector2Int cell = candidates[index];
            index++;

            if (!Placement.IsFree(cell))
            {
                continue;
            }

            GameObject instance = SpawnAndOccupy(prefab, cell);
            CrystalNode node = instance != null ? instance.GetComponent<CrystalNode>() : null;
            if (node == null)
            {
                continue;
            }

            CrystalNode.SizeTier tier = Random.value < 0.5f ? CrystalNode.SizeTier.Small : CrystalNode.SizeTier.Large;
            node.Configure(tier);
            spawnedCount++;
        }

        return index;
    }

    private List<Vector2Int> CollectCandidates(TileType[,] map, Vector2Int playerSpawnCell, DungeonData data)
    {
        var candidates = new List<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (IsValidSpawnCell(cell, playerSpawnCell, data))
                {
                    candidates.Add(cell);
                }
            }
        }

        return candidates;
    }

    private bool IsValidSpawnCell(Vector2Int cell, Vector2Int playerSpawnCell, DungeonData data)
    {
        if (!Placement.IsFree(cell))
        {
            return false;
        }

        return !MapPlacement.WithinSafeRadius(cell, playerSpawnCell, data.spawnSafeRadius);
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
