using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Công cụ slice các sprite sheet trong thư mục "Sprites/Environment/dungeon asset".
///
/// Cách dùng:
///   1. Menu Tools > Dungeon > Slice Environment Sheets  -> slice toàn bộ theo cấu hình bên dưới.
///   2. Mỗi sheet được slice bằng lưới 16x16 (Grid) hoặc tự nhận diện vật thể (Auto).
///   3. Tên sprite được đọc từ file Assets/Editor/SliceNames/<tên_sheet>.txt
///      (mỗi dòng là tên cho 1 sprite, theo thứ tự trên->dưới, trái->phải).
///      Dòng trống hoặc "-" sẽ dùng tên mặc định "<tên_sheet>_<chỉ số>".
///   4. Sau khi slice, công cụ ghi báo cáo Assets/Editor/SliceNames/<tên_sheet>.report.txt
///      liệt kê chỉ số + toạ độ rect để đối chiếu/đặt tên.
/// </summary>
public static class EnvironmentSpriteSlicer
{
    private const string DungeonAssetFolder = "Assets/Sprites/Environment/dungeon asset";
    private const string NamesFolder = "Assets/Editor/SliceNames";
    private const int PixelsPerUnit = 16;
    private const int CellSize = 16;

    private enum SliceMode { Grid16, AutoObjects }

    private struct SheetConfig
    {
        public string FileName;
        public SliceMode Mode;
        public SheetConfig(string fileName, SliceMode mode) { FileName = fileName; Mode = mode; }
    }

    // Cấu hình slice theo loại sheet (quyết định bởi người dùng):
    //  - Tile / animation sheets  -> lưới 16x16
    //  - Vật thể rời (props)      -> tự nhận diện vật thể
    private static readonly SheetConfig[] Sheets =
    {
        new SheetConfig("walls_floor.png",                      SliceMode.Grid16),
        new SheetConfig("Objects.png",                          SliceMode.AutoObjects),
        new SheetConfig("download.png",                         SliceMode.AutoObjects),
        new SheetConfig("fire_animation.png",                   SliceMode.Grid16),
        new SheetConfig("fire_animation2.png",                  SliceMode.Grid16),
        new SheetConfig("trap_animation.png",                   SliceMode.Grid16),
        new SheetConfig("Water_coasts_animation.png",           SliceMode.Grid16),
        new SheetConfig("water_detilazation_v2.png",            SliceMode.Grid16),
        new SheetConfig("doors_lever_chest_animation.png",      SliceMode.Grid16),
        new SheetConfig("decorative_cracks_floor.png",          SliceMode.Grid16),
        new SheetConfig("decorative_cracks_walls.png",          SliceMode.Grid16),
        new SheetConfig("decorative_cracks_coasts_animation.png", SliceMode.Grid16),
    };

    [MenuItem("Tools/Dungeon/Slice Environment Sheets")]
    public static void SliceAll()
    {
        if (!Directory.Exists(NamesFolder))
        {
            Directory.CreateDirectory(NamesFolder);
        }

        int ok = 0;
        try
        {
            for (int i = 0; i < Sheets.Length; i++)
            {
                SheetConfig sheet = Sheets[i];
                EditorUtility.DisplayProgressBar("Slice Environment Sheets", sheet.FileName, (float)i / Sheets.Length);
                if (SliceSheet(sheet))
                {
                    ok++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        Debug.Log($"[EnvironmentSpriteSlicer] Hoàn tất: {ok}/{Sheets.Length} sheet được slice. Báo cáo ở {NamesFolder}.");
    }

    private static bool SliceSheet(SheetConfig sheet)
    {
        string path = $"{DungeonAssetFolder}/{sheet.FileName}";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[EnvironmentSpriteSlicer] Không tìm thấy texture: {path}");
            return false;
        }

        // Đảm bảo cấu hình import đồng nhất cho pixel art + cho phép đọc pixel để slice.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        bool wasReadable = importer.isReadable;
        importer.isReadable = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogWarning($"[EnvironmentSpriteSlicer] Không load được Texture2D: {path}");
            return false;
        }

        Rect[] rects = sheet.Mode == SliceMode.Grid16
            ? InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.zero, new Vector2(CellSize, CellSize), Vector2.zero, false)
            : InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, 8, 0);

        if (rects == null || rects.Length == 0)
        {
            Debug.LogWarning($"[EnvironmentSpriteSlicer] Không tạo được rect nào cho {sheet.FileName}");
            return false;
        }

        // Sắp xếp theo hàng từ trên xuống, trong hàng từ trái sang phải.
        // (rect.y tính từ đáy ảnh nên 'trên' = y lớn hơn.)
        List<Rect> ordered = OrderTopLeftToBottomRight(rects);

        string baseName = Path.GetFileNameWithoutExtension(sheet.FileName);
        List<string> names = LoadNames(baseName);

        var spriteRects = new SpriteRect[ordered.Count];
        var usedNames = new HashSet<string>();
        for (int i = 0; i < ordered.Count; i++)
        {
            string name = (names != null && i < names.Count && !string.IsNullOrWhiteSpace(names[i]) && names[i].Trim() != "-")
                ? names[i].Trim()
                : $"{baseName}_{i}";
            name = MakeUnique(name, usedNames);

            spriteRects[i] = new SpriteRect
            {
                name = name,
                spriteID = GUID.Generate(),
                rect = ordered[i],
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
            };
        }

        ApplySpriteRects(importer, spriteRects);

        // Trả lại trạng thái isReadable ban đầu để không tăng bộ nhớ build ngoài ý muốn.
        importer.isReadable = wasReadable;
        importer.SaveAndReimport();

        WriteReport(baseName, spriteRects);
        return true;
    }

    private static List<Rect> OrderTopLeftToBottomRight(Rect[] rects)
    {
        // Gom các rect thành hàng dựa trên chồng lấn theo trục y, rồi sort mỗi hàng theo x.
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

    private static List<string> LoadNames(string baseName)
    {
        string namesPath = $"{NamesFolder}/{baseName}.txt";
        if (!File.Exists(namesPath))
        {
            return null;
        }

        return File.ReadAllLines(namesPath)
            .Select(line => line.Split('#')[0]) // cho phép comment sau dấu #
            .ToList();
    }

    private static string MakeUnique(string name, HashSet<string> used)
    {
        if (used.Add(name))
        {
            return name;
        }

        int suffix = 2;
        string candidate;
        do
        {
            candidate = $"{name}_{suffix++}";
        } while (!used.Add(candidate));
        return candidate;
    }

    private static void WriteReport(string baseName, SpriteRect[] spriteRects)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {baseName} - {spriteRects.Length} sprites (thứ tự: trên->dưới, trái->phải)");
        sb.AppendLine("# index  name  | x y w h");
        for (int i = 0; i < spriteRects.Length; i++)
        {
            Rect r = spriteRects[i].rect;
            sb.AppendLine($"{i,4}  {spriteRects[i].name,-32} | {r.x} {r.y} {r.width} {r.height}");
        }

        File.WriteAllText($"{NamesFolder}/{baseName}.report.txt", sb.ToString());
    }
}
