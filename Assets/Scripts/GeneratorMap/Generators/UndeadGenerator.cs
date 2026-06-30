using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp C# thuần sinh bố cục dungeon "một khu vực vuông to và kín" cho map Undead.
/// CHỈ tính toán layout lưới (TileType[,]) - KHÔNG chứa MonoBehaviour, không vẽ tile,
/// không spawn prefab. Lớp vẽ/đặt vật ở nơi khác đọc kết quả này.
///
/// Khác với RoomFirstGenerator (chia nhiều phòng nhỏ nối hành lang), Undead chỉ đục MỘT
/// vùng sàn vuông duy nhất, bao kín bởi viền tường ngoài 1 ô - một đấu trường mở:
///   - Toàn bộ khu vực thoáng, không vật cản chia cắt -> tầm nhìn thẳng, góc bắn rộng cho
///     nhân vật tầm xa.
///   - Bên trong carve các vũng SwampWater làm địa hình.
///   - Tâm khu vực được đánh dấu một ô Gate (cổng/portal).
/// </summary>
public class UndeadGenerator
{
    // Cạnh nhỏ nhất tuyệt đối của khu vực, bất kể cấu hình (bảo đảm 12x12).
    private const int MinRoomDimension = DungeonData.MinRoomDimension;

    // Độ dày viền tường ngoài bao quanh khu vực kín.
    private const int OuterWallThickness = 1;

    // Số lần thử tìm chỗ đặt một hồ nước trước khi bỏ qua.
    private const int PoolPlacementTries = 20;

    // Chừa khoảng này quanh mép trong phòng để hồ nước (và vành bờ) không chạm tường ngoài.
    private const int PoolEdgeMargin = 2;

    // Chừa trống quanh tâm khu vực (ô gate/điểm xuất phát) - không cho hồ nước lấp lối ra.
    private const int PoolKeepClearRadius = 6;

    private readonly DungeonData data;
    private readonly System.Random random;

    private readonly List<RectInt> rooms = new List<RectInt>();

    private TileType[,] map;
    private int size;

    /// <summary>Khu vực kín duy nhất đã đặt - 1 phần tử (dùng cho hệ thống spawn quái đọc lại).</summary>
    public IReadOnlyList<RectInt> Rooms => rooms;

    /// <summary>Vị trí ô Gate ở tâm khu vực; (-1,-1) nếu chưa sinh map.</summary>
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
    /// Sinh và trả về lưới bản đồ hoàn chỉnh: tường viền -> một khu vực sàn vuông kín -> vũng nước -> cổng.
    /// </summary>
    public TileType[,] Generate()
    {
        size = Mathf.Max(data.squareMapSize, MinRoomDimension);
        map = CreateSolidGrid();
        rooms.Clear();
        GatePosition = new Vector2Int(-1, -1);

        RectInt mainArea = CarveMainArea();
        rooms.Add(mainArea);

        CarveSwampPools();
        PlaceGate();

        return map;
    }

    // Khởi tạo lưới vuông toàn tường - khu vực sàn sẽ được đục ra từ khối đặc này.
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

    // ----- Bước 1: đục MỘT khu vực sàn vuông to, kín bởi viền tường ngoài 1 ô -----

    // Không chia phòng/hành lang như RoomFirst - toàn bộ phần trong viền tường là sàn đi lại được.
    private RectInt CarveMainArea()
    {
        var area = new RectInt(
            OuterWallThickness,
            OuterWallThickness,
            size - OuterWallThickness * 2,
            size - OuterWallThickness * 2);

        CarveRoom(area);
        return area;
    }

    private void CarveRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                SetFloor(x, y);
            }
        }
    }

    // ----- Bước 2: đặt các HỒ NƯỚC CHỮ NHẬT (kiểu phòng dungeon) -----

    // Mỗi phòng có swampRoomChance% được rải hồ nước. Hồ là các hình chữ nhật rời nhau (cách nhau
    // swampPoolPadding ô) nên mỗi hồ tự được vành bờ 8 hướng bao quanh ở khâu vẽ - giống cấu trúc
    // phòng-có-tường của RoomFirst, thay cho nhiễu Perlin lởm chởm trước đây.
    private void CarveSwampPools()
    {
        foreach (RectInt room in rooms)
        {
            if (RollPercent(data.swampRoomChance))
            {
                CarveRectangularPools(room);
            }
        }
    }

    // Thử rải swampPoolCount hồ chữ nhật không chồng nhau trong phòng, tránh tâm khu vực.
    private void CarveRectangularPools(RectInt room)
    {
        var placedPools = new List<RectInt>();
        Vector2Int center = RoomCenter(room);

        for (int i = 0; i < data.swampPoolCount; i++)
        {
            if (TryFindPoolRect(room, center, placedPools, out RectInt pool))
            {
                FillPool(pool);
                placedPools.Add(pool);
            }
        }
    }

    // Thử vài vị trí/kích thước ngẫu nhiên cho một hồ; trả về true ở lần đầu hợp lệ.
    private bool TryFindPoolRect(
        RectInt room, Vector2Int center, List<RectInt> placedPools, out RectInt pool)
    {
        for (int attempt = 0; attempt < PoolPlacementTries; attempt++)
        {
            int width = RandomRange(data.swampPoolMinSize, data.swampPoolMaxSize);
            int height = RandomRange(data.swampPoolMinSize, data.swampPoolMaxSize);
            RectInt candidate = RandomPoolPosition(room, width, height);

            if (IsPoolPositionValid(candidate, center, placedPools))
            {
                pool = candidate;
                return true;
            }
        }

        pool = default;
        return false;
    }

    // Vị trí góc dưới-trái ngẫu nhiên sao cho cả hồ nằm trong phòng (đã chừa PoolEdgeMargin mép).
    private RectInt RandomPoolPosition(RectInt room, int width, int height)
    {
        int minX = room.xMin + PoolEdgeMargin;
        int maxX = room.xMax - PoolEdgeMargin - width;
        int minY = room.yMin + PoolEdgeMargin;
        int maxY = room.yMax - PoolEdgeMargin - height;

        if (maxX < minX || maxY < minY)
        {
            return new RectInt(minX, minY, width, height);
        }

        return new RectInt(
            random.Next(minX, maxX + 1), random.Next(minY, maxY + 1), width, height);
    }

    // Hợp lệ khi không lấn vùng chừa trống quanh tâm và cách mọi hồ đã đặt >= swampPoolPadding ô.
    private bool IsPoolPositionValid(RectInt pool, Vector2Int center, List<RectInt> placedPools)
    {
        if (IsNearAreaCenter(pool, center))
        {
            return false;
        }

        foreach (RectInt other in placedPools)
        {
            if (Expand(other, data.swampPoolPadding).Overlaps(pool))
            {
                return false;
            }
        }

        return true;
    }

    // Hồ có lấn vào ô vuông chừa trống quanh tâm khu vực (giữ lối ra/điểm xuất phát thông thoáng) không.
    private bool IsNearAreaCenter(RectInt pool, Vector2Int center)
    {
        var clearZone = new RectInt(
            center.x - PoolKeepClearRadius, center.y - PoolKeepClearRadius,
            PoolKeepClearRadius * 2, PoolKeepClearRadius * 2);
        return clearZone.Overlaps(pool);
    }

    // Đổ nước vào toàn bộ ô sàn trong hình chữ nhật hồ (không đè cổng/tường).
    private void FillPool(RectInt pool)
    {
        for (int x = pool.xMin; x < pool.xMax; x++)
        {
            for (int y = pool.yMin; y < pool.yMax; y++)
            {
                if (map[x, y] == TileType.Floor)
                {
                    map[x, y] = TileType.SwampWater;
                }
            }
        }
    }

    // Nới một RectInt ra mọi phía 'by' ô (dùng để kiểm tra khoảng cách tối thiểu giữa các hồ).
    private static RectInt Expand(RectInt rect, int by)
    {
        return new RectInt(rect.xMin - by, rect.yMin - by, rect.width + by * 2, rect.height + by * 2);
    }

    private int RandomRange(int minInclusive, int maxInclusive)
    {
        return random.Next(minInclusive, maxInclusive + 1);
    }

    // ----- Bước 3: đặt cổng ở tâm khu vực -----

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

    // true nếu trúng theo tỉ lệ phần trăm (0-100).
    private bool RollPercent(float percent)
    {
        return random.NextDouble() * 100.0 < percent;
    }
}
