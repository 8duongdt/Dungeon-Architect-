using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rải hazard + vật trang trí cho map Undead Swamp dựa trên ma trận <see cref="TileType"/>[,] do
/// <see cref="UndeadGenerator"/> sinh ra. CHỈ đọc layout và spawn prefab - không tự sinh map,
/// KHÔNG sửa <see cref="TilemapVisualizer"/> (chỉ gọi public method để đổi ô -> tâm ô thế giới).
///
/// Đặt vật theo NGỮ CẢNH (spatial awareness) thay vì rải ngẫu nhiên thuần:
///   - Vua bộ xương khổng lồ -> góc TRÊN của phòng lớn, tựa vào tường (ranh giới hoành tráng).
///   - Cây gai khô            -> "bờ nước" (ô sàn có hàng xóm là SwampWater).
///   - Dây gai                -> cụm 2-4 nối tiếp trên đất khô (chướng ngại tuyến tính).
///   - Sọ / xương / bia mộ    -> rải lẻ trên đất khô, không sát nước.
///   - Bàn thờ/Crypt trung tâm-> ô Gate ở tâm khu vực (cổng thoát Phase 1).
/// </summary>
public class UndeadDecorator : DungeonDecoratorBase
{
    // Bán kính (Chebyshev) tính "bờ nước": 1 = xét đúng 8 ô lân cận.
    private const int ShorelineRadius = 1;

    // Trang trí lẻ chỉ đặt khi KHÔNG có nước trong bán kính này -> đất khô thực sự.
    private const int DecorationDryRadius = 1;

    // Vua bộ xương chỉ dựng ở phòng đủ lớn (mỗi cạnh >= ngưỡng này) để không chiếm không gian đánh nhau.
    private const int GiantSkeletonMinRoomDimension = 14;

    // Một cụm dây gai dài từ 2 đến 4 ô nối tiếp.
    private const int ThornVineMinCluster = 2;
    private const int ThornVineMaxCluster = 4;

    [Header("Hazard nước & Bàn thờ trung tâm")]
    [SerializeField] private GameObject swampWaterHazardPrefab;
    [Tooltip("Bàn thờ / Crypt đặt ở ô Gate - cổng thoát Phase 1.")]
    [SerializeField] private GameObject centralAltarPrefab;

    [Header("Vua bộ xương khổng lồ - góc trên phòng lớn (undead_king_skeleton)")]
    [SerializeField] private GameObject giantSkeletonPrefab;

    [Header("Cây gai khô - mép nước")]
    [SerializeField] private GameObject shorelineTreePrefab;
    [Tooltip("Tỉ lệ % một ô bờ nước mọc cây gai khô.")]
    [Range(0f, 100f)]
    [SerializeField] private float treeShorelineChance = 50f;

    [Header("Dây gai - chướng ngại tuyến tính")]
    [SerializeField] private GameObject thornVinePrefab;

    [Header("Trang trí lẻ - đất khô (sọ / xương / bia mộ)")]
    [SerializeField] private GameObject[] decorationPrefabs;
    [Tooltip("Tỉ lệ % một ô đất khô hợp lệ đặt một vật trang trí lẻ.")]
    [Range(0f, 100f)]
    [SerializeField] private float decorationScatterChance = 8f;

    [Header("Bẫy chông - điểm nghẽn")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [Tooltip("Tỉ lệ % một ô điểm nghẽn đặt bẫy chông.")]
    [Range(0f, 100f)]
    [SerializeField] private float spikeTrapChance = 40f;

    // 4 hướng đặt dây gai theo trục (phải/trái/lên/xuống).
    private static readonly Vector2Int[] AxisDirections =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    // Ngữ cảnh của lần Decorate hiện tại (các sub-method dùng chung, không cần truyền tham số).
    private TileType[,] map;
    private DungeonData data;
    private int mapWidth;
    private int mapHeight;

    /// <summary>
    /// Spawn hazard/bàn thờ rồi đặt vật theo ngữ cảnh. Thứ tự ưu tiên ô tranh chấp (đặt trước = thắng):
    /// bàn thờ -> bộ xương khổng lồ -> bẫy chông -> dây gai -> cây bờ nước -> trang trí lẻ.
    /// </summary>
    public void Decorate(
        TileType[,] map, IReadOnlyList<RectInt> rooms, DungeonData data, TilemapVisualizer visualizer)
    {
        if (map == null || data == null || visualizer == null)
        {
            return;
        }

        BeginDecorationContext(map, data, visualizer);

        SpawnSwampHazards();
        SpawnCentralAltar();
        SpawnGiantSkeletons(rooms);
        SpawnSpikeTrapsInChokePoints();
        SpawnThornVines();
        SpawnShorelineTrees();
        SpawnDecorations();
    }

    private void BeginDecorationContext(TileType[,] map, DungeonData data, TilemapVisualizer visualizer)
    {
        this.map = map;
        this.data = data;
        mapWidth = map.GetLength(0);
        mapHeight = map.GetLength(1);

        // Decorator chỉ đặt vật trên ô Floor khô -> giới hạn tập đi-lại-được của hạt nhân về {Floor}
        // để IsFree đúng nghĩa "ô sàn còn trống" như cũ (không lan ra nước/cổng/mặt đá).
        BeginPlacement(new MapPlacement(map, visualizer, new[] { TileType.Floor }));
    }

    // ----- Hazard nước (1:1 trên ô SwampWater); không chiếm ô sàn -----

    private void SpawnSwampHazards()
    {
        if (swampWaterHazardPrefab == null)
        {
            return;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] == TileType.SwampWater)
                {
                    SpawnAt(swampWaterHazardPrefab, new Vector2Int(x, y));
                }
            }
        }
    }

    /// <summary>Đặt bàn thờ/Crypt ở ô Gate (tâm khu vực) làm cổng thoát Phase 1.</summary>
    private void SpawnCentralAltar()
    {
        if (centralAltarPrefab == null || !TryFindGate(out Vector2Int gate))
        {
            return;
        }

        SpawnFloorProp(centralAltarPrefab, gate.x, gate.y);
    }

    private bool TryFindGate(out Vector2Int gate)
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] == TileType.Gate)
                {
                    gate = new Vector2Int(x, y);
                    return true;
                }
            }
        }

        gate = default;
        return false;
    }

    /// <summary>
    /// Dựng Vua bộ xương khổng lồ ở HAI góc trên của mỗi phòng lớn, tựa vào tường. Góc phòng
    /// luôn kề tường (mép ngoài phòng) nên bộ xương đứng sát viền, không lấn vào không gian combat.
    /// </summary>
    private void SpawnGiantSkeletons(IReadOnlyList<RectInt> rooms)
    {
        if (giantSkeletonPrefab == null || rooms == null)
        {
            return;
        }

        foreach (RectInt room in rooms)
        {
            if (!IsLargeRoom(room))
            {
                continue;
            }

            int topRow = room.yMax - 1;
            TryPlaceGiantSkeleton(room.xMin, topRow);
            TryPlaceGiantSkeleton(room.xMax - 1, topRow);
        }
    }

    private bool IsLargeRoom(RectInt room)
    {
        return room.width >= GiantSkeletonMinRoomDimension
            && room.height >= GiantSkeletonMinRoomDimension;
    }

    private void TryPlaceGiantSkeleton(int x, int y)
    {
        bool isCornerAgainstWall = IsPlaceableFloor(x, y) && LeansAgainstWall(x, y);
        if (isCornerAgainstWall && RollPercent(data.giantSkeletonSpawnChance))
        {
            SpawnFloorProp(giantSkeletonPrefab, x, y);
        }
    }

    /// <summary>
    /// Bẫy chông ở "điểm nghẽn": ô Floor bị kẹp giữa hai tường (lối hẹp 1 ô) - nơi người chơi
    /// tầm xa buộc phải đi qua, thách thức việc chọn vị trí.
    /// </summary>
    private void SpawnSpikeTrapsInChokePoints()
    {
        if (spikeTrapPrefab == null)
        {
            return;
        }

        ForEachPlaceableFloor((x, y) =>
        {
            if (IsChokePoint(x, y) && RollPercent(spikeTrapChance))
            {
                SpawnFloorProp(spikeTrapPrefab, x, y);
            }
        });
    }

    /// <summary>
    /// Dây gai thành CỤM tuyến tính: từ một ô đất khô khởi đầu, kéo một đường thẳng 2-4 ô và
    /// đặt dây gai trên các ô đất khô liên tiếp -> hàng rào chặn lối đi tự nhiên.
    /// </summary>
    private void SpawnThornVines()
    {
        if (thornVinePrefab == null)
        {
            return;
        }

        ForEachPlaceableFloor((x, y) =>
        {
            if (IsDryFloor(x, y) && RollPercent(data.thornyVineDensity))
            {
                PlaceThornVineCluster(x, y);
            }
        });
    }

    private void PlaceThornVineCluster(int startX, int startY)
    {
        Vector2Int direction = AxisDirections[Random.Range(0, AxisDirections.Length)];
        int clusterLength = RandomRange(ThornVineMinCluster, ThornVineMaxCluster);

        var cell = new Vector2Int(startX, startY);
        for (int i = 0; i < clusterLength; i++)
        {
            if (!IsPlaceableFloor(cell.x, cell.y) || !IsDryFloor(cell.x, cell.y))
            {
                return;
            }

            SpawnFloorProp(thornVinePrefab, cell.x, cell.y);
            cell += direction;
        }
    }

    /// <summary>
    /// Cây gai mọc ra từ mép đầm lầy: mỗi ô Floor có ÍT NHẤT một trong 8 hàng xóm là SwampWater
    /// được coi là "bờ nước" và mọc cây với xác suất treeShorelineChance.
    /// </summary>
    private void SpawnShorelineTrees()
    {
        if (shorelineTreePrefab == null)
        {
            return;
        }

        ForEachPlaceableFloor((x, y) =>
        {
            bool isShoreline = HasSwampWithinRadius(x, y, ShorelineRadius);
            if (isShoreline && RollPercent(treeShorelineChance))
            {
                SpawnFloorProp(shorelineTreePrefab, x, y);
            }
        });
    }

    /// <summary>
    /// Rải vật trang trí lẻ (sọ/xương/bia mộ) trên ô đất khô không có nước kề bên - thuần thẩm mỹ.
    /// </summary>
    private void SpawnDecorations()
    {
        if (decorationPrefabs == null || decorationPrefabs.Length == 0)
        {
            return;
        }

        ForEachPlaceableFloor((x, y) =>
        {
            if (IsDryFloor(x, y) && RollPercent(decorationScatterChance))
            {
                SpawnFloorProp(PickRandomDecoration(), x, y);
            }
        });
    }

    private GameObject PickRandomDecoration()
    {
        return decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];
    }

    // ----- Vị từ ngữ cảnh (spatial checks) -----

    // Ô đặt được = là Floor và chưa có vật nào chiếm (hạt nhân đặt vật đã giới hạn về {Floor}).
    private bool IsPlaceableFloor(int x, int y)
    {
        return Placement.IsFree(new Vector2Int(x, y));
    }

    // Đất khô = không có ô SwampWater nào trong bán kính trang trí.
    private bool IsDryFloor(int x, int y)
    {
        return !HasSwampWithinRadius(x, y, DecorationDryRadius);
    }

    // true nếu có ít nhất một ô SwampWater trong vùng vuông bán kính cho trước.
    private bool HasSwampWithinRadius(int centerX, int centerY, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (IsTile(centerX + dx, centerY + dy, TileType.SwampWater))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Điểm nghẽn: tường hai bên trái-phải, HOẶC tường hai bên trên-dưới (lối hẹp 1 ô).
    private bool IsChokePoint(int x, int y)
    {
        bool squeezedHorizontally = IsTile(x - 1, y, TileType.Wall) && IsTile(x + 1, y, TileType.Wall);
        bool squeezedVertically = IsTile(x, y - 1, TileType.Wall) && IsTile(x, y + 1, TileType.Wall);
        return squeezedHorizontally || squeezedVertically;
    }

    // true nếu có ít nhất một trong 4 ô kề là tường (vật tựa vào tường).
    private bool LeansAgainstWall(int x, int y)
    {
        return IsTile(x - 1, y, TileType.Wall) || IsTile(x + 1, y, TileType.Wall)
            || IsTile(x, y - 1, TileType.Wall) || IsTile(x, y + 1, TileType.Wall);
    }

    private bool IsTile(int x, int y, TileType type)
    {
        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight && map[x, y] == type;
    }

    // ----- Quét lưới & spawn -----

    // Duyệt mọi ô đặt được và áp một hành động đặt vật lên nó (gom vòng lặp lưới về một chỗ).
    private void ForEachPlaceableFloor(System.Action<int, int> placeAction)
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (IsPlaceableFloor(x, y))
                {
                    placeAction(x, y);
                }
            }
        }
    }

    // Spawn một vật trên ô sàn và đánh dấu ô đó đã bị chiếm (chống chồng vật).
    private void SpawnFloorProp(GameObject prefab, int x, int y)
    {
        SpawnAndOccupy(prefab, new Vector2Int(x, y));
    }

    private int RandomRange(int minInclusive, int maxInclusive)
    {
        return Random.Range(minInclusive, maxInclusive + 1);
    }
}
