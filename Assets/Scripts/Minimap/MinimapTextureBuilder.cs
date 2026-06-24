using UnityEngine;

/// <summary>
/// Dựng <see cref="Texture2D"/> nền mini-map từ lưới <see cref="TileType"/> mà generator sinh ra:
/// mỗi ô = 1 pixel, màu lấy theo <see cref="MinimapPalette"/>. Không dùng camera phụ nên không cần
/// thêm Layer hay xử lý ánh sáng 2D. Pixel (0,0) trùng ô lưới (0,0) -> RawImage hiện đúng chiều.
/// </summary>
public static class MinimapTextureBuilder
{
    public static Texture2D Build(TileType[,] map, MinimapPalette palette)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pixels[y * width + x] = palette.ColorFor(map[x, y]);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
