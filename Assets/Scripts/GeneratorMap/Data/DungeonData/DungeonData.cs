using UnityEngine;

// Cấu hình sinh dungeon bằng thuật toán Cellular Automata.
// Chỉ chứa dữ liệu cấu hình - không chứa logic sinh map hay vẽ tile.
[CreateAssetMenu(fileName = "DungeonData_", menuName = "PCG/DungeonData")]
public class DungeonData : ScriptableObject
{
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
    // Cấu hình cho UndeadSwampGenerator (thuật toán Room-First, map vuông).
    // Tách riêng khỏi phần Cellular Automata ở trên; hai pipeline dùng chung asset.
    // ---------------------------------------------------------------------

    [Header("Undead Swamp - Bản đồ vuông")]
    // Cạnh của bản đồ vuông (số ô). Map vuông squareMapSize x squareMapSize.
    [Min(MinRoomDimension)]
    public int squareMapSize = 100;

    [Header("Undead Swamp - Phòng (Room-First)")]
    // Cạnh nhỏ nhất của một phòng. Ép sàn tối thiểu 10x10 cho tầm bắn xa và góc bắn rộng.
    [Min(MinRoomDimension)]
    public int minRoomSize = 12;

    // Cạnh lớn nhất của một phòng (phòng to, thoáng, ít vật cản bên trong).
    [Min(MinRoomDimension)]
    public int maxRoomSize = 20;

    // Số phòng mục tiêu cần đặt.
    [Min(1)]
    public int maxRooms = 8;

    // Khoảng đệm tối thiểu (số ô tường) giữa hai phòng để chúng không dính nhau.
    [Min(0)]
    public int roomPadding = 3;

    // Bề rộng hành lang nối các phòng (hành lang rộng cho nhân vật tầm xa cơ động).
    [Min(1)]
    public int hallwayWidth = 4;

    [Header("Undead Swamp - Vũng nước đầm lầy")]
    // Tỉ lệ % mỗi phòng được carve ra vũng nước đầm lầy.
    [Range(0f, 100f)]
    public float swampRoomChance = 65f;

    // Tỉ lệ % diện tích lõi phòng bị phủ nước (mục tiêu, chia đều cho các vũng).
    [Range(0f, 100f)]
    public float swampCoverage = 30f;

    // Số vũng nước rời nhau tối đa trong một phòng.
    [Min(1)]
    public int swampBlobCount = 2;

    [Header("Undead Swamp - Tỉ lệ sinh quái Undead (%)")]
    // Tỉ lệ % một ô sàn hợp lệ được dùng để sinh quái Undead (do hệ thống spawn đọc, không thuộc layout).
    [Range(0f, 100f)]
    public float undeadSpawnRate = 10f;

    // Cạnh tối thiểu cho cả map lẫn phòng - bảo đảm phòng không nhỏ hơn 10x10.
    public const int MinRoomDimension = 10;
}
