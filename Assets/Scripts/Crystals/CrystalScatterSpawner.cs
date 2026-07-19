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

    [Header("Chỗ trống để XÂY quanh tinh thể")]
    [Tooltip("Bán kính (ô) quanh tinh thể được xét xem có đủ đất để đặt công trình hay không.")]
    [Min(0)] [SerializeField] private int buildableClearanceRadius = 3;

    [Tooltip("Tỉ lệ ô ĐẤT tối thiểu trong bán kính trên (0.75 = 75% là đất) thì mới ưu tiên đặt tinh thể ở đó.")]
    [Range(0f, 1f)] [SerializeField] private float minLandRatio = 0.75f;

    // Tinh thể chỉ rải trên ĐẤT LIỀN (Floor): loại nước (SwampWater) và cổng (Gate). Trên map đảo,
    // luật này giữ tinh thể ở giữa đảo, không rơi xuống vành nước bao quanh.
    private static readonly TileType[] LandOnlyWalkable = { TileType.Floor };

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

        BeginPlacement(new MapPlacement(map, visualizer, LandOnlyWalkable));

        // Danh sách đã được trộn + xếp ưu tiên sẵn trong CollectCandidates (ô nhiều đất xung quanh
        // trước, ô sát mép nước sau) - KHÔNG trộn lại ở đây kẻo mất thứ tự ưu tiên.
        List<Vector2Int> candidates = CollectCandidates(map, playerSpawnCell, data);

        int nextCandidateIndex = 0;
        nextCandidateIndex = SpawnType(candidates, nextCandidateIndex, goldCrystalPrefab, goldNodeCount);
        nextCandidateIndex = SpawnType(candidates, nextCandidateIndex, manaCrystalPrefab, manaNodeCount);
        SpawnType(candidates, nextCandidateIndex, progressCrystalPrefab, progressNodeCount);
    }

    /// <summary>
    /// Ngoài phần dọn chuẩn của lớp cơ sở, quét thêm TINH THỂ MỒ CÔI nằm ở GỐC scene: đó là rác
    /// baked từ những phiên Play cũ (khi decorationParent chưa được gán nên vật spawn rơi thẳng ra
    /// gốc, ClearSpawned không với tới). Chúng đứng ở toạ độ của map cũ nên hay "trôi" giữa biển khi
    /// map đổi kích thước/bố cục. Quét ở đây để mỗi lần sinh map là tự sạch.
    /// </summary>
    protected override void OnCleared()
    {
        foreach (CrystalNode stray in FindObjectsByType<CrystalNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Tinh thể do spawner này sinh ra luôn nằm dưới vật chứa; còn nằm ở gốc scene = rác cũ.
            if (stray != null && stray.transform.parent == null)
            {
                DestroyObject(stray.gameObject);
            }
        }
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

    /// <summary>
    /// Ứng viên xếp theo ƯU TIÊN: trước hết là các ô đất còn nhiều đất xung quanh (đủ chỗ đặt công
    /// trình quanh tinh thể), sau đó mới tới các ô đất còn lại làm dự phòng. Nhờ hai tầng này, trên
    /// đảo nhỏ vẫn luôn rải đủ số tinh thể (nhất là cục Progress - đường thắng duy nhất của Phase 1)
    /// thay vì bỏ trống vì không ô nào đạt chuẩn.
    /// </summary>
    private List<Vector2Int> CollectCandidates(TileType[,] map, Vector2Int playerSpawnCell, DungeonData data)
    {
        var preferred = new List<Vector2Int>();
        var fallback = new List<Vector2Int>();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!IsValidSpawnCell(cell, playerSpawnCell, data))
                {
                    continue;
                }

                if (HasBuildableLandAround(map, cell))
                {
                    preferred.Add(cell);
                }
                else
                {
                    fallback.Add(cell);
                }
            }
        }

        Shuffle(preferred);
        Shuffle(fallback);
        preferred.AddRange(fallback);
        return preferred;
    }

    private bool IsValidSpawnCell(Vector2Int cell, Vector2Int playerSpawnCell, DungeonData data)
    {
        if (!Placement.IsFree(cell))
        {
            return false;
        }

        return !MapPlacement.WithinSafeRadius(cell, playerSpawnCell, data.spawnSafeRadius);
    }

    // Quanh ô này có đủ tỉ lệ ĐẤT (Floor) không - chỉ ô Floor mới xây được công trình
    // (GridBuildingSystem.IsFloorCell), nên tinh thể sát mép nước sẽ gần như không còn chỗ đặt mỏ.
    private bool HasBuildableLandAround(TileType[,] map, Vector2Int cell)
    {
        if (buildableClearanceRadius <= 0)
        {
            return true;
        }

        int width = map.GetLength(0);
        int height = map.GetLength(1);
        int landCount = 0;
        int totalCount = 0;

        for (int dx = -buildableClearanceRadius; dx <= buildableClearanceRadius; dx++)
        {
            for (int dy = -buildableClearanceRadius; dy <= buildableClearanceRadius; dy++)
            {
                int x = cell.x + dx;
                int y = cell.y + dy;
                totalCount++;
                bool isLand = x >= 0 && y >= 0 && x < width && y < height && map[x, y] == TileType.Floor;
                if (isLand)
                {
                    landCount++;
                }
            }
        }

        return totalCount > 0 && (float)landCount / totalCount >= minLandRatio;
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
