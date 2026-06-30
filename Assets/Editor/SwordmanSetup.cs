using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;
using BlendTree = UnityEditor.Animations.BlendTree;
using BlendTreeType = UnityEditor.Animations.BlendTreeType;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

/// <summary>
/// Công cụ dựng 3 prefab Swordman (lvl1/lvl2/lvl3) theo khuôn mẫu prefab Human
/// (vd Pastor_Priestess). Mỗi lần chạy menu sẽ:
///   1. Re-import + slice lưới 64x64 cho các sheet trong thư mục Without_shadow
///      (Point filter, không nén, PixelsPerUnit = 16).
///   2. Tạo AnimationClip theo 4 hướng cho idle/run/attack/hurt/death.
///   3. Tạo AnimatorController với blend tree 2D + transition giống Human.
///   4. Nhân bản prefab Human gốc, gắn controller + sprite mặc định, lưu prefab mới.
///
/// Mỗi sheet cao 256px = 4 hàng = 4 hướng. Thứ tự hàng (trên -> dưới) khai báo ở
/// <see cref="RowDirectionsTopToBottom"/>; nếu trái/phải bị ngược ngoài game thì chỉ cần
/// đổi chỗ "west"/"east" trong mảng đó.
/// </summary>
public static class SwordmanSetup
{
    private const int PixelsPerUnit = 16;
    private const int CellSize = 64;
    private const int FrameRate = 10;

    private const string HumanPrefabFolder = "Assets/Prefabs/Human";
    private const string BaseHumanPrefabPath = "Assets/Prefabs/Human/Pastor_Priestess.prefab";
    private const string AnimationRootFolder = "Assets/Animation";
    private const string VisualChildName = "Visual";

    // Thứ tự hàng từ trên xuống của sprite sheet (đã xác nhận: trên = quay xuống/south,
    // dưới = quay lên/north; 2 hàng giữa là nhìn nghiêng). Đổi chỗ west/east nếu ngược.
    private static readonly string[] RowDirectionsTopToBottom = { "south", "west", "east", "north" };

    private static readonly Dictionary<string, Vector2> DirectionBlendPositions = new Dictionary<string, Vector2>
    {
        { "north", new Vector2(0f, 1f) },
        { "south", new Vector2(0f, -1f) },
        { "east", new Vector2(1f, 0f) },
        { "west", new Vector2(-1f, 0f) },
    };

    private struct StateConfig
    {
        public string StateName;
        public bool Loop;
        public StateConfig(string stateName, bool loop) { StateName = stateName; Loop = loop; }
    }

    private struct LevelConfig
    {
        public string LevelName;            // vd "Swordsman_lvl1"
        public string WithoutShadowFolder;  // thư mục chứa các sheet
        public Dictionary<string, string> StateSheetFiles; // stateName -> file sheet
    }

    [MenuItem("Tools/Dungeon/Build Swordman Prefabs")]
    public static void BuildAll()
    {
        LevelConfig[] levels = BuildLevelConfigs();
        int built = 0;
        try
        {
            for (int i = 0; i < levels.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Build Swordman Prefabs", levels[i].LevelName, (float)i / levels.Length);
                if (BuildLevel(levels[i]))
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
        Debug.Log($"[SwordmanSetup] Hoàn tất: {built}/{levels.Length} prefab Swordman được dựng trong {HumanPrefabFolder}.");
    }

    private static LevelConfig[] BuildLevelConfigs()
    {
        const string root = "Assets/Sprites/character/Swordman";

        var lvl1 = new LevelConfig
        {
            LevelName = "Swordsman_lvl1",
            WithoutShadowFolder = $"{root}/Swordsman_lvl1/Without_shadow",
            StateSheetFiles = new Dictionary<string, string>
            {
                { "idle", "Swordsman_lvl1_Idle_without_shadow.png" },
                { "run", "run.png" },
                { "attack", "Swordsman_lvl1_attack_without_shadow.png" },
                { "hurt", "hurt.png" },
                { "death", "death.png" },
            },
        };

        var lvl2 = MakeStandardLevel(root, "Swordsman_lvl2");
        var lvl3 = MakeStandardLevel(root, "Swordsman_lvl3");
        return new[] { lvl1, lvl2, lvl3 };
    }

    private static LevelConfig MakeStandardLevel(string root, string levelName)
    {
        return new LevelConfig
        {
            LevelName = levelName,
            WithoutShadowFolder = $"{root}/{levelName}/Without_shadow",
            StateSheetFiles = new Dictionary<string, string>
            {
                { "idle", $"{levelName}_Idle_without_shadow.png" },
                { "run", $"{levelName}_Run_without_shadow.png" },
                { "attack", $"{levelName}_attack_without_shadow.png" },
                { "hurt", $"{levelName}_Hurt_without_shadow.png" },
                { "death", $"{levelName}_Death_without_shadow.png" },
            },
        };
    }

    private static readonly StateConfig[] States =
    {
        new StateConfig("idle", true),
        new StateConfig("run", true),
        new StateConfig("attack", false),
        new StateConfig("hurt", false),
        new StateConfig("death", false),
    };

    private static bool BuildLevel(LevelConfig level)
    {
        // stateName -> (direction -> danh sách sprite theo thứ tự frame)
        var framesByState = new Dictionary<string, Dictionary<string, List<Sprite>>>();
        foreach (StateConfig state in States)
        {
            string sheetPath = $"{level.WithoutShadowFolder}/{level.StateSheetFiles[state.StateName]}";
            Dictionary<string, List<Sprite>> directionalFrames = SliceSheetIntoDirections(sheetPath);
            if (directionalFrames == null)
            {
                Debug.LogWarning($"[SwordmanSetup] Bỏ qua {level.LevelName}: không slice được {sheetPath}");
                return false;
            }
            framesByState[state.StateName] = directionalFrames;
        }

        string animFolder = $"{AnimationRootFolder}/{level.LevelName}";
        var clipsByState = CreateClips(level.LevelName, animFolder, framesByState);
        AnimatorController controller = CreateController(animFolder, level.LevelName, clipsByState);

        Sprite defaultSprite = ResolveDefaultSprite(framesByState);
        return CreatePrefab(level.LevelName, controller, defaultSprite);
    }

    // --- Bước 1: slice ----------------------------------------------------------------

    private static Dictionary<string, List<Sprite>> SliceSheetIntoDirections(string sheetPath)
    {
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        bool wasReadable = importer.isReadable;
        importer.isReadable = true;
        importer.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
        if (texture == null)
        {
            return null;
        }

        // keepEmptyRects = false: tự bỏ các ô trống nên mỗi hàng chỉ còn frame thật.
        Rect[] rects = InternalSpriteUtility.GenerateGridSpriteRectangles(
            texture, Vector2.zero, new Vector2(CellSize, CellSize), Vector2.zero, false);
        if (rects == null || rects.Length == 0)
        {
            return null;
        }

        ApplyGridSprites(importer, sheetPath, rects);
        importer.isReadable = wasReadable;
        importer.SaveAndReimport();

        return GroupSpritesByDirection(sheetPath);
    }

    private static void ApplyGridSprites(TextureImporter importer, string sheetPath, Rect[] rects)
    {
        string baseName = Path.GetFileNameWithoutExtension(sheetPath);
        var spriteRects = new SpriteRect[rects.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            spriteRects[i] = new SpriteRect
            {
                name = $"{baseName}_{i}",
                spriteID = GUID.Generate(),
                rect = rects[i],
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
            };
        }

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

    private static Dictionary<string, List<Sprite>> GroupSpritesByDirection(string sheetPath)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
        if (sprites.Length == 0)
        {
            return null;
        }

        // Gom theo hàng dựa trên toạ độ y (y tính từ đáy ảnh -> y lớn = hàng trên).
        var rowKeys = sprites
            .Select(s => Mathf.RoundToInt(s.rect.y))
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        var framesByDirection = new Dictionary<string, List<Sprite>>();
        for (int row = 0; row < rowKeys.Count && row < RowDirectionsTopToBottom.Length; row++)
        {
            string direction = RowDirectionsTopToBottom[row];
            int rowY = rowKeys[row];
            List<Sprite> frames = sprites
                .Where(s => Mathf.RoundToInt(s.rect.y) == rowY)
                .OrderBy(s => s.rect.x)
                .ToList();
            framesByDirection[direction] = frames;
        }

        return framesByDirection;
    }

    // --- Bước 2: clip -----------------------------------------------------------------

    private static Dictionary<string, Dictionary<string, AnimationClip>> CreateClips(
        string levelName, string animFolder,
        Dictionary<string, Dictionary<string, List<Sprite>>> framesByState)
    {
        EnsureFolder(animFolder);
        var clipsByState = new Dictionary<string, Dictionary<string, AnimationClip>>();

        foreach (StateConfig state in States)
        {
            string stateFolder = $"{animFolder}/{state.StateName}";
            EnsureFolder(stateFolder);

            var clipsByDirection = new Dictionary<string, AnimationClip>();
            foreach (KeyValuePair<string, List<Sprite>> entry in framesByState[state.StateName])
            {
                string direction = entry.Key;
                List<Sprite> frames = entry.Value;
                if (frames.Count == 0)
                {
                    continue;
                }

                string clipName = $"{state.StateName}_{direction}";
                string clipPath = $"{stateFolder}/{clipName}.anim";
                AnimationClip clip = BuildSpriteClip(clipName, frames, state.Loop);
                AssetDatabase.CreateAsset(clip, clipPath);
                clipsByDirection[direction] = clip;
            }

            clipsByState[state.StateName] = clipsByDirection;
        }

        return clipsByState;
    }

    private static AnimationClip BuildSpriteClip(string clipName, List<Sprite> frames, bool loop)
    {
        var clip = new AnimationClip { name = clipName, frameRate = FrameRate };

        // Path rỗng: SpriteRenderer nằm cùng GameObject "Visual" với Animator (giống Human).
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)FrameRate,
                value = frames[i],
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    // --- Bước 3: controller -----------------------------------------------------------

    private static AnimatorController CreateController(
        string animFolder, string levelName,
        Dictionary<string, Dictionary<string, AnimationClip>> clipsByState)
    {
        string controllerPath = $"{animFolder}/{levelName}.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Horizontal", AnimatorControllerParameterType.Float);
        controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Bool);
        controller.AddParameter("takeDamage", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idle = CreateDirectionalState(controller, "idle", clipsByState["idle"]);
        AnimatorState run = CreateDirectionalState(controller, "run", clipsByState["run"]);
        AnimatorState attack = CreateDirectionalState(controller, "attack", clipsByState["attack"]);
        AnimatorState hurt = CreateDirectionalState(controller, "hurt", clipsByState["hurt"]);
        AnimatorState death = CreateDirectionalState(controller, "death", clipsByState["death"]);

        stateMachine.defaultState = idle;

        AddFloatTransition(idle, run, "Speed", AnimatorConditionMode.Greater, 0.01f);
        AddFloatTransition(run, idle, "Speed", AnimatorConditionMode.Less, 0.01f);
        AddBoolTransition(idle, attack, "Attack", true);
        AddBoolTransition(run, attack, "Attack", true);
        AddBoolTransition(attack, idle, "Attack", false);
        AddAnyStateTriggerTransition(stateMachine, hurt, "takeDamage");
        AddAnyStateTriggerTransition(stateMachine, death, "die");
        AddExitTimeTransition(hurt, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState CreateDirectionalState(
        AnimatorController controller, string stateName, Dictionary<string, AnimationClip> clipsByDirection)
    {
        AnimatorState state = controller.CreateBlendTreeInController(stateName, out BlendTree tree, 0);
        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = "Horizontal";
        tree.blendParameterY = "Vertical";
        tree.useAutomaticThresholds = true;

        foreach (string direction in new[] { "north", "south", "east", "west" })
        {
            if (clipsByDirection.TryGetValue(direction, out AnimationClip clip))
            {
                tree.AddChild(clip, DirectionBlendPositions[direction]);
            }
        }

        return state;
    }

    private static void AddFloatTransition(
        AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        ConfigureInstantTransition(transition);
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        ConfigureInstantTransition(transition);
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState to, string parameter)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        ConfigureInstantTransition(transition);
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTimeTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.9f;
        transition.hasFixedDuration = true;
        transition.duration = 0.05f;
    }

    private static void ConfigureInstantTransition(AnimatorStateTransition transition)
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.05f;
    }

    // --- Bước 4: prefab ---------------------------------------------------------------

    private static bool CreatePrefab(string levelName, AnimatorController controller, Sprite defaultSprite)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseHumanPrefabPath);
        if (basePrefab == null)
        {
            Debug.LogWarning($"[SwordmanSetup] Không tìm thấy prefab Human gốc: {BaseHumanPrefabPath}");
            return false;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = levelName;

        Transform visual = instance.transform.Find(VisualChildName);
        if (visual == null)
        {
            Debug.LogWarning($"[SwordmanSetup] Không tìm thấy child '{VisualChildName}' trong prefab gốc.");
            Object.DestroyImmediate(instance);
            return false;
        }

        var animator = visual.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        var spriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && defaultSprite != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }

        string prefabPath = $"{HumanPrefabFolder}/{levelName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        return true;
    }

    private static Sprite ResolveDefaultSprite(Dictionary<string, Dictionary<string, List<Sprite>>> framesByState)
    {
        Dictionary<string, List<Sprite>> idleFrames = framesByState["idle"];
        if (idleFrames.TryGetValue("south", out List<Sprite> southFrames) && southFrames.Count > 0)
        {
            return southFrames[0];
        }

        foreach (List<Sprite> frames in idleFrames.Values)
        {
            if (frames.Count > 0)
            {
                return frames[0];
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string leaf = Path.GetFileName(folderPath);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
