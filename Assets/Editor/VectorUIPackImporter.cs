using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Áp dụng cấu hình import + 9-slice cho gói "Vector_UI_Pack_dobo_ui".
///
/// Cách dùng:
///   Menu Tools > UI > Apply Vector UI Pack Settings
///
/// Quy tắc:
///   - Mỗi PNG rời được import dạng Sprite (2D/UI), Single mode, không nén,
///     lọc Bilinear, alpha-is-transparency (UI nét, không bị mờ/nén).
///   - Các phần tử khung chữ nhật (nút, panel, container, modal, card, header,
///     ô đồ, label, thanh tiến trình) được set 9-slice border tự động bằng cách
///     đo bán kính bo góc từ viền alpha, để co giãn không méo.
///   - Nút tròn (Circle/Exit), mũi tên, hiệu ứng, icon: KHÔNG set border.
///   - Hai spritesheet gộp khổ lớn (Spritesheets/, Icons/icon_spritesheet/) vượt
///     2048px và đã có sprite rời tương ứng -> BỎ QUA, không đụng vào.
/// </summary>
public static class VectorUIPackImporter
{
    private const string RootFolder = "Assets/Sprites/Vector_UI_Pack_dobo_ui";
    private const int PixelsPerUnit = 100;
    private const float OpaqueAlphaThreshold = 0.1f;
    private const int BorderSafetyMargin = 2;

    // Một cạnh chỉ được giữ 9-slice khi khung đối xứng và chừa phần co giãn đủ rộng
    // (loại các hình trang trí: header có ruy-băng, nút "pressed", label gắn icon...).
    private const float MaxBorderAsymmetryRatio = 0.15f;
    private const float MinStretchMiddleRatio = 0.25f;

    // Thư mục được bỏ qua hoàn toàn (spritesheet gộp khổ lớn, đã có sprite rời).
    private static readonly string[] ExcludedFolders =
    {
        "Spritesheets",
        "Icons/icon_spritesheet",
    };

    // Thư mục có phần tử khung chữ nhật -> bật 9-slice.
    private static readonly HashSet<string> NineSliceFolders = new HashSet<string>
    {
        "Buttons", "Cards", "Containers", "Headers", "Item Slots",
        "Labels", "Modals", "Panels", "ProgressBars",
    };

    [MenuItem("Tools/UI/Apply Vector UI Pack Settings")]
    public static void ApplyAll()
    {
        string absoluteRoot = Path.Combine(Directory.GetCurrentDirectory(), RootFolder);
        if (!Directory.Exists(absoluteRoot))
        {
            Debug.LogError($"[VectorUIPackImporter] Không tìm thấy thư mục: {RootFolder}");
            return;
        }

        string[] pngFiles = Directory.GetFiles(absoluteRoot, "*.png", SearchOption.AllDirectories);
        var report = new StringBuilder();
        report.AppendLine("# Vector UI Pack import report");
        report.AppendLine("# file | mode | border(L,B,R,T)");

        int processed = 0;
        try
        {
            for (int i = 0; i < pngFiles.Length; i++)
            {
                string assetPath = ToAssetPath(pngFiles[i]);
                EditorUtility.DisplayProgressBar("Apply Vector UI Pack Settings", assetPath, (float)i / pngFiles.Length);

                if (IsExcluded(assetPath))
                {
                    report.AppendLine($"{assetPath} | SKIPPED (packed spritesheet) |");
                    continue;
                }

                if (ApplyToSprite(assetPath, report))
                {
                    processed++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), RootFolder, "import_report.txt"), report.ToString());
        Debug.Log($"[VectorUIPackImporter] Hoàn tất: {processed} sprite đã cấu hình. Báo cáo: {RootFolder}/import_report.txt");
    }

    private static bool ApplyToSprite(string assetPath, StringBuilder report)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[VectorUIPackImporter] Không phải texture: {assetPath}");
            return false;
        }

        ApplyBaseSettings(importer);

        bool wantsBorder = WantsNineSlice(assetPath);
        Vector4 border = Vector4.zero;
        if (wantsBorder)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            border = texture != null ? DetectNineSliceBorder(texture) : Vector4.zero;

            importer.spriteBorder = border;
            importer.isReadable = false;
        }

        importer.SaveAndReimport();

        string mode = wantsBorder ? "9-slice" : "single";
        report.AppendLine($"{assetPath} | {mode} | {border.x},{border.y},{border.z},{border.w}");
        return true;
    }

    private static void ApplyBaseSettings(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }

    /// <summary>
    /// Đo border 9-slice bằng bán kính bo góc của viền alpha: phần co giãn an toàn
    /// nằm giữa, nên border mỗi cạnh = độ "thụt vào" lớn nhất của đường viền tại các góc.
    /// </summary>
    private static Vector4 DetectNineSliceBorder(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        byte alphaCutoff = (byte)(OpaqueAlphaThreshold * 255f);

        int[] leftOpaque = Filled(height, int.MaxValue);
        int[] rightOpaque = Filled(height, int.MinValue);
        int[] topOpaque = Filled(width, int.MinValue);
        int[] bottomOpaque = Filled(width, int.MaxValue);

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[rowStart + x].a < alphaCutoff)
                {
                    continue;
                }
                if (x < leftOpaque[y]) leftOpaque[y] = x;
                if (x > rightOpaque[y]) rightOpaque[y] = x;
                if (y > topOpaque[x]) topOpaque[x] = y;
                if (y < bottomOpaque[x]) bottomOpaque[x] = y;
            }
        }

        int bodyLeft = MinValid(leftOpaque, int.MaxValue);
        int bodyRight = MaxValid(rightOpaque, int.MinValue);
        int bodyTop = MaxValid(topOpaque, int.MinValue);
        int bodyBottom = MinValid(bottomOpaque, int.MaxValue);

        int left = MaxInset(leftOpaque, y => leftOpaque[y] - bodyLeft, height);
        int right = MaxInset(rightOpaque, y => bodyRight - rightOpaque[y], height);
        int top = MaxInset(topOpaque, x => bodyTop - topOpaque[x], width);
        int bottom = MaxInset(bottomOpaque, x => bottomOpaque[x] - bodyBottom, width);

        left = ClampBorder(left, width);
        right = ClampBorder(right, width);
        top = ClampBorder(top, height);
        bottom = ClampBorder(bottom, height);

        if (!IsCleanStretchAxis(left, right, width))
        {
            left = 0;
            right = 0;
        }
        if (!IsCleanStretchAxis(top, bottom, height))
        {
            top = 0;
            bottom = 0;
        }

        // Vector4 border: x=left, y=bottom, z=right, w=top.
        return new Vector4(left, bottom, right, top);
    }

    private static bool IsCleanStretchAxis(int borderA, int borderB, int dimension)
    {
        bool isSymmetric = Mathf.Abs(borderA - borderB) <= MaxBorderAsymmetryRatio * dimension;
        bool hasStretchMiddle = (dimension - borderA - borderB) >= MinStretchMiddleRatio * dimension;
        return isSymmetric && hasStretchMiddle;
    }

    private static int MaxInset(int[] edge, System.Func<int, int> insetAt, int count)
    {
        int max = 0;
        for (int i = 0; i < count; i++)
        {
            if (edge[i] == int.MaxValue || edge[i] == int.MinValue)
            {
                continue; // dòng/cột rỗng (không có pixel đục)
            }
            int inset = insetAt(i);
            if (inset > max) max = inset;
        }
        return max + BorderSafetyMargin;
    }

    private static int ClampBorder(int border, int dimension)
    {
        int maxBorder = (dimension / 2) - 1;
        if (maxBorder < 0) maxBorder = 0;
        return Mathf.Clamp(border, 0, maxBorder);
    }

    private static int[] Filled(int length, int value)
    {
        var array = new int[length];
        for (int i = 0; i < length; i++) array[i] = value;
        return array;
    }

    private static int MinValid(int[] values, int sentinel)
    {
        int result = int.MaxValue;
        foreach (int v in values)
        {
            if (v != sentinel && v < result) result = v;
        }
        return result == int.MaxValue ? 0 : result;
    }

    private static int MaxValid(int[] values, int sentinel)
    {
        int result = int.MinValue;
        foreach (int v in values)
        {
            if (v != sentinel && v > result) result = v;
        }
        return result == int.MinValue ? 0 : result;
    }

    private static bool WantsNineSlice(string assetPath)
    {
        string subFolder = SubFolderOf(assetPath);
        if (!NineSliceFolders.Contains(subFolder))
        {
            return false;
        }

        // Loại trừ nút tròn trong thư mục Buttons (không 9-slice được).
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        bool isRound = fileName.Contains("Circle") || fileName.Contains("Exit");
        return !isRound;
    }

    private static bool IsExcluded(string assetPath)
    {
        foreach (string excluded in ExcludedFolders)
        {
            if (assetPath.Contains($"{RootFolder}/{excluded}/"))
            {
                return true;
            }
        }
        return false;
    }

    private static string SubFolderOf(string assetPath)
    {
        string relative = assetPath.Substring(RootFolder.Length + 1);
        int slash = relative.IndexOf('/');
        return slash >= 0 ? relative.Substring(0, slash) : string.Empty;
    }

    private static string ToAssetPath(string absolutePath)
    {
        string normalized = absolutePath.Replace('\\', '/');
        int index = normalized.IndexOf(RootFolder);
        return index >= 0 ? normalized.Substring(index) : normalized;
    }
}
