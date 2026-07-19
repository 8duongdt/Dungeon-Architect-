using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp C# thuần sinh bố cục "QUẦN ĐẢO" cho map Undead: một vùng BIỂN (SwampWater) kín bởi viền tường
/// ngoài 1 ô, rải rác NHIỀU ĐẢO nhỏ bằng sàn (Floor) trong biển. CHỈ tính toán layout lưới
/// (TileType[,]) - KHÔNG chứa MonoBehaviour, không vẽ tile, không spawn prefab.
///
/// Đặc điểm quần đảo:
///   - Biển đi lại được (nước không có collider, chỉ làm chậm qua hazard) -> unit lội từ đảo sang đảo.
///   - Pha lê/cổng chỉ đặt trên đất (bên đặt vật lọc Floor); máy không xây được trên nước.
///   - Mỗi đảo được phơi thành một <see cref="Rooms"/> với ô TÂM luôn là đất, nên mọi hệ đọc
///     RoomCenter (spawn người chơi, đặt cổng, gate) tự rơi lên đảo mà không cần sửa gì thêm.
/// </summary>
public class UndeadGenerator
{
    // Cạnh nhỏ nhất tuyệt đối của khu vực, bất kể cấu hình (bảo đảm 12x12).
    private const int MinRoomDimension = DungeonData.MinRoomDimension;

    // Độ dày viền tường ngoài bao quanh vùng biển kín.
    private const int OuterWallThickness = 1;

    // Tần số lấy mẫu Perlin cho mép đảo: nhỏ -> bờ uốn lượn thoải; lớn -> gợn vụn.
    private const float ShorelineNoiseScale = 0.18f;

    // Bán kính lõi đặc quanh tâm mỗi đảo (luôn là đất) - bảo đảm đảo liền khối và ô tâm là Floor.
    private const int IslandCoreRadius = 2;

    // Số lần thử đặt tâm đảo cho mỗi đảo mong muốn trước khi bỏ qua.
    private const int PlacementTriesPerIsland = 12;

    private readonly DungeonData data;
    private readonly System.Random random;

    private readonly List<RectInt> rooms = new List<RectInt>();

    private TileType[,] map;
    private int size;

    /// <summary>Danh sách các đảo đã đặt (mỗi đảo 1 phần tử) - hệ spawn quái/cổng đọc lại.</summary>
    public IReadOnlyList<RectInt> Rooms => rooms;

    /// <summary>Vị trí ô Gate trên một đảo; (-1,-1) nếu chưa sinh map.</summary>
    public Vector2Int GatePosition { get; private set; } = new Vector2Int(-1, -1);

    public UndeadGenerator(DungeonData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new System.Random();
    }

    // Cho phép truyền seed để tái lập kết quả (hữu ích khi test/debug).
    public UndeadGenerator(DungeonData data, int seed)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new System.Random(seed);
    }

    /// <summary>
    /// Sinh và trả về lưới bản đồ hoàn chỉnh: tường viền -> biển kín -> rải các đảo đất -> cổng trên đảo.
    /// </summary>
    public TileType[,] Generate()
    {
        size = Mathf.Max(data.squareMapSize, MinRoomDimension);
        map = CreateSolidGrid();
        rooms.Clear();
        GatePosition = new Vector2Int(-1, -1);

        CarveSea();
        CarveIslands();
        PlaceGate();

        return map;
    }

    // Khởi tạo lưới vuông toàn tường - biển và đảo sẽ được đục ra từ khối đặc này.
    private TileType[,] CreateSolidGrid()
    {
        var grid = new TileType[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                grid[x, y] = TileType.Wall;
            }
        }
        return grid;
    }

    // ----- Bước 1: đổ BIỂN cho toàn bộ nội vùng (trong viền tường ngoài) -----

    private void CarveSea()
    {
        int innerMin = OuterWallThickness;
        int innerMax = size - 1 - OuterWallThickness;
        for (int x = innerMin; x <= innerMax; x++)
        {
            for (int y = innerMin; y <= innerMax; y++)
            {
                map[x, y] = TileType.SwampWater;
            }
        }
    }

    // ----- Bước 2: rải các ĐẢO ĐẤT rời rạc trong biển -----

    private void CarveIslands()
    {
        int islandCount = Mathf.Max(1, data.islandCount);
        int maxRadius = Mathf.Max(1, data.islandMaxRadius);
        int minRadius = Mathf.Clamp(data.islandMinRadius, 1, maxRadius);
        int spacing = Mathf.Max(0, data.islandSpacing);

        // Chừa maxRadius quanh viền để cả đảo (và RectInt vuông của nó) nằm trọn trong biển kín.
        int centerMin = OuterWallThickness + maxRadius;
        int centerMax = size - 1 - OuterWallThickness - maxRadius;
        if (centerMax < centerMin)
        {
            centerMin = centerMax = size / 2;
        }

        var placedCenters = new List<Vector2Int>();
        var placedRadii = new List<int>();
        int totalTries = islandCount * PlacementTriesPerIsland;

        for (int attempt = 0; attempt < totalTries && placedCenters.Count < islandCount; attempt++)
        {
            int baseRadius = random.Next(minRadius, maxRadius + 1);
            var center = new Vector2Int(
                random.Next(centerMin, centerMax + 1),
                random.Next(centerMin, centerMax + 1));

            if (!IsFarEnough(center, baseRadius, spacing, placedCenters, placedRadii))
            {
                continue;
            }

            CarveIsland(center, baseRadius);
            placedCenters.Add(center);
            placedRadii.Add(baseRadius);
            rooms.Add(IslandRect(center, maxRadius));
        }

        // Bảo hiểm: nếu không đặt được đảo nào (map quá nhỏ) thì đục một đảo ở giữa để game vẫn chạy.
        if (rooms.Count == 0)
        {
            var center = new Vector2Int(size / 2, size / 2);
            CarveIsland(center, Mathf.Min(maxRadius, (size - 2 * OuterWallThickness) / 2 - 1));
            rooms.Add(IslandRect(center, maxRadius));
        }
    }

    // Đảo mới đủ tách biệt khi tâm cách MỌI đảo đã đặt >= tổng hai bán kính + spacing (chừa biển ở giữa).
    private bool IsFarEnough(
        Vector2Int center, int baseRadius, int spacing, List<Vector2Int> centers, List<int> radii)
    {
        for (int i = 0; i < centers.Count; i++)
        {
            int minDistance = baseRadius + radii[i] + spacing;
            if ((center - centers[i]).sqrMagnitude < minDistance * minDistance)
            {
                return false;
            }
        }
        return true;
    }

    // Đục một đảo: ô trong bán kính (đã cộng gợn sóng Perlin) thành Floor; lõi quanh tâm luôn là đất
    // để đảo liền khối và ô tâm chắc chắn là Floor (RoomCenter sẽ rơi đúng lên đất).
    private void CarveIsland(Vector2Int center, int baseRadius)
    {
        float noiseOffset = (float)(random.NextDouble() * 1000.0);
        int reach = baseRadius + data.islandEdgeJitter + 1;

        for (int dx = -reach; dx <= reach; dx++)
        {
            for (int dy = -reach; dy <= reach; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= IslandCoreRadius || distance <= EffectiveRadius(x, y, baseRadius, noiseOffset))
                {
                    SetFloor(x, y);
                }
            }
        }
    }

    // Bán kính hiệu dụng tại một ô = bán kính đảo + gợn Perlin trong [-jitter, +jitter] -> mép cong tự nhiên.
    private float EffectiveRadius(int x, int y, int baseRadius, float noiseOffset)
    {
        float noise = Mathf.PerlinNoise((x + noiseOffset) * ShorelineNoiseScale, (y + noiseOffset) * ShorelineNoiseScale);
        return baseRadius + (noise - 0.5f) * 2f * data.islandEdgeJitter;
    }

    // RectInt VUÔNG tâm-tại-tâm-đảo, cạnh lẻ (2*maxRadius+1) để RoomCenter (chia nguyên) trả về ĐÚNG tâm.
    private static RectInt IslandRect(Vector2Int center, int maxRadius)
    {
        return new RectInt(center.x - maxRadius, center.y - maxRadius, maxRadius * 2 + 1, maxRadius * 2 + 1);
    }

    // ----- Bước 3: đặt cổng trên một đảo -----

    private void PlaceGate()
    {
        if (rooms.Count == 0)
        {
            return;
        }

        Vector2Int center = RoomCenter(rooms[rooms.Count - 1]);
        map[center.x, center.y] = TileType.Gate;
        GatePosition = center;
    }

    // ----- Helpers -----

    private static Vector2Int RoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }

    // Đặt ô thành sàn nếu nằm trong vùng đi lại (bên trong viền tường ngoài).
    private void SetFloor(int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= size - 1 || y >= size - 1)
        {
            return;
        }
        map[x, y] = TileType.Floor;
    }
}
