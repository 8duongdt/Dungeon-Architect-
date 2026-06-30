using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hạt nhân đặt vật dùng chung cho cả decorator lẫn spawner gameplay. Trả lời một câu duy nhất cho
/// mọi nơi cần đặt vật lên map: "ô này có ĐI LẠI ĐƯỢC và CÒN TRỐNG không? nếu có thì tâm ô ở đâu?".
///
/// Nhờ đi qua đây mà không vật nào bị kẹt trong tường hay nằm chênh giữa hai block:
///   - walkable = tra <see cref="TileType"/> trong tập ô đi lại được (mặc định: Floor/SwampWater/Gate).
///   - snap tâm ô = <see cref="TilemapVisualizer.CellToWorldCenter"/> (nguồn toạ độ DUY NHẤT).
///   - chống chồng = đánh dấu ô đã chiếm sau mỗi lần đặt.
/// Ngoài ra gom sẵn các phép phụ trợ cho luật spawn: bán kính an toàn và nhiễu Perlin (gom cụm).
/// </summary>
public class MapPlacement
{
    // Tập TileType được coi là đi lại được (đặt vật được). Nước/cổng đi lên được; tường là vật cản
    // đặc nên KHÔNG nằm trong đây.
    private static readonly TileType[] DefaultWalkableTypes =
    {
        TileType.Floor, TileType.SwampWater, TileType.Gate
    };

    private readonly TileType[,] map;
    private readonly TilemapVisualizer visualizer;
    private readonly HashSet<TileType> walkableTypes;
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private readonly int width;
    private readonly int height;

    public MapPlacement(TileType[,] map, TilemapVisualizer visualizer)
        : this(map, visualizer, DefaultWalkableTypes)
    {
    }

    public MapPlacement(TileType[,] map, TilemapVisualizer visualizer, TileType[] walkableTypes)
    {
        this.map = map;
        this.visualizer = visualizer;
        this.walkableTypes = new HashSet<TileType>(walkableTypes);
        width = map.GetLength(0);
        height = map.GetLength(1);
    }

    /// <summary>Ô đi lại được (theo TileType) và nằm trong biên map.</summary>
    public bool IsWalkable(Vector2Int cell)
    {
        return IsInBounds(cell) && walkableTypes.Contains(map[cell.x, cell.y]);
    }

    /// <summary>Ô đi lại được VÀ chưa bị vật nào chiếm.</summary>
    public bool IsFree(Vector2Int cell)
    {
        return IsWalkable(cell) && !occupied.Contains(cell);
    }

    /// <summary>Đánh dấu ô đã có vật để các luật đặt sau không chồng lên.</summary>
    public void Occupy(Vector2Int cell)
    {
        occupied.Add(cell);
    }

    /// <summary>Tâm ô trong không gian thế giới - vật luôn snap đúng giữa ô.</summary>
    public Vector3 CellCenter(Vector2Int cell)
    {
        return visualizer.CellToWorldCenter(cell);
    }

    public void ClearOccupied()
    {
        occupied.Clear();
    }

    // ----- Phụ trợ cho luật spawn -----

    /// <summary>true nếu ô nằm trong bán kính an toàn quanh điểm xuất phát người chơi (cấm spawn).</summary>
    public static bool WithinSafeRadius(Vector2Int cell, Vector2Int playerCell, float radius)
    {
        return Vector2Int.Distance(cell, playerCell) < radius;
    }

    /// <summary>Giá trị nhiễu Perlin [0,1] tại ô - dùng để gom vật thành cụm tự nhiên.</summary>
    public static float Perlin(Vector2Int cell, float scale, Vector2 offset)
    {
        return Mathf.PerlinNoise((cell.x + offset.x) * scale, (cell.y + offset.y) * scale);
    }

    private bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
    }
}
