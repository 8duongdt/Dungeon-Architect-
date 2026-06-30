using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vẽ viền bờ quanh nước theo kiểu autotile 8 hướng: với MỖI ô bờ (ô sàn kề nước), xét 8 ô lân cận
/// có phải NƯỚC hay không rồi chọn tile bờ ở đúng mặt của hồ. Nhờ vậy mép nước có viền liền mạch
/// nhiều hướng thay vì một tile đồng nhất - đây là "vùng bao quanh" nước (không có collider).
///
/// Quy ước cờ: <see cref="TileNeighbors"/> mang nghĩa true = phía đó LÀ nước. Khâu chọn tile
/// (<see cref="TilemapVisualizer.PaintShoreTile"/>) đặt ô bờ ở mặt ĐỐI DIỆN với nước.
/// </summary>
public static class ShoreGeneration
{
    public static void CreateShoreTiles(
        IEnumerable<Vector2Int> shoreCells,
        HashSet<Vector2Int> waterCells,
        TilemapVisualizer tilemapVisualizer)
    {
        foreach (Vector2Int position in shoreCells)
        {
            tilemapVisualizer.PaintShoreTile(position, WaterNeighborsOf(position, waterCells));
        }
    }

    // 8 ô lân cận của một ô bờ có phải NƯỚC hay không. true = LÀ nước; false = đất/biên.
    private static TileNeighbors WaterNeighborsOf(Vector2Int p, HashSet<Vector2Int> water)
    {
        return new TileNeighbors(
            north: water.Contains(p + Vector2Int.up),
            east: water.Contains(p + Vector2Int.right),
            south: water.Contains(p + Vector2Int.down),
            west: water.Contains(p + Vector2Int.left),
            northEast: water.Contains(p + new Vector2Int(1, 1)),
            northWest: water.Contains(p + new Vector2Int(-1, 1)),
            southEast: water.Contains(p + new Vector2Int(1, -1)),
            southWest: water.Contains(p + new Vector2Int(-1, -1)));
    }
}
