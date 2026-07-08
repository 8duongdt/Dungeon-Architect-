using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Đặt Filter Mode = Point, Compression = None cho toàn bộ sprite trong 3 gói
/// hiệu ứng pixel-art: Foozle Pixel Magic Effects, Magic Pack, Super Pixel Effects Gigapack.
/// Chỉ chỉnh đúng 2 thuộc tính này, KHÔNG đụng tới kiểu texture / slice sprite đã có.
///   Menu: Tools > Sprites > Fix Pixel VFX Import (Point + No Compression)
/// </summary>
public static class PixelMagicVfxImporter
{
    private const string ProgressTitle = "Fix Pixel VFX Import";

    private static readonly string[] TargetFolders =
    {
        "Assets/Sprites/Foozle_2DE0001_Pixel_Magic_Effects",
        "Assets/Sprites/Magic Pack",
        "Assets/Sprites/Super Pixel Effects Gigapack",
    };

    [MenuItem("Tools/Sprites/Fix Pixel VFX Import (Point + No Compression)")]
    public static void ApplyAll()
    {
        string[] assetPaths = CollectPngAssetPaths();
        if (assetPaths.Length == 0)
        {
            Debug.LogWarning("[PixelMagicVfxImporter] Không tìm thấy PNG nào trong các gói VFX.");
            return;
        }

        int changed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i];
                EditorUtility.DisplayProgressBar(ProgressTitle, assetPath, (float)i / assetPaths.Length);
                if (ApplyPixelSettings(assetPath))
                {
                    changed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[PixelMagicVfxImporter] Hoàn tất: {changed}/{assetPaths.Length} sprite đã đặt Point + No Compression.");
    }

    /// <summary>
    /// Chỉ đổi filterMode + textureCompression, giữ nguyên slice/sprite mode đang có.
    /// </summary>
    private static bool ApplyPixelSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[PixelMagicVfxImporter] Không phải texture: {assetPath}");
            return false;
        }

        bool alreadyCorrect = importer.filterMode == FilterMode.Point
            && importer.textureCompression == TextureImporterCompression.Uncompressed;
        if (alreadyCorrect)
        {
            return false;
        }

        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return true;
    }

    private static string[] CollectPngAssetPaths()
    {
        var assetPaths = new System.Collections.Generic.List<string>();
        string projectRoot = Directory.GetCurrentDirectory();

        foreach (string folder in TargetFolders)
        {
            string absoluteFolder = Path.Combine(projectRoot, folder);
            if (!Directory.Exists(absoluteFolder))
            {
                Debug.LogWarning($"[PixelMagicVfxImporter] Bỏ qua thư mục không tồn tại: {folder}");
                continue;
            }

            foreach (string absolutePng in Directory.GetFiles(absoluteFolder, "*.png", SearchOption.AllDirectories))
            {
                assetPaths.Add(ToAssetPath(absolutePng));
            }
        }

        return assetPaths.ToArray();
    }

    private static string ToAssetPath(string absolutePath)
    {
        string normalized = absolutePath.Replace('\\', '/');
        int index = normalized.IndexOf("Assets/");
        return index >= 0 ? normalized.Substring(index) : normalized;
    }
}
