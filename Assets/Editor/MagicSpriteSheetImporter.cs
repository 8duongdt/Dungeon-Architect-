using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Cấu hình import cho gói "10-magic-sprite-sheet-effects-pixel-art".
///
///   - 10 sheet hiệu ứng (mỗi thư mục số, vd "1 Lightning/Lightning.png"):
///     Sprite Multiple, cắt lưới theo KÍCH THƯỚC Ô 72x72, PPU 16,
///     pivot Bottom-Center, Point + No Compression.
///   - Icons/ (icon rời): giữ Single, chỉ đặt Point + No Compression.
///
///   Menu: Tools > Sprites > Config 10-Magic Sprite Sheets
///
/// Lưu ý: cắt theo ô cố định 72x72. Nếu texture không chia hết cho 72, phần dư sẽ bị bỏ qua
/// theo cách Unity tạo lưới sprite.
/// </summary>
public static class MagicSpriteSheetImporter
{
    private const string RootFolder = "Assets/Sprites/10-magic-sprite-sheet-effects-pixel-art";
    private const string IconsFolderToken = "/Icons/";
    private const int PixelsPerUnit = 16;
    private const int CellWidth = 72;
    private const int CellHeight = 72;

    private static readonly Vector2 BottomCenterPivot = new Vector2(0.5f, 0f);

    [MenuItem("Tools/Sprites/Config 10-Magic Sprite Sheets")]
    public static void ApplyAll()
    {
        string absoluteRoot = Path.Combine(Directory.GetCurrentDirectory(), RootFolder);
        if (!Directory.Exists(absoluteRoot))
        {
            Debug.LogError($"[MagicSpriteSheetImporter] Không tìm thấy thư mục: {RootFolder}");
            return;
        }

        List<string> sheetPaths = new List<string>();
        List<string> iconPaths = new List<string>();
        ClassifyPngAssets(absoluteRoot, sheetPaths, iconPaths);

        int slicedSheets = ConfigureSheets(sheetPaths);
        int fixedIcons = ConfigureIcons(iconPaths);

        AssetDatabase.Refresh();
        Debug.Log($"[MagicSpriteSheetImporter] Hoàn tất: {slicedSheets}/{sheetPaths.Count} sheet cắt lưới {CellWidth}x{CellHeight}, "
            + $"{fixedIcons}/{iconPaths.Count} icon đặt Point + No Compression.");
    }

    private static int ConfigureSheets(List<string> sheetPaths)
    {
        int done = 0;
        try
        {
            for (int i = 0; i < sheetPaths.Count; i++)
            {
                string assetPath = sheetPaths[i];
                EditorUtility.DisplayProgressBar("Config 10-Magic Sprite Sheets", assetPath, (float)i / sheetPaths.Count);
                if (SliceSheet(assetPath))
                {
                    done++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        return done;
    }

    private static bool SliceSheet(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[MagicSpriteSheetImporter] Không phải texture: {assetPath}");
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        bool wasReadable = importer.isReadable;
        importer.isReadable = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogWarning($"[MagicSpriteSheetImporter] Không load được Texture2D: {assetPath}");
            return false;
        }

        Rect[] rects = GenerateGridByCellCount(texture);
        if (rects == null || rects.Length == 0)
        {
            Debug.LogWarning($"[MagicSpriteSheetImporter] Không tạo được rect nào cho {assetPath}");
            return false;
        }

        List<Rect> ordered = OrderTopLeftToBottomRight(rects);
        SpriteRect[] spriteRects = BuildSpriteRects(ordered, Path.GetFileNameWithoutExtension(assetPath));
        ApplySpriteRects(importer, spriteRects);

        importer.isReadable = wasReadable;
        importer.SaveAndReimport();
        return true;
    }

    /// <summary>
    /// Cắt "Grid By Cell Size" 72x72.
    /// </summary>
    private static Rect[] GenerateGridByCellCount(Texture2D texture)
    {
        var cellSize = new Vector2(CellWidth, CellHeight);
        return InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.zero, cellSize, Vector2.zero, true);
    }

    private static SpriteRect[] BuildSpriteRects(List<Rect> ordered, string baseName)
    {
        var spriteRects = new SpriteRect[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            spriteRects[i] = new SpriteRect
            {
                name = $"{baseName}_{i}",
                spriteID = GUID.Generate(),
                rect = ordered[i],
                alignment = SpriteAlignment.BottomCenter,
                pivot = BottomCenterPivot,
                border = Vector4.zero,
            };
        }
        return spriteRects;
    }

    private static List<Rect> OrderTopLeftToBottomRight(Rect[] rects)
    {
        var sortedByTop = rects.OrderByDescending(r => r.y + r.height).ToList();
        var rows = new List<List<Rect>>();
        foreach (Rect r in sortedByTop)
        {
            float centerY = r.y + r.height * 0.5f;
            List<Rect> row = rows.FirstOrDefault(existing =>
            {
                Rect rep = existing[0];
                return centerY <= rep.y + rep.height && centerY >= rep.y;
            });
            if (row == null)
            {
                row = new List<Rect>();
                rows.Add(row);
            }
            row.Add(r);
        }

        var result = new List<Rect>();
        foreach (var row in rows)
        {
            result.AddRange(row.OrderBy(r => r.x));
        }
        return result;
    }

    private static void ApplySpriteRects(TextureImporter importer, SpriteRect[] spriteRects)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        provider.SetSpriteRects(spriteRects);

        var nameIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
        {
            var pairs = spriteRects.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)).ToList();
            nameIdProvider.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
    }

    private static int ConfigureIcons(List<string> iconPaths)
    {
        int done = 0;
        foreach (string assetPath in iconPaths)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            done++;
        }
        return done;
    }

    private static void ClassifyPngAssets(string absoluteRoot, List<string> sheetPaths, List<string> iconPaths)
    {
        foreach (string absolutePng in Directory.GetFiles(absoluteRoot, "*.png", SearchOption.AllDirectories))
        {
            string assetPath = ToAssetPath(absolutePng);
            if (assetPath.Contains(IconsFolderToken))
            {
                iconPaths.Add(assetPath);
            }
            else
            {
                sheetPaths.Add(assetPath);
            }
        }
    }

    private static string ToAssetPath(string absolutePath)
    {
        string normalized = absolutePath.Replace('\\', '/');
        int index = normalized.IndexOf("Assets/");
        return index >= 0 ? normalized.Substring(index) : normalized;
    }
}
