using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Một "thể loại map": gom toàn bộ dữ liệu để sinh ra một kiểu dungeon riêng biệt.
///   - Bộ tile (floor + các biến thể tường) -> quyết định hình ảnh.
///   - DungeonData -> quyết định kích thước/bố cục khi sinh.
///   - Prefab trang trí (đuốc/rương) -> quyết định vật thể rải trong map.
/// Chỉ chứa dữ liệu cấu hình, không chứa logic.
/// </summary>
[CreateAssetMenu(fileName = "MapTheme_", menuName = "PCG/MapTheme")]
public class MapThemeSO : ScriptableObject
{
    [Header("Cấu hình sinh map")]
    public DungeonData dungeonData;

    [Header("Tile sàn / tường (khớp với TilemapVisualizer)")]
    public TileBase floorTile;
    public TileBase wallTop;
    public TileBase wallSideRight;
    public TileBase wallSideLeft;
    public TileBase wallBottom;
    public TileBase wallFull;
    public TileBase wallInnerCornerDownLeft;
    public TileBase wallInnerCornerDownRight;
    public TileBase wallDiagonalCornerDownRight;
    public TileBase wallDiagonalCornerDownLeft;
    public TileBase wallDiagonalCornerUpRight;
    public TileBase wallDiagonalCornerUpLeft;

    [Header("Nước đầm lầy (Undead)")]
    [Tooltip("Tile vẽ cho ô nước (SwampWater) - phủ trên sàn, KHÔNG có collider (chỉ làm chậm).")]
    public TileBase waterTile;

    // =========================================================================
    // Viền bờ quanh nước - autotile 8 hướng. Tile đặt tên theo MẶT CỦA HỒ mà ô bờ nằm:
    // shoreN = bờ Bắc (nằm phía trên hồ, nước ở dưới/Nam). Khâu chọn đặt ô bờ ở mặt đối diện nước.
    // Đây là "vùng bao quanh" nước - vẽ trên layer trang trí nên KHÔNG chặn di chuyển.
    // =========================================================================
    [Header("Viền bờ nước - 8 hướng (autotile)")]
    [Tooltip("Viền bờ - ô GIỮA. Cũng là tile mặc định khi thiếu biến thể hướng.")]
    public TileBase shoreTile;
    [Tooltip("Bờ Bắc: ô nằm TRÊN hồ (nước ở phía Nam của ô).")]
    public TileBase shoreN;
    [Tooltip("Bờ Nam: ô nằm DƯỚI hồ (nước ở phía Bắc của ô).")]
    public TileBase shoreS;
    [Tooltip("Bờ Tây: ô nằm bên TRÁI hồ (nước ở phía Đông của ô).")]
    public TileBase shoreW;
    [Tooltip("Bờ Đông: ô nằm bên PHẢI hồ (nước ở phía Tây của ô).")]
    public TileBase shoreE;
    [Tooltip("Góc lồi trên-trái của hồ (nước ở đường chéo Đông-Nam của ô).")]
    public TileBase shoreNW;
    [Tooltip("Góc lồi trên-phải của hồ (nước ở đường chéo Tây-Nam của ô).")]
    public TileBase shoreNE;
    [Tooltip("Góc lồi dưới-trái của hồ (nước ở đường chéo Đông-Bắc của ô).")]
    public TileBase shoreSW;
    [Tooltip("Góc lồi dưới-phải của hồ (nước ở đường chéo Tây-Bắc của ô).")]
    public TileBase shoreSE;

    [Header("Prefab trang trí")]
    public GameObject torchPrefab;
    public GameObject chestPrefab;

    [Header("Cổng sinh quái (Room-First)")]
    // Cổng (EnemySpawner) rải vào tâm một vài phòng khi sinh map kiểu Room-First.
    public GameObject portalPrefab;

    // Số cổng tối đa; sẽ bị giới hạn theo số phòng hiện có.
    [Min(0)]
    public int portalCount = 1;
}
