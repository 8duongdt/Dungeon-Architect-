using System;

/// <summary>
/// Lớp C# thuần sinh bản đồ dungeon bằng thuật toán Cellular Automata.
/// Chỉ chứa logic toán học/sinh map, KHÔNG chứa code Unity hay code vẽ tile.
/// Kết quả trả về là lưới TileType[,] để lớp khác (visualizer) đọc và vẽ.
/// </summary>
public class DungeonGenerator
{
    private readonly DungeonData data;
    private readonly Random random;

    // Một ô được coi là tường nếu có nhiều hơn ngưỡng này ô tường lân cận (8 hướng).
    private const int WallNeighbourThreshold = 4;

    public DungeonGenerator(DungeonData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new Random();
    }

    // Cho phép truyền seed để tái lập kết quả (hữu ích khi test/debug).
    public DungeonGenerator(DungeonData data, int seed)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.random = new Random(seed);
    }

    /// <summary>
    /// Sinh và trả về lưới bản đồ hoàn chỉnh.
    /// </summary>
    public TileType[,] Generate()
    {
        TileType[,] map = CreateRandomNoiseMap();

        for (int step = 0; step < data.simulationSteps; step++)
        {
            map = ApplySimulationStep(map);
        }

        return map;
    }

    // Bước 1: rải nhiễu ngẫu nhiên - mỗi ô là Floor theo xác suất floorGenerationChance,
    // còn lại là Wall. Viền ngoài luôn là tường để bản đồ không bị hở.
    private TileType[,] CreateRandomNoiseMap()
    {
        var map = new TileType[data.mapWidth, data.mapHeight];

        for (int x = 0; x < data.mapWidth; x++)
        {
            for (int y = 0; y < data.mapHeight; y++)
            {
                if (IsBorder(x, y))
                {
                    map[x, y] = TileType.Wall;
                    continue;
                }

                bool isFloor = random.NextDouble() * 100.0 < data.floorGenerationChance;
                map[x, y] = isFloor ? TileType.Floor : TileType.Wall;
            }
        }

        return map;
    }

    // Bước 2: một lần làm mượt - ô có quá nhiều tường lân cận thì thành tường, ngược lại thành sàn.
    private TileType[,] ApplySimulationStep(TileType[,] source)
    {
        var result = new TileType[data.mapWidth, data.mapHeight];

        for (int x = 0; x < data.mapWidth; x++)
        {
            for (int y = 0; y < data.mapHeight; y++)
            {
                if (IsBorder(x, y))
                {
                    result[x, y] = TileType.Wall;
                    continue;
                }

                int wallNeighbours = CountWallNeighbours(source, x, y);
                result[x, y] = wallNeighbours > WallNeighbourThreshold
                    ? TileType.Wall
                    : TileType.Floor;
            }
        }

        return result;
    }

    // Đếm số ô tường trong vùng 8 lân cận quanh (x, y). Ô ngoài biên tính là tường.
    private int CountWallNeighbours(TileType[,] map, int x, int y)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                if (IsOutOfBounds(nx, ny) || map[nx, ny] == TileType.Wall)
                    count++;
            }
        }

        return count;
    }

    private bool IsBorder(int x, int y)
    {
        return x == 0 || y == 0 || x == data.mapWidth - 1 || y == data.mapHeight - 1;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || y < 0 || x >= data.mapWidth || y >= data.mapHeight;
    }
}
