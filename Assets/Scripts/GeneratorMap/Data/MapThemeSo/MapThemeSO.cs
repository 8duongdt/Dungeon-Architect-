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

    [Header("Prefab trang trí")]
    public GameObject torchPrefab;
    public GameObject chestPrefab;
}
