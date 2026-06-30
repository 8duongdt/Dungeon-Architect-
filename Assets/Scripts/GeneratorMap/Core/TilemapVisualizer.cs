using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Vẽ sàn/tường/nước lên Tilemap. KHÔNG còn giữ bộ tile riêng - bộ tile là nguồn DUY NHẤT ở
/// <see cref="MapThemeSO"/>. DungeonManager gọi <see cref="SetTheme"/> trước khi vẽ để chọn diện mạo.
/// </summary>
public class TilemapVisualizer : MonoBehaviour
{
    [SerializeField]
    private Tilemap floorTilemap;

    [SerializeField]
    private Tilemap wallTilemap;

    [Tooltip("Layer phủ trang trí trên sàn (viền bờ nước) - không có collider.")]
    [SerializeField]
    private Tilemap decorationTilemap;

    [Tooltip("Layer nước (SwampWater) - phủ trên sàn, KHÔNG collider (chỉ làm chậm qua hazard prefab). "
        + "Bỏ trống sẽ vẽ chung lên layer trang trí.")]
    [SerializeField]
    private Tilemap waterTilemap;

    // Theme đang dùng để lấy tile sàn/tường. Gán qua SetTheme trước khi paint.
    private MapThemeSO activeTheme;

    // Layer vẽ nước: ưu tiên waterTilemap riêng, không có thì dùng chung layer trang trí.
    private Tilemap WaterLayer => waterTilemap != null ? waterTilemap : decorationTilemap;

    /// <summary>Chọn bộ tile (theme) sẽ dùng cho các lần paint kế tiếp.</summary>
    public void SetTheme(MapThemeSO theme)
    {
        activeTheme = theme;
    }

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {
        if (!HasTheme())
        {
            return;
        }

        PaintTiles(floorPositions, floorTilemap, activeTheme.floorTile);
    }

    /// <summary>Vẽ tile nước lên các ô SwampWater (phủ trên sàn, không có collider).</summary>
    public void PaintWaterTiles(IEnumerable<Vector2Int> waterPositions)
    {
        if (!HasTheme())
        {
            return;
        }

        if (activeTheme.waterTile == null)
        {
            Debug.LogWarning("[TilemapVisualizer] waterTile chưa gán trong MapThemeSO — nước không hiện.");
            return;
        }

        if (WaterLayer == null)
        {
            Debug.LogWarning("[TilemapVisualizer] waterTilemap và decorationTilemap đều chưa gán — nước không hiện.");
            return;
        }

        PaintTiles(waterPositions, WaterLayer, activeTheme.waterTile);
    }

    /// <summary>
    /// Vẽ một ô viền bờ quanh nước lên layer trang trí (trên sàn) - chọn tile cạnh/góc theo phía CÓ
    /// NƯỚC (<paramref name="waterNeighbors"/>) để mép nước tự nhiên.
    /// </summary>
    public void PaintShoreTile(Vector2Int position, TileNeighbors waterNeighbors)
    {
        if (!HasTheme())
        {
            return;
        }

        if (decorationTilemap == null)
        {
            Debug.LogWarning("[TilemapVisualizer] decorationTilemap chưa gán trong Inspector — viền bờ nước không hiện.");
            return;
        }

        TileBase tile = PickShoreTile(waterNeighbors);
        if (tile != null)
        {
            PaintSingleTile(decorationTilemap, tile, position);
        }
    }

    // Viền bờ: ô bờ nằm ở mặt ĐỐI DIỆN với nước (nước ở Nam -> ô là bờ Bắc của hồ = shoreN, v.v.).
    // Ưu tiên CẠNH (nước ở một hướng trục) rồi tới GÓC LỒI (nước chỉ ở một đường chéo - đặc trưng của
    // hồ chữ nhật). TileNeighbors ở đây mang nghĩa "phía đó LÀ nước".
    private TileBase PickShoreTile(TileNeighbors water)
    {
        if (water.South) return activeTheme.shoreN;
        if (water.North) return activeTheme.shoreS;
        if (water.East) return activeTheme.shoreW;
        if (water.West) return activeTheme.shoreE;

        if (water.SouthEast) return activeTheme.shoreNW;
        if (water.SouthWest) return activeTheme.shoreNE;
        if (water.NorthEast) return activeTheme.shoreSW;
        if (water.NorthWest) return activeTheme.shoreSE;

        return activeTheme.shoreTile;
    }

    private void PaintTiles(IEnumerable<Vector2Int> positions, Tilemap tilemap, TileBase tile)
    {
        foreach (var position in positions)
        {
            PaintSingleTile(tilemap, tile, position);
        }
    }

    internal void PaintSingleBasicWall(Vector2Int position, string binaryType)
    {
        if (!HasTheme())
        {
            return;
        }

        int typeAsInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;
        if (WallTypesHelper.wallTop.Contains(typeAsInt))
        {
            tile = activeTheme.wallTop;
        }
        else if (WallTypesHelper.wallSideRight.Contains(typeAsInt))
        {
            tile = activeTheme.wallSideRight;
        }
        else if (WallTypesHelper.wallSideLeft.Contains(typeAsInt))
        {
            tile = activeTheme.wallSideLeft;
        }
        else if (WallTypesHelper.wallBottm.Contains(typeAsInt))
        {
            tile = activeTheme.wallBottom;
        }
        else if (WallTypesHelper.wallFull.Contains(typeAsInt))
        {
            tile = activeTheme.wallFull;
        }

        if (tile != null)
            PaintSingleTile(wallTilemap, tile, position);
    }

    internal void PaintSingleCornerWall(Vector2Int position, string binaryType)
    {
        if (!HasTheme())
        {
            return;
        }

        int typeASInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;

        if (WallTypesHelper.wallInnerCornerDownLeft.Contains(typeASInt))
        {
            tile = activeTheme.wallInnerCornerDownLeft;
        }
        else if (WallTypesHelper.wallInnerCornerDownRight.Contains(typeASInt))
        {
            tile = activeTheme.wallInnerCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownLeft.Contains(typeASInt))
        {
            tile = activeTheme.wallDiagonalCornerDownLeft;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownRight.Contains(typeASInt))
        {
            tile = activeTheme.wallDiagonalCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpRight.Contains(typeASInt))
        {
            tile = activeTheme.wallDiagonalCornerUpRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpLeft.Contains(typeASInt))
        {
            tile = activeTheme.wallDiagonalCornerUpLeft;
        }
        else if (WallTypesHelper.wallFullEightDirections.Contains(typeASInt))
        {
            tile = activeTheme.wallFull;
        }
        else if (WallTypesHelper.wallBottmEightDirections.Contains(typeASInt))
        {
            tile = activeTheme.wallBottom;
        }

        if (tile != null)
            PaintSingleTile(wallTilemap, tile, position);
    }

    private void PaintSingleTile(Tilemap tilemap, TileBase tile, Vector2Int position)
    {
        // position LÀ chỉ số ô (cell), không phải toạ độ world - đặt tile thẳng vào ô đó.
        // (Trước đây WorldToCell((Vector3Int)position) chỉ đúng khi tilemap nằm ở gốc world;
        //  khi Grid bị dời, nó làm lệch toàn bộ map so với lưới logic.)
        tilemap.SetTile((Vector3Int)position, tile);
    }

    public void Clear()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        if (decorationTilemap != null)
        {
            decorationTilemap.ClearAllTiles();
        }
        if (waterTilemap != null)
        {
            waterTilemap.ClearAllTiles();
        }
    }

    // Chuyển tọa độ ô (cell) thành tọa độ tâm ô trong không gian thế giới (world),
    // dùng để đặt các GameObject (ví dụ: Cổng sinh quái) vào đúng giữa phòng.
    public Vector3 CellToWorldCenter(Vector2Int cellPosition)
    {
        return floorTilemap.GetCellCenterWorld((Vector3Int)cellPosition);
    }

    // Chuyển tọa độ world thành tọa độ ô (cell) - nghịch đảo của CellToWorldCenter, dùng cho
    // hệ thống xây dựng snap chuột về ô lưới.
    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        return (Vector2Int)floorTilemap.WorldToCell(worldPosition);
    }

    // Kích thước một ô lưới (theo trục X) của Tilemap - để hệ thống xây dựng căn footprint cho khớp.
    public float CellSize => floorTilemap.layoutGrid.cellSize.x;

    // Khung world (Rect) bao trọn lưới kích thước cellWidth x cellHeight bắt đầu từ ô (0,0).
    // Tính theo gốc và cellSize thực của tilemap nên đã gồm offset của Grid -> mini-map map
    // toạ độ world<->khung chính xác (không hardcode (0,0)).
    public Rect GetWorldBounds(int cellWidth, int cellHeight)
    {
        Vector3 origin = floorTilemap.CellToWorld(Vector3Int.zero);
        Vector3 cellSize = floorTilemap.layoutGrid.cellSize;
        return new Rect(origin.x, origin.y, cellWidth * cellSize.x, cellHeight * cellSize.y);
    }

    private bool HasTheme()
    {
        if (activeTheme == null)
        {
            Debug.LogError("[TilemapVisualizer] Chưa gọi SetTheme - không có bộ tile để vẽ.");
            return false;
        }
        return true;
    }
}
