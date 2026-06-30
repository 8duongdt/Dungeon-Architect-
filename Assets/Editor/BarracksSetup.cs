using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;

/// <summary>
/// Công cụ dựng 3 công trình "trại huấn luyện lính" (Crypt lv1 / Hellforge lv2 / Dark Zenith lv3)
/// từ các sprite trong Assets/Sprites/Placement. Mỗi lần chạy menu sẽ:
///   1. Re-import + slice (lưới 64x64 -> 1 sprite/ảnh vì ảnh 48x48, Point, không nén, PPU 16) cho
///      ảnh tĩnh (rotations) và toàn bộ frame animation của 3 công trình.
///   2. Tạo 1 AnimationClip lặp + 1 AnimatorController cho mỗi công trình (giống prefab Placement).
///   3. Dựng prefab (SpriteRenderer + Animator + UnitTrainingBuilding) đặt ở gốc, như prefab Placement.
///   4. Tạo PlacedObjectTypeSO cho mỗi công trình để thêm vào bảng lệnh xây (HUD).
/// Việc nối SO vào HUD và xoá placeholder làm ở bước riêng (thao tác trên scene).
/// </summary>
public static class BarracksSetup
{
    private const int PixelsPerUnit = 16;
    private const int CellSize = 64;
    private const int FrameRate = 12;
    private const int SortingOrder = 5;
    private const int FootprintCells = 3;

    private const string PlacementSpriteRoot = "Assets/Sprites/Placement";
    private const string PrefabFolder = "Assets/Prefabs/Placement";
    private const string AnimationFolder = "Assets/Animation/Placement";
    private const string DataFolder = "Assets/Data/Building";
    private const string MonsterFolder = "Assets/Prefabs/Monster";
    private const string LitSpriteMaterialGuid = "a97c105638bdf8b4a8650670310a4cd3";

    private struct UnitSpec
    {
        public string MonsterPrefab; // tên file trong MonsterFolder (không đuôi)
        public int Gold;
        public int Mana;
        public UnitSpec(string monsterPrefab, int gold, int mana) { MonsterPrefab = monsterPrefab; Gold = gold; Mana = mana; }
    }

    private struct BarracksConfig
    {
        public string Name;          // tên prefab/clip/controller/SO
        public string DisplayName;   // tên hiển thị trên HUD
        public string TopFolder;     // thư mục cấp 1 trong Placement
        public string EnvFolder;     // thư mục "*_Environme" bên trong
        public string Hotkey;
        public int BuildGold;
        public int BuildMana;
        public UnitSpec[] Units;     // 3 loại lính (Orc/Slime/Vampire) ở cấp tương ứng
    }

    [MenuItem("Tools/Dungeon/Build Barracks Buildings")]
    public static void BuildAll()
    {
        BarracksConfig[] configs = BuildConfigs();
        int built = 0;
        try
        {
            for (int i = 0; i < configs.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Build Barracks Buildings", configs[i].Name, (float)i / configs.Length);
                if (BuildBarracks(configs[i]))
                {
                    built++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BarracksSetup] Hoàn tất: {built}/{configs.Length} trại huấn luyện được dựng. " +
                  $"Prefab ở {PrefabFolder}, SO ở {DataFolder}. Nhớ chạy bước nối HUD.");
    }

    private static BarracksConfig[] BuildConfigs()
    {
        return new[]
        {
            new BarracksConfig
            {
                Name = "Crypt_of_Ruin", DisplayName = "Crypt of Ruin (Lv1)",
                TopFolder = "Crypt_of_Ruin_Base_Structure", EnvFolder = "Crypt_of_Ruin_Base_Structure_Environme",
                Hotkey = "E", BuildGold = 100, BuildMana = 0,
                Units = new[]
                {
                    new UnitSpec("Orc_1", 20, 0),
                    new UnitSpec("Slime", 25, 10),
                    new UnitSpec("Vampires_1", 15, 30),
                },
            },
            new BarracksConfig
            {
                Name = "Hellforge_Barracks", DisplayName = "Hellforge Barracks (Lv2)",
                TopFolder = "Hellforge_Barracks_Base_Struct", EnvFolder = "Hellforge_Barracks_Base_Structure_Envi",
                Hotkey = "R", BuildGold = 200, BuildMana = 50,
                Units = new[]
                {
                    new UnitSpec("Orc_2", 40, 0),
                    new UnitSpec("Ice_Slime", 50, 20),
                    new UnitSpec("Vampires_2", 30, 60),
                },
            },
            new BarracksConfig
            {
                Name = "Dark_Zenith_Altar", DisplayName = "Dark Zenith Altar (Lv3)",
                TopFolder = "Dark_Zenith_Altar_Base_Structu", EnvFolder = "Dark_Zenith_Altar_Base_Structure_Envir",
                Hotkey = "T", BuildGold = 300, BuildMana = 150,
                Units = new[]
                {
                    new UnitSpec("Orc_3", 60, 0),
                    new UnitSpec("Fire_Slime", 75, 30),
                    new UnitSpec("Vampires_3", 45, 90),
                },
            },
        };
    }

    private static bool BuildBarracks(BarracksConfig config)
    {
        string envPath = $"{PlacementSpriteRoot}/{config.TopFolder}/{config.EnvFolder}";

        string rotationPath = $"{envPath}/rotations/unknown.png";
        Sprite staticSprite = ImportAndSlice(rotationPath);

        List<Sprite> animationFrames = ImportAnimationFrames(envPath);
        if (animationFrames.Count == 0)
        {
            Debug.LogWarning($"[BarracksSetup] {config.Name}: không tìm thấy frame animation trong {envPath}/animations.");
            return false;
        }

        AnimationClip clip = CreateLoopingClip(config.Name, animationFrames);
        AnimatorController controller = CreateController(config.Name, clip);

        Sprite defaultSprite = animationFrames[0];
        GameObject prefab = CreatePrefab(config, controller, defaultSprite);
        if (prefab == null)
        {
            return false;
        }

        Sprite iconSprite = staticSprite != null ? staticSprite : defaultSprite;
        CreatePlacedObjectType(config, prefab, iconSprite);
        return true;
    }

    // --- Import & slice ---------------------------------------------------------------

    private static List<Sprite> ImportAnimationFrames(string envPath)
    {
        string animationsRoot = $"{envPath}/animations";
        var frames = new List<Sprite>();
        if (!Directory.Exists(animationsRoot))
        {
            return frames;
        }

        string descFolder = Directory.GetDirectories(animationsRoot).FirstOrDefault();
        if (descFolder == null)
        {
            return frames;
        }

        string framesFolder = Path.Combine(descFolder, "unknown").Replace('\\', '/');
        if (!Directory.Exists(framesFolder))
        {
            return frames;
        }

        IEnumerable<string> framePaths = Directory
            .GetFiles(framesFolder, "*.png")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => p);

        foreach (string framePath in framePaths)
        {
            Sprite sprite = ImportAndSlice(framePath);
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }
        return frames;
    }

    // Áp cấu hình pixel-art + slice toàn ảnh thành 1 sprite (ô 64 nhưng ảnh 48x48 nên gói trọn ảnh).
    private static Sprite ImportAndSlice(string pngPath)
    {
        var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        if (texture == null)
        {
            return null;
        }

        ApplyWholeImageSprite(importer, pngPath, texture);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().FirstOrDefault();
    }

    private static void ApplyWholeImageSprite(TextureImporter importer, string pngPath, Texture2D texture)
    {
        float width = Mathf.Min(CellSize, texture.width);
        float height = Mathf.Min(CellSize, texture.height);

        var spriteRect = new SpriteRect
        {
            name = Path.GetFileNameWithoutExtension(pngPath),
            spriteID = GUID.Generate(),
            rect = new Rect(0f, 0f, width, height),
            alignment = SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f),
            border = Vector4.zero,
        };

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(new[] { spriteRect });

        var nameIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
        {
            nameIdProvider.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID) });
        }

        provider.Apply();
    }

    // --- Animation --------------------------------------------------------------------

    private static AnimationClip CreateLoopingClip(string name, List<Sprite> frames)
    {
        var clip = new AnimationClip { name = name, frameRate = FrameRate };

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i / (float)FrameRate, value = frames[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, $"{AnimationFolder}/{name}.anim");
        return clip;
    }

    private static AnimatorController CreateController(string name, AnimationClip clip)
    {
        string controllerPath = $"{AnimationFolder}/{name}.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddMotion(clip); // tạo state mặc định chạy clip lặp
        return controller;
    }

    // --- Prefab -----------------------------------------------------------------------

    private static GameObject CreatePrefab(BarracksConfig config, AnimatorController controller, Sprite defaultSprite)
    {
        var go = new GameObject(config.Name);

        var spriteRenderer = go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.sortingOrder = SortingOrder;
        Material litMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(LitSpriteMaterialGuid));
        if (litMaterial != null)
        {
            spriteRenderer.sharedMaterial = litMaterial;
        }

        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var training = go.AddComponent<UnitTrainingBuilding>();
        ConfigureTraining(training, config);

        string prefabPath = $"{PrefabFolder}/{config.Name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void ConfigureTraining(UnitTrainingBuilding training, BarracksConfig config)
    {
        var serialized = new SerializedObject(training);

        SerializedProperty units = serialized.FindProperty("trainableUnits");
        units.arraySize = config.Units.Length;
        for (int i = 0; i < config.Units.Length; i++)
        {
            UnitSpec spec = config.Units[i];
            SerializedProperty element = units.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("prefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>($"{MonsterFolder}/{spec.MonsterPrefab}.prefab");
            element.FindPropertyRelative("goldCost").intValue = spec.Gold;
            element.FindPropertyRelative("manaCost").intValue = spec.Mana;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // --- Building type SO -------------------------------------------------------------

    private static void CreatePlacedObjectType(BarracksConfig config, GameObject prefab, Sprite icon)
    {
        string assetPath = $"{DataFolder}/PlacedObjectType_{config.Name}.asset";
        var type = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(assetPath);
        bool isNew = type == null;
        if (isNew)
        {
            type = ScriptableObject.CreateInstance<PlacedObjectTypeSO>();
        }

        type.nameString = config.DisplayName;
        type.prefab = prefab.transform;
        type.visual = prefab.transform;
        type.icon = icon;
        type.goldCost = config.BuildGold;
        type.manaCost = config.BuildMana;
        type.hotkey = config.Hotkey;
        type.description = "Trại huấn luyện lính: sinh Orc/Slime/Vampire theo chu kỳ.";
        type.width = FootprintCells;
        type.height = FootprintCells;

        if (isNew)
        {
            AssetDatabase.CreateAsset(type, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(type);
        }
    }
}
