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
    [Range(0, 10)]
    public int roomFirstOffset = 1;

    // ---------------------------------------------------------------------
    // Cấu hình cho UndeadGenerator (thuật toán Room-First, map vuông).
    // Tách riêng khỏi phần Cellular Automata ở trên; hai pipeline dùng chung asset.
    // ---------------------------------------------------------------------

    [Header("Undead Swamp - Bản đồ vuông, một khu kín duy nhất")]
    // Cạnh của bản đồ vuông (số ô). Toàn bộ vùng trong viền tường ngoài là MỘT khu vực kín
    // duy nhất (không chia phòng/hành lang) - đấu trường rộng cho nhân vật tầm xa.
    [Min(MinRoomDimension)]
    public int squareMapSize = 100;

    [Header("Undead Swamp - Vũng nước đầm lầy")]
    // Tỉ lệ % mỗi phòng được carve ra vũng nước đầm lầy.
    [Range(0f, 100f)]
    public float swampRoomChance = 65f;

    // Tỉ lệ % diện tích phòng bị phủ nước độc. Generator kẹp về dải [20, 30]% để vừa
    // tạo hazard cục bộ, vừa chừa đủ sàn khô cho nhân vật tầm xa lấy góc bắn.
    [Range(0f, 100f)]
    public float swampWaterPercentage = 25f;

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

    // Cạnh tối thiểu cho cả map lẫn phòng - bảo đảm phòng không nhỏ hơn 12x12 để
    // có tầm nhìn thẳng và góc bắn rộng cho nhân vật tầm xa.
    public const int MinRoomDimension = 12;
}
