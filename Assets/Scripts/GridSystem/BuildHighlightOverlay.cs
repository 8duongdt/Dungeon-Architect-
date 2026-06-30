using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Phủ màu lên các ô sàn khi đang ở chế độ xây để người chơi thấy ngay khu nào đặt được: ô sàn còn
/// trống tô xanh, ô đã có công trình tô đỏ. Tắt (xóa hết) khi thoát chế độ xây. Chỉ vẽ lại theo sự kiện
/// <see cref="GridBuildingSystem.BuildStateChanged"/> nên không tốn chi phí mỗi frame.
/// </summary>
public class BuildHighlightOverlay : MonoBehaviour
{
    [SerializeField]
    private GridBuildingSystem buildingSystem;

    [Tooltip("Tilemap riêng để phủ màu, đặt dưới cùng Grid với floor/wall tilemap.")]
    [SerializeField]
    private Tilemap overlayTilemap;

    [Tooltip("Tile ô vuông trắng - sẽ được nhân màu xanh/đỏ qua SetColor.")]
    [SerializeField]
    private TileBase highlightTile;

    [SerializeField]
    private Color freeColor = new Color(0f, 1f, 0f, 0.35f);

    [SerializeField]
    private Color occupiedColor = new Color(1f, 0f, 0f, 0.35f);

    private void OnEnable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.BuildStateChanged += Redraw;
        }
    }

    private void OnDisable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.BuildStateChanged -= Redraw;
        }
    }

    private void Start()
    {
        Redraw();
    }

    private void Redraw()
    {
        if (overlayTilemap == null || highlightTile == null)
        {
            return;
        }

        overlayTilemap.ClearAllTiles();
        if (!buildingSystem.IsBuildModeActive)
        {
            return;
        }

        for (int x = 0; x < buildingSystem.GridWidth; x++)
        {
            for (int y = 0; y < buildingSystem.GridHeight; y++)
            {
                var cell = new Vector2Int(x, y);
                if (buildingSystem.IsFloorCell(cell))
                {
                    PaintCell(cell, buildingSystem.IsCellOccupied(cell));
                }
            }
        }
    }

    private void PaintCell(Vector2Int cell, bool isOccupied)
    {
        // Ô lưới ở đây là ô của Tilemap sàn. Đổi qua tâm ô thế giới rồi quy về ô của overlayTilemap
        // để overlay luôn khớp sàn dù overlay nằm trên Grid/transform lệch (tránh lệch cố định).
        Vector3 world = buildingSystem.CellToWorldCenter(cell);
        Vector3Int position = overlayTilemap.WorldToCell(world);
        overlayTilemap.SetTile(position, highlightTile);
        overlayTilemap.SetTileFlags(position, TileFlags.None);
        overlayTilemap.SetColor(position, isOccupied ? occupiedColor : freeColor);
    }
}
