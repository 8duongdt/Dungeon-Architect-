using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lớp C# thuần sinh bố cục dungeon kiểu "Room-First" cho map Undead Swamp.
/// CHỈ tính toán layout lưới (TileType[,]) - KHÔNG chứa MonoBehaviour, không vẽ tile,
/// không spawn prefab. Lớp vẽ/đặt vật ở nơi khác đọc kết quả này.
///
/// Thiết kế tối ưu cho nhân vật tầm xa:
///   - Phòng chữ nhật/vuông to và thoáng (tối thiểu 10x10) -> tầm nhìn thẳng, góc bắn rộng,
///     ít vật cản bên trong.
///   - Hành lang rộng nối sạch các phòng.
///   - Bên trong phòng carve các vũng SwampWater làm địa hình.
///   - Phòng cuối được đánh dấu một ô Gate (cổng/portal).
/// </summary>
public class UndeadSwampGenerator
{
    // Cạnh nhỏ nhất tuyệt đối của một phòng, bất kể cấu hình (bảo đảm 10x10).
    private const int MinRoomDimension = DungeonData.MinRoomDimension;

    // Số lần thử đặt cho mỗi phòng trước khi bỏ cuộc (tránh kẹt vòng lặp vô hạn).
    private const int PlacementAttemptsPerRoom = 16;

    // Chừa 1 ô sàn quanh mép trong phòng để vũng nước không lan ra sát tường.
    private const int SwampEdgeMargin = 1;

    // Nước phải phủ trong khoảng này so với diện tích sàn phòng. Tối đa 35% -> chừa >= 65%
    // sàn khô cho người chơi tầm xa xoay xở và lấy góc bắn.
    private const float SwampMinCoverage = 0.20f;
    private const float SwampMaxCoverage = 0.35f;

    // Mỗi phòng gieo 1 hoặc 2 "lõi" nước (Swamp Core) rồi loang ra từ đó.
    private const int MinSwampCores = 1;
    private const int MaxSwampCores = 2;

    private readonly DungeonData data;
    private readonly System.Random random;

    private readonly List<RectInt> rooms = new List<RectInt>();

    // 4 hướng loang nước; được xáo trộn mỗi lần mở rộng để vũng nước có hình bất quy tắc.
    private readonly Vector2Int[] expandDirections =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    private TileType[,] map;
    private int size;

    /// <summary>Danh sách phòng đã đặt (dùng cho hệ thống spawn quái đọc lại).</summary>
    public IReadOnlyList<RectInt> Rooms => rooms;

    /// <summary>Vị trí ô Gate ở phòng cuối; (-1,-1) nếu không đặt được phòng nào.</summary>
    public Vector2Int GatePosition { get; private set; } = new Vector2Int(-1, -1);

    public UndeadSwampGenerator(DungeonData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new System.Random();
    }

    // Cho phép truyền seed để tái lập kết quả (hữu ích khi test/debug).
    public UndeadSwampGenerator(DungeonData data, int seed)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new System.Random(seed);
    }

    /// <summary>
    /// Sinh và trả về lưới bản đồ hoàn chỉnh: tường nền -> phòng -> hành lang -> vũng nước -> cổng.
    /// </summary>
    public TileType[,] Generate()
    {
        size = Mathf.Max(data.squareMapSize, MinRoomDimension);
        map = CreateSolidGrid();
        rooms.Clear();
        GatePosition = new Vector2Int(-1, -1);

        PlaceRooms();
        ConnectRooms();
        CarveSwampPools();
        PlaceGate();

        return map;
    }

    // Khởi tạo lưới vuông toàn tường - phòng và hành lang sẽ được đục ra từ khối đặc này.
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

    // ----- Bước 1: đặt các phòng chữ nhật to, không chồng lấn -----

    private void PlaceRooms()
    {
        int minRoom = Mathf.Max(data.minRoomSize, MinRoomDimension);
        // Phòng phải nằm gọn trong viền tường ngoài, nên cạnh không vượt quá size - 2.
        int maxRoom = Mathf.Clamp(data.maxRoomSize, minRoom, size - 2);

        int totalAttempts = data.maxRooms * PlacementAttemptsPerRoom;
        for (int attempt = 0; attempt < totalAttempts && rooms.Count < data.maxRooms; attempt++)
        {
            RectInt candidate = CreateRandomRoom(minRoom, maxRoom);
            if (OverlapsExistingRoom(candidate))
            {
                continue;
            }

            rooms.Add(candidate);
            CarveRoom(candidate);
        }
    }

    private RectInt CreateRandomRoom(int minRoom, int maxRoom)
    {
        int width = RandomRange(minRoom, maxRoom);
        int height = RandomRange(minRoom, maxRoom);
        // Giữ phòng cách viền ngoài ít nhất 1 ô tường.
        int x = RandomRange(1, size - width - 1);
        int y = RandomRange(1, size - height - 1);
        return new RectInt(x, y, width, height);
    }

    // Phòng mới phải cách phòng cũ ít nhất roomPadding ô để không dính nhau.
    private bool OverlapsExistingRoom(RectInt candidate)
    {
        var padded = new RectInt(
            candidate.xMin - data.roomPadding,
            candidate.yMin - data.roomPadding,
            candidate.width + data.roomPadding * 2,
            candidate.height + data.roomPadding * 2);

        foreach (RectInt room in rooms)
        {
            if (padded.Overlaps(room))
            {
                return true;
            }
        }
        return false;
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

    // ----- Bước 2: nối tâm các phòng bằng hành lang rộng hình chữ L -----

    private void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int previous = RoomCenter(rooms[i - 1]);
            Vector2Int current = RoomCenter(rooms[i]);

            // Ngẫu nhiên đi ngang trước hay dọc trước để hành lang đỡ lặp khuôn.
            if (random.Next(2) == 0)
            {
                CarveHorizontalHallway(previous.x, current.x, previous.y);
                CarveVerticalHallway(previous.y, current.y, current.x);
            }
            else
            {
                CarveVerticalHallway(previous.y, current.y, previous.x);
                CarveHorizontalHallway(previous.x, current.x, current.y);
            }
        }
    }

    private void CarveHorizontalHallway(int xStart, int xEnd, int y)
    {
        int from = Mathf.Min(xStart, xEnd);
        int to = Mathf.Max(xStart, xEnd);
        for (int x = from; x <= to; x++)
        {
            CarveHallwayBand(x, y, horizontal: true);
        }
    }

    private void CarveVerticalHallway(int yStart, int yEnd, int x)
    {
        int from = Mathf.Min(yStart, yEnd);
        int to = Mathf.Max(yStart, yEnd);
        for (int y = from; y <= to; y++)
        {
            CarveHallwayBand(x, y, horizontal: false);
        }
    }

    // Đục một "lát" hành lang dày hallwayWidth ô, căn giữa quanh trục đi.
    private void CarveHallwayBand(int x, int y, bool horizontal)
    {
        int width = data.hallwayWidth;
        for (int offset = 0; offset < width; offset++)
        {
            int shift = offset - width / 2;
            if (horizontal)
            {
                SetFloor(x, y + shift);
            }
            else
            {
                SetFloor(x + shift, y);
            }
        }
    }

    // ----- Bước 3: carve vũng nước đầm lầy bên trong phòng -----

    private void CarveSwampPools()
    {
        foreach (RectInt room in rooms)
        {
            if (!RollPercent(data.swampRoomChance))
            {
                continue;
            }

            CarveSwampInRoom(room);
        }
    }

    // Sinh vũng nước hữu cơ trong một phòng: gieo 1-2 lõi ngẫu nhiên rồi loang ra
    // (flood-fill ngẫu nhiên) cho tới khi nước phủ 20-35% diện tích sàn phòng.
    private void CarveSwampInRoom(RectInt room)
    {
        // Vùng lõi: chừa SwampEdgeMargin ô sàn quanh mép phòng làm lối đi/tầm bắn.
        var core = new RectInt(
            room.xMin + SwampEdgeMargin,
            room.yMin + SwampEdgeMargin,
            room.width - SwampEdgeMargin * 2,
            room.height - SwampEdgeMargin * 2);
        if (core.width < 1 || core.height < 1)
        {
            return;
        }

        int targetSwampTiles = SwampTargetTileCount(room);
        if (targetSwampTiles <= 0)
        {
            return;
        }

        List<Vector2Int> frontier = SeedSwampCores(core);
        GrowSwamp(frontier, core, targetSwampTiles);
    }

    // Số ô nước mục tiêu = (20-35% theo cấu hình) nhân diện tích sàn phòng.
    private int SwampTargetTileCount(RectInt room)
    {
        float coverage = Mathf.Clamp(data.swampCoverage / 100f, SwampMinCoverage, SwampMaxCoverage);
        int totalFloorArea = room.width * room.height;
        return Mathf.RoundToInt(totalFloorArea * coverage);
    }

    // Gieo 1-2 lõi nước ngẫu nhiên trong vùng lõi; trả về danh sách biên (frontier) để loang.
    private List<Vector2Int> SeedSwampCores(RectInt core)
    {
        int coreCount = Mathf.Clamp(data.swampBlobCount, MinSwampCores, MaxSwampCores);
        var frontier = new List<Vector2Int>();

        for (int i = 0; i < coreCount; i++)
        {
            int x = RandomRange(core.xMin, core.xMax - 1);
            int y = RandomRange(core.yMin, core.yMax - 1);
            if (map[x, y] != TileType.Floor)
            {
                continue;
            }

            map[x, y] = TileType.SwampWater;
            frontier.Add(new Vector2Int(x, y));
        }

        return frontier;
    }

    // Loang nước từ frontier theo flood-fill NGẪU NHIÊN: mỗi bước chọn một ô biên ngẫu nhiên
    // và biến một ô sàn lân cận của nó thành nước -> hình vũng bất quy tắc, hữu cơ.
    // Dừng ngay khi đạt số ô mục tiêu hoặc khi không còn ô sàn nào để lan.
    private void GrowSwamp(List<Vector2Int> frontier, RectInt core, int targetSwampTiles)
    {
        int swampCount = frontier.Count;

        while (swampCount < targetSwampTiles && frontier.Count > 0)
        {
            int pick = random.Next(frontier.Count);
            Vector2Int cell = frontier[pick];

            if (TryExpandSwamp(cell, core, out Vector2Int grown))
            {
                map[grown.x, grown.y] = TileType.SwampWater;
                frontier.Add(grown);
                swampCount++;
            }
            else
            {
                // Ô biên này đã hết hàng xóm sàn -> rút khỏi frontier (swap-remove cho nhanh).
                frontier[pick] = frontier[frontier.Count - 1];
                frontier.RemoveAt(frontier.Count - 1);
            }
        }
    }

    // Tìm một ô sàn 4-hướng còn trống quanh cell (trong vùng lõi) để biến thành nước.
    // Xáo trộn thứ tự hướng để nước không loang thiên về một phía.
    private bool TryExpandSwamp(Vector2Int cell, RectInt core, out Vector2Int result)
    {
        ShuffleExpandDirections();

        foreach (Vector2Int direction in expandDirections)
        {
            int nx = cell.x + direction.x;
            int ny = cell.y + direction.y;
            if (nx < core.xMin || ny < core.yMin || nx >= core.xMax || ny >= core.yMax)
            {
                continue;
            }

            if (map[nx, ny] == TileType.Floor)
            {
                result = new Vector2Int(nx, ny);
                return true;
            }
        }

        result = default;
        return false;
    }

    // Fisher-Yates xáo trộn tại chỗ 4 hướng loang.
    private void ShuffleExpandDirections()
    {
        for (int i = expandDirections.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (expandDirections[i], expandDirections[j]) = (expandDirections[j], expandDirections[i]);
        }
    }

    // ----- Bước 4: đặt cổng ở phòng cuối -----

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

    // Số nguyên ngẫu nhiên trong [minInclusive, maxInclusive].
    private int RandomRange(int minInclusive, int maxInclusive)
    {
        if (maxInclusive <= minInclusive)
        {
            return minInclusive;
        }
        return random.Next(minInclusive, maxInclusive + 1);
    }

    // true nếu trúng theo tỉ lệ phần trăm (0-100).
    private bool RollPercent(float percent)
    {
        return random.NextDouble() * 100.0 < percent;
    }
}
