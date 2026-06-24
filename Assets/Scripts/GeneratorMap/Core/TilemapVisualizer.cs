using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Vẽ sàn/tường lên Tilemap. KHÔNG còn giữ bộ tile riêng - bộ tile là nguồn DUY NHẤT ở
/// <see cref="MapThemeSO"/>. DungeonManager gọi <see cref="SetTheme"/> trước khi vẽ để chọn diện mạo.
/// </summary>
public class TilemapVisualizer : MonoBehaviour
{
    [SerializeField]
    private Tilemap floorTilemap;

    [SerializeField]
    private Tilemap wallTilemap;

    // Theme đang dùng để lấy tile sàn/tường. Gán qua SetTheme trước khi paint.
    private MapThemeSO activeTheme;

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
        var tilePosition = tilemap.WorldToCell((Vector3Int)position);
        tilemap.SetTile(tilePosition, tile);
    }

    public void Clear()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    // Chuyển tọa độ ô (cell) thành tọa độ tâm ô trong không gian thế giới (world),
    // dùng để đặt các GameObject (ví dụ: Cổng sinh quái) vào đúng giữa phòng.
    public Vector3 CellToWorldCenter(Vector2Int cellPosition)
    {
        return floorTilemap.GetCellCenterWorld((Vector3Int)cellPosition);
    }

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
