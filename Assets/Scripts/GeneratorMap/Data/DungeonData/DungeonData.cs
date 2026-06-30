using UnityEngine;

// Kiểu thuật toán sinh layout mà DungeonManager sẽ chạy cho data này.
//   RoomFirst     -> chia nhiều phòng nhỏ nối bằng hành lang (hầm ngục cổ điển).
//   UndeadBigRoom -> một khu vực vuông to, kín duy nhất + vũng nước đầm lầy (đấu trường tầm xa).
public enum DungeonGeneratorType
{
    RoomFirst,
    UndeadBigRoom
}

// Cấu hình sinh dungeon. Chỉ chứa dữ liệu cấu hình - không chứa logic sinh map hay vẽ tile.
// Một data quyết định DÙNG generator nào (generatorType) và tham số cho generator đó.
[CreateAssetMenu(fileName = "DungeonData_", menuName = "PCG/DungeonData")]
public class DungeonData : ScriptableObject
{
    [Header("Thuật toán sinh")]
    // DungeonManager đọc field này để chọn generator tương ứng.
    public DungeonGeneratorType generatorType = DungeonGeneratorType.UndeadBigRoom;

    [Header("Kích thước bản đồ")]
    [Min(1)]
    public int mapWidth = 80;

    [Min(1)]
    public int mapHeight = 50;

    [Header("Cellular Automata")]
    // Tỉ lệ % một ô được khởi tạo là sàn (Floor) ở bước random ban đầu.
    [Range(0f, 100f)]
    public float floorGenerationChance = 45f;

    // Số lần chạy mô phỏng làm mượt (smoothing) lưới.
    [Min(0)]
    public int simulationSteps = 5;

    [Header("Tỉ lệ sinh vật thể (%)")]
    // Tỉ lệ % một ô sàn hợp lệ được đặt đuốc (torch).
    [Range(0f, 100f)]
    public float torchSpawnRate = 5f;

    // Tỉ lệ % một ô sàn hợp lệ được đặt rương (chest).
    [Range(0f, 100f)]
    public float chestSpawnRate = 2f;

    // ---------------------------------------------------------------------
    // Cấu hình cho RoomFirstGenerator (chia nhiều phòng nhỏ bằng BSP).
    // Chỉ dùng khi generatorType == RoomFirst.
    // ---------------------------------------------------------------------

    [Header("Room-First - Chia phòng")]
    // Kích thước vùng BSP để chia phòng.
    [Min(1)]
    public int roomFirstMapWidth = 60;

    [Min(1)]
    public int roomFirstMapHeight = 40;

    // Cạnh nhỏ nhất của một phòng khi chia BSP.
    [Min(1)]
    public int roomFirstMinRoomWidth = 8;

    [Min(1)]
    public int roomFirstMinRoomHeight = 8;

    // Số ô lề chừa quanh mép phòng (phòng không lấp kín ô BSP -> có khe tường giữa các phòng).
    // Tối thiểu 3 để hành lang 3x3 không lấp đầy khoảng cách → tường phân chia phòng thấy được.
    [Range(0, 10)]
    public int roomFirstOffset = 3;

    // ---------------------------------------------------------------------
    // Cấu hình cho UndeadGenerator (thuật toán Room-First, map vuông).
    // Tách riêng khỏi phần Cellular Automata ở trên; hai pipeline dùng chung asset.
    // ---------------------------------------------------------------------

    [Header("Undead Swamp - Bản đồ vuông, một khu kín duy nhất")]
    // Cạnh của bản đồ vuông (số ô). Toàn bộ vùng trong viền tường ngoài là MỘT khu vực kín
    // duy nhất (không chia phòng/hành lang) - đấu trường rộng cho nhân vật tầm xa.
    [Min(MinRoomDimension)]
    public int squareMapSize = 100;

    [Header("Undead Swamp - Hồ nước chữ nhật (kiểu phòng dungeon)")]
    // Tỉ lệ % mỗi phòng được rải hồ nước.
    [Range(0f, 100f)]
    public float swampRoomChance = 65f;

    // Số hồ nước chữ nhật thử rải trong phòng (mỗi hồ là một hình chữ nhật rời, có vành bờ bao quanh).
    [Min(0)]
    public int swampPoolCount = 8;

    // Kích thước mỗi cạnh hồ (số ô) - chọn ngẫu nhiên trong [min, max].
    [Min(2)]
    public int swampPoolMinSize = 5;

    [Min(2)]
    public int swampPoolMaxSize = 12;

    // Khoảng cách tối thiểu giữa hai hồ (số ô) - để mỗi hồ có vành bờ riêng, không dính nhau.
    [Min(1)]
    public int swampPoolPadding = 3;

    [Header("Undead Swamp - Vật trang trí theo ngữ cảnh (%)")]
    // Tỉ lệ % mỗi góc trên của phòng lớn dựng một Vua bộ xương khổng lồ tựa vào tường.
    [Range(0f, 100f)]
    public float giantSkeletonSpawnChance = 35f;

    // Tỉ lệ % một ô sàn khô khởi đầu một cụm dây gai (chướng ngại tuyến tính).
    [Range(0f, 100f)]
    public float thornyVineDensity = 4f;

    [Header("Undead Swamp - Tỉ lệ sinh quái Undead (%)")]
    // Tỉ lệ % một ô sàn hợp lệ được dùng để sinh quái Undead (do hệ thống spawn đọc, không thuộc layout).
    [Range(0f, 100f)]
    public float undeadSpawnRate = 10f;

    // ---------------------------------------------------------------------
    // Luật spawn dùng chung (WeightedScatterSpawner đọc).
    // ---------------------------------------------------------------------

    [Header("Luật spawn - rải vật/quái theo trọng số")]
    // Bán kính an toàn quanh điểm xuất phát người chơi: cấm spawn quái/vật trong vùng này (số ô).
    [Min(0f)]
    public float spawnSafeRadius = 8f;

    // Tần số lấy mẫu Perlin cho việc gom cụm: nhỏ -> cụm to mượt; lớn -> rải vụn.
    [Min(0.001f)]
    public float spawnPerlinScale = 0.15f;

    // Chỉ spawn ở ô có giá trị nhiễu Perlin vượt ngưỡng -> vật xuất hiện thành cụm tự nhiên.
    [Range(0f, 1f)]
    public float spawnPerlinThreshold = 0.6f;

    // Giới hạn số vật/quái spawn mỗi phòng (chống dồn cục bộ).
    [Min(0)]
    public int spawnPerRoomCap = 5;

    // Giới hạn tổng số vật/quái spawn toàn map.
    [Min(0)]
    public int spawnGlobalCap = 40;

    // Cạnh tối thiểu cho cả map lẫn phòng - bảo đảm phòng không nhỏ hơn 12x12 để
    // có tầm nhìn thẳng và góc bắn rộng cho nhân vật tầm xa.
    public const int MinRoomDimension = 12;
}
