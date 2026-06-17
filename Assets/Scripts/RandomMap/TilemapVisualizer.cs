using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapVisualizer : MonoBehaviour
{
    [SerializeField]
    private Tilemap floorTilemap;

    [SerializeField]
    private Tilemap wallTilemap;

    [SerializeField]
    private TileBase floorTile;

    // Wall: ô tường phía trên - dùng khi ô này ở trên cùng của nhóm tường (floor dưới ô này)
    [SerializeField]
    private TileBase wallTop;

    // Wall: cạnh phải - dùng khi ô có sàn ở bên phải
    [SerializeField]
    private TileBase wallSideRight;

    // Wall: cạnh trái - dùng khi ô có sàn ở bên trái
    [SerializeField]
    private TileBase wallSiderLeft;

    // Wall: phía dưới - dùng khi ô này là đáy tường (sàn ở trên ô này)
    [SerializeField]
    private TileBase wallBottom;

    // Wall: tường đầy (được dùng khi ô này được bao quanh bởi sàn nhiều hướng)
    [SerializeField]
    private TileBase wallFull;

    [SerializeField]
    private TileBase wallInnerCornerDownLeft;

    // Inner corner: góc trong phía dưới-trái (sàn có xu hướng tạo góc lõm ở dưới trái)
    [SerializeField]
    private TileBase wallInnerCornerDownRight;

    // Inner corner: góc trong phía dưới-phải (sàn có xu hướng tạo góc lõm ở dưới phải)
    [SerializeField]
    private TileBase wallDiagonalCornerDownRight;

    // Diagonal corner: góc chéo xuống phải
    [SerializeField]
    private TileBase wallDiagonalCornerDownLeft;

    // Diagonal corner: góc chéo xuống trái
    [SerializeField]
    private TileBase wallDiagonalCornerUpRight;

    // Diagonal corner: góc chéo lên phải
    [SerializeField]
    private TileBase wallDiagonalCornerUpLeft;

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {
        PaintTiles(floorPositions, floorTilemap, floorTile);
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
        int typeAsInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;
        if (WallTypesHelper.wallTop.Contains(typeAsInt))
        {
            tile = wallTop;
        }else if (WallTypesHelper.wallSideRight.Contains(typeAsInt))
        {
            tile = wallSideRight;
        }
        else if (WallTypesHelper.wallSideLeft.Contains(typeAsInt))
        {
            tile = wallSiderLeft;
        }
        else if (WallTypesHelper.wallBottm.Contains(typeAsInt))
        {
            tile = wallBottom;
        }
        else if (WallTypesHelper.wallFull.Contains(typeAsInt))
        {
            tile = wallFull;
        }

        if (tile!=null)
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

    // Chuyển tọa độ ô (cell) trên bản đồ thành tọa độ tâm ô trong không gian thế giới (world),
    // dùng để đặt các GameObject (ví dụ: Cổng sinh quái) vào đúng giữa phòng.
    public Vector3 CellToWorldCenter(Vector2Int cellPosition)
    {
        return floorTilemap.GetCellCenterWorld((Vector3Int)cellPosition);
    }

    internal void PaintSingleCornerWall(Vector2Int position, string binaryType)
    {
        int typeASInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;

        if (WallTypesHelper.wallInnerCornerDownLeft.Contains(typeASInt))
        {
            tile = wallInnerCornerDownLeft;
        }
        else if (WallTypesHelper.wallInnerCornerDownRight.Contains(typeASInt))
        {
            tile = wallInnerCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownLeft.Contains(typeASInt))
        {
            tile = wallDiagonalCornerDownLeft;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownRight.Contains(typeASInt))
        {
            tile = wallDiagonalCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpRight.Contains(typeASInt))
        {
            tile = wallDiagonalCornerUpRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpLeft.Contains(typeASInt))
        {
            tile = wallDiagonalCornerUpLeft;
        }
        else if (WallTypesHelper.wallFullEightDirections.Contains(typeASInt))
        {
            tile = wallFull;
        }
        else if (WallTypesHelper.wallBottmEightDirections.Contains(typeASInt))
        {
            tile = wallBottom;
        }

        if (tile != null)
            PaintSingleTile(wallTilemap, tile, position);
    }
}