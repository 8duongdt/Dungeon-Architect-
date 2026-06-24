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

    // Chừa 1 ô sàn quanh mép trong phòng để vũng nước không lan ra sát tường.
    private const int SwampEdgeMargin = 1;

    // Nước phủ trong dải này so với diện tích phòng: đủ làm hazard cục bộ nhưng vẫn chừa
    // >= 70% sàn khô cho nhân vật tầm xa xoay xở và lấy góc bắn.
    private const float SwampMinCoverage = 0.20f;
    private const float SwampMaxCoverage = 0.30f;

    // Tần số lấy mẫu Perlin: số nhỏ -> vũng nước to, mượt; số lớn -> vũng vụn, lốm đốm.
    private const float SwampNoiseScale = 0.22f;

    // Biên độ dịch gốc Perlin để mỗi phòng (và mỗi lần sinh) ra hình nước khác nhau.
    private const float SwampNoiseOffsetRange = 1000f;

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

    // ----- Bước 2: carve vũng nước đầm lầy bằng nhiễu Perlin -----

    private void CarveSwampPools()
    {
        foreach (RectInt room in rooms)
        {
            if (RollPercent(data.swampRoomChance))
            {
                CarveSwampInRoom(room);
            }
        }
    }

    // Phủ nước hữu cơ trong phòng bằng nhiễu Perlin: lấy mẫu mọi ô sàn trong vùng lõi rồi
    // biến những ô có giá trị nhiễu CAO NHẤT thành nước cho tới khi đạt tỉ lệ phủ mục tiêu.
    // Perlin liên tục nên các ô giá trị cao nằm liền nhau -> vũng nước liền mạch, bờ bất quy tắc.
    private void CarveSwampInRoom(RectInt room)
    {
        RectInt core = ShrinkToCore(room);
        if (core.width < 1 || core.height < 1)
        {
            return;
        }

        int targetWaterTiles = SwampTargetTileCount(room);
        if (targetWaterTiles <= 0)
        {
            return;
        }

        List<NoiseSample> floorSamples = SampleFloorNoise(core);
        FloodHighestSamples(floorSamples, targetWaterTiles);
    }

    // Thu phòng vào trong SwampEdgeMargin ô để nước không chạm sát tường (chừa lối men theo mép).
    private RectInt ShrinkToCore(RectInt room)
    {
        return new RectInt(
            room.xMin + SwampEdgeMargin,
            room.yMin + SwampEdgeMargin,
            room.width - SwampEdgeMargin * 2,
            room.height - SwampEdgeMargin * 2);
    }

    // Số ô nước mục tiêu = (20-30% theo cấu hình) nhân diện tích sàn phòng.
    private int SwampTargetTileCount(RectInt room)
    {
        float coverage = Mathf.Clamp(
            data.swampWaterPercentage / 100f, SwampMinCoverage, SwampMaxCoverage);
        int totalFloorArea = room.width * room.height;
        return Mathf.RoundToInt(totalFloorArea * coverage);
    }

    // Lấy mẫu nhiễu Perlin cho từng ô sàn trong vùng lõi. Gốc lấy mẫu dịch ngẫu nhiên nên
    // mỗi phòng/lần sinh cho ra hình nước khác nhau.
    private List<NoiseSample> SampleFloorNoise(RectInt core)
    {
        float offsetX = (float)random.NextDouble() * SwampNoiseOffsetRange;
        float offsetY = (float)random.NextDouble() * SwampNoiseOffsetRange;

        var samples = new List<NoiseSample>(core.width * core.height);
        for (int x = core.xMin; x < core.xMax; x++)
        {
            for (int y = core.yMin; y < core.yMax; y++)
            {
                if (map[x, y] != TileType.Floor)
                {
                    continue;
                }

                float noise = Mathf.PerlinNoise(
                    (x + offsetX) * SwampNoiseScale, (y + offsetY) * SwampNoiseScale);
                samples.Add(new NoiseSample(new Vector2Int(x, y), noise));
            }
        }

        return samples;
    }

    // Sắp xếp ô sàn theo giá trị nhiễu giảm dần và ngập nước những ô cao nhất đến khi đủ chỉ tiêu.
    private void FloodHighestSamples(List<NoiseSample> samples, int targetWaterTiles)
    {
        samples.Sort((a, b) => b.Noise.CompareTo(a.Noise));

        int waterTiles = Mathf.Min(targetWaterTiles, samples.Count);
        for (int i = 0; i < waterTiles; i++)
        {
            Vector2Int cell = samples[i].Cell;
            map[cell.x, cell.y] = TileType.SwampWater;
        }
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

    // Cặp (ô sàn, giá trị nhiễu Perlin) - dùng để xếp hạng và chọn ô ngập nước.
    private readonly struct NoiseSample
    {
        public readonly Vector2Int Cell;
        public readonly float Noise;

        public NoiseSample(Vector2Int cell, float noise)
        {
            Cell = cell;
            Noise = noise;
        }
    }
}
