using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rải hazard + vật trang trí cho map Undead Swamp dựa trên ma trận TileType[,] do
/// <see cref="UndeadSwampGenerator"/> sinh ra. CHỈ đọc layout và spawn prefab - không tự sinh map.
///
/// Đặt vật theo NGỮ CẢNH (spatial awareness) thay vì rải ngẫu nhiên thuần:
///   - Cây khô  -> mọc ở "bờ nước" (ô sàn sát SwampWater).
///   - Đống sọ  -> chỉ trên đất khô (không có nước trong bán kính 2 ô).
///   - Bẫy chông -> ở "điểm nghẽn" (hành lang/cửa hẹp, sàn kẹp giữa hai tường).
/// Ngoài ra spawn hazard nước (1:1) và cổng ở đúng tọa độ generator đánh dấu.
/// Dùng TilemapVisualizer (không sửa đổi) để đổi tọa độ ô sang tâm ô thế giới.
/// </summary>
public class UndeadDecorator : MonoBehaviour
{
    // Bán kính (Chebyshev) tính "bờ nước": 1 = xét đúng 8 ô lân cận.
    private const int ShorelineRadius = 1;

    // Sọ chỉ đặt khi KHÔNG có nước trong bán kính này -> giữ sọ trên đất khô.
    private const int SkullDrySafeRadius = 2;

    [Header("Hazard / Cổng")]
    [SerializeField] private GameObject swampWaterHazardPrefab;
    [SerializeField] private GameObject gatePrefab;

    [Header("Cây khô - mọc ở bờ nước (Objects_separately)")]
    [SerializeField] private GameObject deadTreePrefab;
    [Tooltip("Tỉ lệ % một ô bờ nước mọc cây khô.")]
    [Range(0f, 100f)]
    [SerializeField] private float treeShorelineChance = 60f;

    [Header("Đống sọ - chỉ trên đất khô (undead_skull_pile)")]
    [SerializeField] private GameObject skullPilePrefab;
    [Tooltip("Tỉ lệ % một ô đất khô hợp lệ đặt đống sọ.")]
    [Range(0f, 100f)]
    [SerializeField] private float skullDryChance = 10f;

    [Header("Bẫy chông - ở điểm nghẽn (undead_spike_trap)")]
    [SerializeField] private GameObject spikeTrapPrefab;
    [Tooltip("Tỉ lệ % một ô điểm nghẽn đặt bẫy chông.")]
    [Range(0f, 100f)]
    [SerializeField] private float spikeTrapChance = 40f;

    // Gốc gom các vật đã spawn (để scene gọn và dễ dọn). Có thể bỏ trống.
    [Header("Tổ chức scene")]
    [SerializeField] private Transform propParent;

    // Theo dõi vật đã spawn để dọn ở lần sinh map sau.
    private readonly List<GameObject> spawnedProps = new List<GameObject>();

    // Các ô sàn đã có vật, để 3 luật đặt không chồng lên nhau.
    private readonly HashSet<Vector2Int> occupiedFloorTiles = new HashSet<Vector2Int>();

    // Ngữ cảnh của lần Decorate hiện tại (các sub-method dùng chung, không cần truyền tham số).
    private TileType[,] map;
    private TilemapVisualizer visualizer;
    private int mapWidth;
    private int mapHeight;

    /// <summary>
    /// Spawn hazard/cổng rồi đặt vật trang trí theo ngữ cảnh. Thứ tự ưu tiên ô tranh chấp:
    /// bẫy (gameplay) -> cây (bờ nước) -> sọ (đất khô).
    /// </summary>
    public void Decorate(TileType[,] map, TilemapVisualizer visualizer)
    {
        if (map == null || visualizer == null)
        {
            return;
        }

        this.map = map;
        this.visualizer = visualizer;
        mapWidth = map.GetLength(0);
        mapHeight = map.GetLength(1);
        occupiedFloorTiles.Clear();

        SpawnSwampHazardsAndGate();
        SpawnSpikeTrapsInChokePoints();
        SpawnTreesNearWater();
        SpawnSkullsOnDryFloor();
    }

    // Phủ hazard lên ô nước (1:1) và đặt cổng ở ô Gate. Không đụng tới ô sàn.
    private void SpawnSwampHazardsAndGate()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] == TileType.SwampWater)
                {
                    Spawn(swampWaterHazardPrefab, x, y);
                }
                else if (map[x, y] == TileType.Gate)
                {
                    Spawn(gatePrefab, x, y);
                }
            }
        }
    }

    /// <summary>
    /// Cây khô mọc ra từ mép đầm lầy: mỗi ô Floor có ÍT NHẤT một trong 8 hàng xóm là SwampWater
    /// được coi là "bờ nước" và mọc cây với xác suất cao (treeShorelineChance).
    /// </summary>
    private void SpawnTreesNearWater()
    {
        if (deadTreePrefab == null)
        {
            return;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!IsPlaceableFloor(x, y))
                {
                    continue;
                }

                bool isShoreline = HasSwampWithinRadius(x, y, ShorelineRadius);
                if (isShoreline && RollPercent(treeShorelineChance))
                {
                    SpawnFloorProp(deadTreePrefab, x, y);
                }
            }
        }
    }

    /// <summary>
    /// Đống sọ CHỈ đặt trên ô Floor không có ô SwampWater nào trong bán kính 2 ô -> nằm trên đất khô.
    /// </summary>
    private void SpawnSkullsOnDryFloor()
    {
        if (skullPilePrefab == null)
        {
            return;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!IsPlaceableFloor(x, y))
                {
                    continue;
                }

                bool isDryLand = !HasSwampWithinRadius(x, y, SkullDrySafeRadius);
                if (isDryLand && RollPercent(skullDryChance))
                {
                    SpawnFloorProp(skullPilePrefab, x, y);
                }
            }
        }
    }

    /// <summary>
    /// Bẫy chông ở "điểm nghẽn": ô Floor bị kẹp chặt giữa hai ô Wall (hành lang/cửa hẹp 1 ô) -
    /// nơi người chơi tầm xa buộc phải đi qua, thách thức việc chọn vị trí.
    /// </summary>
    private void SpawnSpikeTrapsInChokePoints()
    {
        if (spikeTrapPrefab == null)
        {
            return;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (!IsPlaceableFloor(x, y))
                {
                    continue;
                }

                if (IsChokePoint(x, y) && RollPercent(spikeTrapChance))
                {
                    SpawnFloorProp(spikeTrapPrefab, x, y);
                }
            }
        }
    }

    // Ô đặt được = là Floor và chưa có vật nào chiếm.
    private bool IsPlaceableFloor(int x, int y)
    {
        return map[x, y] == TileType.Floor && !occupiedFloorTiles.Contains(new Vector2Int(x, y));
    }

    // true nếu có ít nhất một ô SwampWater trong vùng vuông bán kính cho trước (bỏ qua ô tâm là sàn).
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

    private bool IsTile(int x, int y, TileType type)
    {
        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight && map[x, y] == type;
    }

    // Spawn một vật trên ô sàn và đánh dấu ô đó đã bị chiếm (chống chồng vật).
    private void SpawnFloorProp(GameObject prefab, int x, int y)
    {
        Spawn(prefab, x, y);
        occupiedFloorTiles.Add(new Vector2Int(x, y));
    }

    private void Spawn(GameObject prefab, int x, int y)
    {
        if (prefab == null)
        {
            return;
        }

        Vector3 worldPosition = visualizer.CellToWorldCenter(new Vector2Int(x, y));
        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity, propParent);
        spawnedProps.Add(instance);
    }

    // true nếu trúng theo tỉ lệ phần trăm (0-100).
    private static bool RollPercent(float percent)
    {
        return Random.value * 100f < percent;
    }

    /// <summary>
    /// Dọn toàn bộ hazard/vật trang trí của lần sinh map trước.
    /// </summary>
    public void ClearProps()
    {
        foreach (GameObject prop in spawnedProps)
        {
            if (prop == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(prop);
            }
            else
            {
                DestroyImmediate(prop);
            }
        }

        spawnedProps.Clear();
        occupiedFloorTiles.Clear();
    }
}
