using System;
using UnityEngine;

/// <summary>
/// Lưới ô vuông 2D thuần (không phụ thuộc Unity world-math). Chỉ lưu trạng thái chiếm dụng của
/// từng ô; mọi quy đổi tọa độ world↔cell đi qua <see cref="TilemapVisualizer"/> để luôn khớp với
/// Tilemap dungeon (nguồn quy đổi duy nhất).
/// </summary>
public class GridXY<TGridObject>
{
    private readonly int width;
    private readonly int height;
    private readonly TGridObject[,] gridArray;

    public int Width => width;
    public int Height => height;

    public GridXY(int width, int height, Func<GridXY<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;

        gridArray = new TGridObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridArray[x, y] = createGridObject(this, x, y);
            }
        }
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public bool IsInBounds(Vector2Int cell)
    {
        return IsInBounds(cell.x, cell.y);
    }

    // Trả về default (null với class) nếu ô nằm ngoài lưới - caller tự kiểm tra null.
    public TGridObject GetGridObject(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return default;
        }
        return gridArray[x, y];
    }

    public TGridObject GetGridObject(Vector2Int cell)
    {
        return GetGridObject(cell.x, cell.y);
    }
}
