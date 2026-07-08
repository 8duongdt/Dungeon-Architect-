using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Dựng toàn bộ hệ thống kỹ năng phép cho unit tầm xa từ gói
/// "Assets/Sprites/10-magic-sprite-sheet-effects-pixel-art":
///
///   1. Kiểm tra các sheet đã được slice (Tools > Sprites > Config 10-Magic Sprite Sheets).
///   2. Tạo AnimationClip (không loop) + AnimatorController + prefab VFX cho 10 phép.
///   3. Tạo 10 asset SkillDefinitionSO trong Assets/Skills.
///   4. Gắn UnitSkillCaster + gán skill hợp chủ đề cho 5 prefab tầm xa.
///
///   Menu: Tools > Dungeon > Setup Ranged Skills
///
/// Chạy lại an toàn (idempotent): asset ghi đè cùng đường dẫn nên GUID giữ nguyên.
/// Lưu ý: KHÔNG tạo clip/controller trong AssetDatabase.StartAssetEditing (lỗi đã biết).
/// </summary>
public static class RangedSkillSetup
{
    private const string SheetRootFolder = "Assets/Sprites/10-magic-sprite-sheet-effects-pixel-art";
    private const string IconFolder = "Assets/Sprites/10-magic-sprite-sheet-effects-pixel-art/Icons";
    private const string AnimationFolder = "Assets/Animation/SkillVfx";
    private const string PrefabFolder = "Assets/Prefabs/SkillVfx";
    private const string SkillFolder = "Assets/Skills";

    private const int FrameRate = 12;
    private const int VfxSortingOrder = 20;
    private const float DefaultMagicPower = 10f;

    // Projectile FireBall: đạn vẽ giữa unit (9-10) và VFX nổ (20).
    private const int ProjectileSortingOrder = 15;
    private const float FireBallProjectileSpeed = 6f;
    private const float FireBallProjectileMaxRange = 12f;
    private const float FireBallColliderRadius = 0.3f;
    // Pivot BottomCenter: hạ sprite con xuống nửa chiều cao (72px/PPU16/2 x scale 0.8) để tâm hình trùng root.
    private const float ProjectileSpriteCenterOffset = -1.8f;

    // Chain Lightning: nảy sang tối đa 2 địch trong bán kính 3, mỗi nảy 50% sát thương.
    private const int LightningMaxChainTargets = 2;
    private const float LightningChainRadius = 3f;
    private const float LightningChainDamageFactor = 0.5f;

    // Vùng/bẫy/xoáy vẽ sát đất, dưới unit (unit vẽ ở order 9-10).
    private const int ZoneSortingOrder = 5;

    // Explosion: nổ quanh mục tiêu + đẩy lùi (nút "cứu nguy" phá vây theo thiết kế).
    private const float ExplosionAreaRadius = 2.5f;
    private const float ExplosionKnockbackDistance = 1.5f;

    // SunStrike: nổ hủy diệt trễ 1.5s tại đúng điểm đã chọn, không đẩy lùi.
    private const float SunStrikeAreaRadius = 2f;

    // LightningBolt: giật liên tục 3s trong vùng nhỏ, mỗi nhịp kèm choáng ngắn.
    private const float LightningBoltZoneDuration = 3f;
    private const float LightningBoltZoneRadius = 1.8f;
    private const float LightningBoltTickInterval = 0.5f;
    private const float LightningBoltTickDamageFactor = 0.3f;
    private const float LightningBoltStunDuration = 0.6f;

    // FireWall: tường lửa thiêu đốt kéo dài, không choáng.
    private const float FireWallZoneDuration = 6f;
    private const float FireWallZoneRadius = 1.5f;
    private const float FireWallTickInterval = 0.5f;
    private const float FireWallTickDamageFactor = 0.25f;

    // Spikes: bẫy chờ 0.5s rồi mờ đi, kẻ dẫm bẫy bị trói chân.
    private const float SpikesTriggerRadius = 1f;
    private const float SpikesArmDelay = 0.5f;
    private const float SpikesRootDuration = 1.2f;

    // BlackHole: ultimate hút mọi kẻ địch trong bán kính rộng về tâm và giữ chân.
    private const float BlackHolePullRadius = 4f;
    private const float BlackHolePullDuration = 2.5f;
    private const float BlackHolePullSpeed = 4f;

    // Shield: khiên máu ảo bao bọc người thi triển.
    private const float ShieldAbsorbAmount = 50f;
    private const float ShieldDuration = 6f;

    // MidasTouch: đòn kết liễu mục tiêu dưới 10% máu, thưởng vàng.
    private const float MidasExecuteThreshold = 0.1f;
    private const int MidasExecuteGoldReward = 25;

    private struct SkillConfig
    {
        public string SheetPath;
        public string Name;
        public float BaseDamage;
        public float StatScaling;
        public float Cooldown;
        public float PrefabScale;
        public SkillMechanic Mechanic;
        public int ManaCost;
        public float ImpactDelay;

        public SkillConfig(string sheetPath, string name, float baseDamage, float statScaling, float cooldown, float prefabScale,
            SkillMechanic mechanic = SkillMechanic.InstantStrike, int manaCost = 15, float impactDelay = 0f)
        {
            SheetPath = sheetPath;
            Name = name;
            BaseDamage = baseDamage;
            StatScaling = statScaling;
            Cooldown = cooldown;
            PrefabScale = prefabScale;
            Mechanic = mechanic;
            ManaCost = manaCost;
            ImpactDelay = impactDelay;
        }
    }

    // PrefabScale 0.8: PPU 16 x khung 72px = 4.5 world unit, nhân vật ~3.75 -> thu về cỡ unit.
    // Cân bằng theo thiết kế: FireBall/Lightning = nhóm spam (CD ngắn, mana rẻ);
    // Explosion/SunStrike = nhóm nuke (damage lớn, CD dài, mana đắt; SunStrike trễ 1.5s
    // trước khi giáng xuống); BlackHole = ultimate (mana đắt nhất).
    private static readonly SkillConfig[] Skills =
    {
        new SkillConfig($"{SheetRootFolder}/1 Lightning/Lightning.png",            "Lightning",     10f, 1f, 2.5f, 0.8f, SkillMechanic.ChainStrike, 8),
        new SkillConfig($"{SheetRootFolder}/2 Lightning bolt/Lightning-bolt.png",  "LightningBolt", 12f, 1f, 7f, 0.8f, SkillMechanic.DamageZone, 20),
        new SkillConfig($"{SheetRootFolder}/3 Midas touch/Midas-touch.png",        "MidasTouch",    5f, 1f, 4f, 0.8f, SkillMechanic.ExecuteStrike, 10),
        new SkillConfig($"{SheetRootFolder}/4 Sun strike/Sun-strike.png",          "SunStrike",     30f, 1f, 8f, 0.8f, SkillMechanic.AreaStrike, 30, 1.5f),
        new SkillConfig($"{SheetRootFolder}/5 Explosion/Explosion.png",            "Explosion",     25f, 1f, 6f, 0.8f, SkillMechanic.AreaStrike, 20),
        new SkillConfig($"{SheetRootFolder}/6 Spikes/Spikes.png",                  "Spikes",        15f, 1f, 5f, 0.8f, SkillMechanic.Trap, 12),
        new SkillConfig($"{SheetRootFolder}/7 Fire wall/Fire-wall.png",            "FireWall",      12f, 1f, 8f, 0.8f, SkillMechanic.DamageZone, 20),
        new SkillConfig($"{SheetRootFolder}/8 Shield/Shield.png",                  "Shield",        0f, 0f, 10f, 0.8f, SkillMechanic.SelfShield, 25),
        new SkillConfig($"{SheetRootFolder}/9 Black hole/Black-hole.png",          "BlackHole",     15f, 1f, 15f, 0.8f, SkillMechanic.PullVortex, 50),
        new SkillConfig($"{SheetRootFolder}/10 Fire ball/Fire-ball.png",           "FireBall",      12f, 1f, 2f, 0.8f, SkillMechanic.Projectile, 5),
    };

    // Tên file icon trong Icons/ khớp 1:1 với tên skill (sheet đặt tên "N-Ten-co-gach").
    private static readonly Dictionary<string, string> IconFileBySkillName = new Dictionary<string, string>
    {
        ["Lightning"] = "1-Lightning",
        ["LightningBolt"] = "2-Lightning-bolt",
        ["MidasTouch"] = "3-Midas-touch",
        ["SunStrike"] = "4-Sun-strike",
        ["Explosion"] = "5-Explosion",
        ["Spikes"] = "6-Spikes",
        ["FireWall"] = "7-Fire-wall",
        ["Shield"] = "8-Shield",
        ["BlackHole"] = "9-Black-hole",
        ["FireBall"] = "10-Fire-ball",
    };

    // Gán skill theo tier: con yếu cầm skill spam cơ bản, con mạnh nhất cầm ultimate.
    private static readonly (string prefabPath, string skillName)[] UnitSkillAssignments =
    {
        ("Assets/Prefabs/Human/Pastor_Priestess.prefab",  "SunStrike"),
        ("Assets/Prefabs/Human/Pastor_Apprentice.prefab", "Lightning"),
        ("Assets/Prefabs/Monster/Vampires_1.prefab",      "FireBall"),
        ("Assets/Prefabs/Monster/Vampires_2.prefab",      "Spikes"),
        ("Assets/Prefabs/Monster/Vampires_3.prefab",      "BlackHole"),
    };

    [MenuItem("Tools/Dungeon/Setup Ranged Skills")]
    public static void SetupAll()
    {
        Dictionary<string, List<Sprite>> framesBySkill = LoadAllSheetFrames();
        if (framesBySkill == null)
        {
            return;
        }

        EnsureFolder(AnimationFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(SkillFolder);

        var skillAssets = new Dictionary<string, SkillDefinitionSO>();
        try
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                SkillConfig config = Skills[i];
                EditorUtility.DisplayProgressBar("Setup Ranged Skills", config.Name, (float)i / Skills.Length);

                List<Sprite> frames = framesBySkill[config.Name];
                GameObject vfxPrefab = BuildVfxPrefab(config, frames);
                GameObject mechanicPrefab = BuildMechanicPrefab(config, frames);
                skillAssets[config.Name] = BuildSkillAsset(config, vfxPrefab, mechanicPrefab);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        int wiredUnits = WireUnitPrefabs(skillAssets);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RangedSkillSetup] Hoàn tất: {skillAssets.Count} skill (clip + controller + VFX prefab + SO), "
            + $"{wiredUnits}/{UnitSkillAssignments.Length} unit prefab đã gắn UnitSkillCaster.");
    }

    // ---------- Bước 1: nạp frame từ sheet đã slice ----------

    private static Dictionary<string, List<Sprite>> LoadAllSheetFrames()
    {
        var framesBySkill = new Dictionary<string, List<Sprite>>();
        foreach (SkillConfig config in Skills)
        {
            List<Sprite> frames = LoadOrderedFrames(config.SheetPath);
            bool isSliced = frames.Count >= 2;
            if (!isSliced)
            {
                Debug.LogError($"[RangedSkillSetup] Sheet chưa được slice: {config.SheetPath}. "
                    + "Chạy Tools > Sprites > Config 10-Magic Sprite Sheets trước, rồi chạy lại.");
                return null;
            }

            framesBySkill[config.Name] = frames;
        }

        return framesBySkill;
    }

    private static List<Sprite> LoadOrderedFrames(string sheetPath)
    {
        HashSet<string> visibleFrameNames = FindVisibleFrameNames(sheetPath);

        // Sprite được đặt tên "<Sheet>_<i>" bởi MagicSpriteSheetImporter - sắp theo chỉ số,
        // bỏ frame trống hoàn toàn (ô thừa ở các sheet ngắn hơn 720px).
        return AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .Where(sprite => visibleFrameNames == null || visibleFrameNames.Contains(sprite.name))
            .Select(sprite => (sprite, index: ParseTrailingIndex(sprite.name)))
            .Where(entry => entry.index >= 0)
            .OrderBy(entry => entry.index)
            .Select(entry => entry.sprite)
            .ToList();
    }

    /// <summary>
    /// Trả về tên các sprite có ít nhất một pixel không trong suốt.
    /// Bật isReadable tạm thời để đọc pixel rồi trả lại như cũ TRƯỚC khi dựng clip,
    /// nên tham chiếu sprite trong clip không bị hỏng. Trả về null nếu không đọc được.
    /// </summary>
    private static HashSet<string> FindVisibleFrameNames(string sheetPath)
    {
        var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        try
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            if (texture == null)
            {
                return null;
            }

            var visibleNames = new HashSet<string>();
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>())
            {
                if (HasVisiblePixels(texture, sprite.rect))
                {
                    visibleNames.Add(sprite.name);
                }
            }

            return visibleNames;
        }
        finally
        {
            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static bool HasVisiblePixels(Texture2D texture, Rect rect)
    {
        Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        return pixels.Any(pixel => pixel.a > 0f);
    }

    private static int ParseTrailingIndex(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');
        bool hasIndexSuffix = underscore >= 0
            && int.TryParse(spriteName.Substring(underscore + 1), out int index);
        return hasIndexSuffix ? int.Parse(spriteName.Substring(underscore + 1)) : -1;
    }

    // ---------- Bước 2: clip + controller + prefab VFX ----------

    private static GameObject BuildVfxPrefab(SkillConfig config, List<Sprite> frames)
    {
        AnimationClip clip = BuildSpriteClip($"SkillVfx_{config.Name}", frames, false);
        AnimatorController controller = BuildController(config.Name, clip);
        return SaveVfxPrefab(config, frames[0], controller);
    }

    private static AnimationClip BuildSpriteClip(string clipName, List<Sprite> frames, bool loop)
    {
        string clipPath = $"{AnimationFolder}/{clipName}.anim";

        // Ghi đè clip cũ nếu có để giữ GUID (controller/prefab đang tham chiếu).
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNewClip = clip == null;
        if (isNewClip)
        {
            clip = new AnimationClip();
        }

        clip.name = clipName;
        clip.frameRate = FrameRate;

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keyframes = new ObjectReferenceKeyframe[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i / (float)FrameRate, value = frames[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNewClip)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }

        return clip;
    }

    private static AnimatorController BuildController(string skillName, AnimationClip clip)
    {
        string controllerPath = $"{AnimationFolder}/SkillVfx_{skillName}.controller";

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddMotion(clip);
            return controller;
        }

        // Controller cũ: đảm bảo state mặc định trỏ đúng clip (clip giữ GUID nên thường đã đúng).
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine.defaultState != null)
        {
            stateMachine.defaultState.motion = clip;
            EditorUtility.SetDirty(controller);
        }
        else
        {
            controller.AddMotion(clip);
        }

        return controller;
    }

    private static GameObject SaveVfxPrefab(SkillConfig config, Sprite firstFrame, AnimatorController controller)
    {
        string prefabPath = $"{PrefabFolder}/SkillVfx_{config.Name}.prefab";

        var go = new GameObject($"SkillVfx_{config.Name}");
        try
        {
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = firstFrame;
            spriteRenderer.sortingOrder = VfxSortingOrder; // trên unit (unit vẽ ở order 9-10)

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            go.AddComponent<SkillVfxOneShot>();
            go.transform.localScale = Vector3.one * config.PrefabScale;

            return PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // ---------- Bước 2b: prefab cơ chế (đạn/vùng/bẫy/xoáy) theo mechanic ----------

    private static GameObject BuildMechanicPrefab(SkillConfig config, List<Sprite> frames)
    {
        switch (config.Mechanic)
        {
            case SkillMechanic.Projectile:
                return BuildProjectilePrefab(config, frames);
            case SkillMechanic.DamageZone:
                return BuildZonePrefab<SkillDamageZone>(config, frames, "SkillZone");
            case SkillMechanic.Trap:
                return BuildZonePrefab<SkillTrap>(config, frames, "SkillTrap");
            case SkillMechanic.PullVortex:
                return BuildZonePrefab<SkillBlackHole>(config, frames, "SkillVortex");
            default:
                return null;
        }
    }

    private static GameObject BuildProjectilePrefab(SkillConfig config, List<Sprite> frames)
    {
        AnimationClip loopingClip = BuildSpriteClip($"SkillVfx_{config.Name}Projectile", frames, true);
        AnimatorController controller = BuildController($"{config.Name}Projectile", loopingClip);
        return SaveProjectilePrefab(config, frames[0], controller);
    }

    /// <summary>
    /// Prefab vùng hiệu ứng đặt trên đất: root mang component cơ chế, sprite con
    /// chạy animation lặp, vẽ dưới unit. Ghi đè cùng đường dẫn để giữ GUID.
    /// </summary>
    private static GameObject BuildZonePrefab<TComponent>(SkillConfig config, List<Sprite> frames, string prefabPrefix)
        where TComponent : MonoBehaviour
    {
        AnimationClip loopingClip = BuildSpriteClip($"SkillVfx_{config.Name}Loop", frames, true);
        AnimatorController controller = BuildController($"{config.Name}Loop", loopingClip);
        string prefabPath = $"{PrefabFolder}/{prefabPrefix}_{config.Name}.prefab";

        var root = new GameObject($"{prefabPrefix}_{config.Name}");
        try
        {
            root.AddComponent<TComponent>();

            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform, false);
            spriteChild.transform.localScale = Vector3.one * config.PrefabScale;

            var spriteRenderer = spriteChild.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames[0];
            spriteRenderer.sortingOrder = ZoneSortingOrder;

            var animator = spriteChild.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject SaveProjectilePrefab(SkillConfig config, Sprite firstFrame, AnimatorController controller)
    {
        string prefabPath = $"{PrefabFolder}/SkillProjectile_{config.Name}.prefab";

        var root = new GameObject($"SkillProjectile_{config.Name}");
        try
        {
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = FireBallColliderRadius;

            root.AddComponent<SkillProjectile>();

            // Sprite ở con: pivot BottomCenter nên hạ xuống nửa chiều cao để tâm hình
            // trùng root - root xoay quanh tâm thật của viên đạn, collider giữ đơn vị world.
            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform, false);
            spriteChild.transform.localPosition = new Vector3(0f, ProjectileSpriteCenterOffset, 0f);
            spriteChild.transform.localScale = Vector3.one * config.PrefabScale;

            var spriteRenderer = spriteChild.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = firstFrame;
            spriteRenderer.sortingOrder = ProjectileSortingOrder;

            var animator = spriteChild.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------- Bước 3: asset SkillDefinitionSO ----------

    private static SkillDefinitionSO BuildSkillAsset(SkillConfig config, GameObject vfxPrefab, GameObject mechanicPrefab)
    {
        string assetPath = $"{SkillFolder}/Skill_{config.Name}.asset";

        var skillAsset = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(assetPath);
        if (skillAsset == null)
        {
            skillAsset = ScriptableObject.CreateInstance<SkillDefinitionSO>();
            AssetDatabase.CreateAsset(skillAsset, assetPath);
        }

        var serialized = new SerializedObject(skillAsset);
        serialized.FindProperty("skillName").stringValue = config.Name;
        serialized.FindProperty("icon").objectReferenceValue = LoadIconSprite(config.Name);
        serialized.FindProperty("vfxPrefab").objectReferenceValue = vfxPrefab;
        serialized.FindProperty("baseDamage").floatValue = config.BaseDamage;
        serialized.FindProperty("statScaling").floatValue = config.StatScaling;
        serialized.FindProperty("cooldown").floatValue = config.Cooldown;
        serialized.FindProperty("manaCost").intValue = config.ManaCost;
        serialized.FindProperty("impactDelay").floatValue = config.ImpactDelay;
        // Ghi mechanic cho CẢ 10 asset để chạy lại luôn đưa về đúng cấu hình (idempotent).
        serialized.FindProperty("mechanic").enumValueIndex = (int)config.Mechanic;
        WriteMechanicFields(serialized, config, mechanicPrefab);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        return skillAsset;
    }

    private static void WriteMechanicFields(SerializedObject serialized, SkillConfig config, GameObject mechanicPrefab)
    {
        switch (config.Mechanic)
        {
            case SkillMechanic.Projectile:
                serialized.FindProperty("projectilePrefab").objectReferenceValue = mechanicPrefab;
                serialized.FindProperty("projectileSpeed").floatValue = FireBallProjectileSpeed;
                serialized.FindProperty("projectileMaxRange").floatValue = FireBallProjectileMaxRange;
                break;
            case SkillMechanic.ChainStrike:
                serialized.FindProperty("maxChainTargets").intValue = LightningMaxChainTargets;
                serialized.FindProperty("chainRadius").floatValue = LightningChainRadius;
                serialized.FindProperty("chainDamageFactor").floatValue = LightningChainDamageFactor;
                break;
            case SkillMechanic.AreaStrike:
                WriteAreaStrikeFields(serialized, config);
                break;
            case SkillMechanic.DamageZone:
                WriteDamageZoneFields(serialized, config, mechanicPrefab);
                break;
            case SkillMechanic.Trap:
                serialized.FindProperty("trapPrefab").objectReferenceValue = mechanicPrefab;
                serialized.FindProperty("trapArmDelay").floatValue = SpikesArmDelay;
                serialized.FindProperty("trapRootDuration").floatValue = SpikesRootDuration;
                serialized.FindProperty("zoneRadius").floatValue = SpikesTriggerRadius;
                break;
            case SkillMechanic.PullVortex:
                serialized.FindProperty("vortexPrefab").objectReferenceValue = mechanicPrefab;
                serialized.FindProperty("pullRadius").floatValue = BlackHolePullRadius;
                serialized.FindProperty("pullDuration").floatValue = BlackHolePullDuration;
                serialized.FindProperty("pullSpeed").floatValue = BlackHolePullSpeed;
                break;
            case SkillMechanic.SelfShield:
                serialized.FindProperty("shieldAmount").floatValue = ShieldAbsorbAmount;
                serialized.FindProperty("shieldDuration").floatValue = ShieldDuration;
                break;
            case SkillMechanic.ExecuteStrike:
                serialized.FindProperty("executeHealthThreshold").floatValue = MidasExecuteThreshold;
                serialized.FindProperty("executeGoldReward").intValue = MidasExecuteGoldReward;
                break;
        }
    }

    private static void WriteAreaStrikeFields(SerializedObject serialized, SkillConfig config)
    {
        bool isSunStrike = config.Name == "SunStrike";
        serialized.FindProperty("areaRadius").floatValue = isSunStrike ? SunStrikeAreaRadius : ExplosionAreaRadius;
        serialized.FindProperty("knockbackDistance").floatValue = isSunStrike ? 0f : ExplosionKnockbackDistance;
    }

    private static void WriteDamageZoneFields(SerializedObject serialized, SkillConfig config, GameObject zonePrefab)
    {
        serialized.FindProperty("zonePrefab").objectReferenceValue = zonePrefab;
        bool isFireWall = config.Name == "FireWall";
        serialized.FindProperty("zoneDuration").floatValue = isFireWall ? FireWallZoneDuration : LightningBoltZoneDuration;
        serialized.FindProperty("zoneRadius").floatValue = isFireWall ? FireWallZoneRadius : LightningBoltZoneRadius;
        serialized.FindProperty("zoneTickInterval").floatValue = isFireWall ? FireWallTickInterval : LightningBoltTickInterval;
        serialized.FindProperty("zoneTickDamageFactor").floatValue = isFireWall ? FireWallTickDamageFactor : LightningBoltTickDamageFactor;
        serialized.FindProperty("zoneStunDuration").floatValue = isFireWall ? 0f : LightningBoltStunDuration;
    }

    // ---------- Bước 4: gắn UnitSkillCaster cho prefab tầm xa ----------

    private static int WireUnitPrefabs(Dictionary<string, SkillDefinitionSO> skillAssets)
    {
        int wired = 0;
        foreach ((string prefabPath, string skillName) in UnitSkillAssignments)
        {
            if (WireUnitPrefab(prefabPath, skillAssets[skillName]))
            {
                wired++;
            }
        }

        return wired;
    }

    private static bool WireUnitPrefab(string prefabPath, SkillDefinitionSO skillAsset)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (contents == null)
        {
            Debug.LogWarning($"[RangedSkillSetup] Không mở được prefab: {prefabPath}");
            return false;
        }

        try
        {
            var caster = contents.GetComponent<UnitSkillCaster>();
            if (caster == null)
            {
                caster = contents.AddComponent<UnitSkillCaster>();
            }

            var serialized = new SerializedObject(caster);
            serialized.FindProperty("skill").objectReferenceValue = skillAsset;
            serialized.FindProperty("magicPower").floatValue = DefaultMagicPower;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// Icon 32x32 của skill trong Icons/ - sheet import dạng Multiple nên phải lấy
    /// Sprite sub-asset qua LoadAllAssetsAtPath (LoadAssetAtPath&lt;Sprite&gt; có thể trả null).
    /// </summary>
    private static Sprite LoadIconSprite(string skillName)
    {
        if (!IconFileBySkillName.TryGetValue(skillName, out string iconFile))
        {
            Debug.LogWarning($"[RangedSkillSetup] Không có icon map cho skill: {skillName}");
            return null;
        }

        Sprite icon = AssetDatabase.LoadAllAssetsAtPath($"{IconFolder}/{iconFile}.png")
            .OfType<Sprite>()
            .FirstOrDefault();
        if (icon == null)
        {
            Debug.LogWarning($"[RangedSkillSetup] Không load được icon: {IconFolder}/{iconFile}.png");
        }

        return icon;
    }

    // ---------- Tiện ích ----------

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string currentPath = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = $"{currentPath}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }
            currentPath = nextPath;
        }
    }
}
