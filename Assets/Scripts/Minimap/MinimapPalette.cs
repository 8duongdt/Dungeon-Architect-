using System;
using UnityEngine;

/// <summary>
/// Bảng màu mini-map: gán màu cho từng loại ô khi dựng texture nền. Tách riêng để chỉnh
/// trong Inspector mà không phải sửa code (tránh hardcode magic color).
/// </summary>
[Serializable]
public struct MinimapPalette
{
    [Tooltip("Màu ô trống / ngoài map.")]
    public Color empty;

    [Tooltip("Màu ô sàn đi lại được.")]
    public Color floor;

    [Tooltip("Màu ô tường (vật cản).")]
    public Color wall;

    [Tooltip("Màu vũng nước đầm lầy.")]
    public Color swampWater;

    [Tooltip("Màu ô cổng/portal.")]
    public Color gate;

    /// <summary>Bảng màu mặc định hợp lý nếu chưa cấu hình trong Inspector.</summary>
    public static MinimapPalette Default => new MinimapPalette
    {
        empty = new Color(0f, 0f, 0f, 0f),
        floor = new Color(0.55f, 0.52f, 0.45f, 1f),
        wall = new Color(0.13f, 0.12f, 0.15f, 1f),
        swampWater = new Color(0.20f, 0.45f, 0.55f, 1f),
        gate = new Color(0.95f, 0.80f, 0.20f, 1f)
    };

    public readonly Color ColorFor(TileType tile)
    {
        switch (tile)
        {
            case TileType.Floor: return floor;
            case TileType.Wall: return wall;
            case TileType.SwampWater: return swampWater;
            case TileType.Gate: return gate;
            default: return empty;
        }
    }
}
