using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Gán tự động các AudioClip trong Assets/Audio/ vào UnitAudioPlayer (prefab unit),
/// SkillDefinitionSO (castSfx theo Mechanic), và các component âm thanh của scene đang mở
/// (BuildingAudioPlayer, PhaseAudioPlayer, CrystalAudioPlayer, LobbyController).
///
/// Mở qua menu: Tools > Dungeon > Audio > ...
/// Công cụ chỉ chạy trong Editor, KHÔNG đụng vào code/logic runtime.
/// </summary>
public static class AudioSetup
{
    private const string AudioRoot = "Assets/Audio/";

    private static readonly string[] UnitPrefabPaths =
    {
        "Assets/Prefabs/Human/Swordsman_lvl1.prefab",
        "Assets/Prefabs/Human/Swordsman_lvl2.prefab",
        "Assets/Prefabs/Human/Swordsman_lvl3.prefab",
        "Assets/Prefabs/Human/TheTank_Apprentice.prefab",
        "Assets/Prefabs/Human/TheTank_Master.prefab",
        "Assets/Prefabs/Human/Pastor_Apprentice.prefab",
        "Assets/Prefabs/Human/Pastor_Priestess.prefab",
        "Assets/Prefabs/Monster/Orc_1.prefab",
        "Assets/Prefabs/Monster/Orc_2.prefab",
        "Assets/Prefabs/Monster/Orc_3.prefab",
        "Assets/Prefabs/Monster/Slime.prefab",
        "Assets/Prefabs/Monster/Fire_Slime.prefab",
        "Assets/Prefabs/Monster/Ice_Slime.prefab",
        "Assets/Prefabs/Monster/Vampires_1.prefab",
        "Assets/Prefabs/Monster/Vampires_2.prefab",
        "Assets/Prefabs/Monster/Vampires_3.prefab",
    };

    [MenuItem("Tools/Dungeon/Audio/Wire Unit Prefabs")]
    internal static void WireUnitPrefabs()
    {
        AudioClip[] swingClips = LoadClips(
            "Weapons/sword_light.wav", "Weapons/sword_slice.wav", "Other/whoosh_1.wav", "Other/whoosh_2.wav");
        AudioClip[] hitClips = LoadClips(
            "Combat and Gore/punch.wav", "Combat and Gore/punch_2.wav", "Combat and Gore/punch_3.wav",
            "Combat and Gore/slap.wav", "Combat and Gore/crunch.wav");
        AudioClip[] hurtClips = LoadClips(
            "Human/man_0.wav", "Human/man_1.wav", "Human/man_2.wav", "Human/man_3.wav", "Human/man_4.wav");
        AudioClip[] deathClips = LoadClips(
            "Combat and Gore/crunch_splat_2.wav", "Combat and Gore/splat_quick.wav", "Combat and Gore/bone_snap.wav");
        AudioClip[] footstepClips = LoadClips(
            "Footsteps/digital/digital_footstep_gravel_1.wav", "Footsteps/digital/digital_footstep_gravel_2.wav",
            "Footsteps/digital/digital_footstep_gravel_3.wav", "Footsteps/digital/digital_footstep_gravel_4.wav");

        int wiredCount = 0;
        foreach (string prefabPath in UnitPrefabPaths)
        {
            if (WireUnitPrefab(prefabPath, swingClips, hitClips, hurtClips, deathClips, footstepClips))
            {
                wiredCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioSetup] Đã gắn UnitAudioPlayer cho {wiredCount}/{UnitPrefabPaths.Length} prefab.");
    }

    private static bool WireUnitPrefab(string prefabPath, AudioClip[] swingClips, AudioClip[] hitClips,
        AudioClip[] hurtClips, AudioClip[] deathClips, AudioClip[] footstepClips)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[AudioSetup] Không tìm thấy prefab: {prefabPath}");
            return false;
        }

        UnitAudioPlayer audioPlayer = GetOrAddComponent<UnitAudioPlayer>(root);
        var so = new SerializedObject(audioPlayer);
        AssignClipArray(so, "swingClips", swingClips);
        AssignClipArray(so, "hitClips", hitClips);
        AssignClipArray(so, "hurtClips", hurtClips);
        AssignClipArray(so, "deathClips", deathClips);
        AssignClipArray(so, "footstepClips", footstepClips);
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        return true;
    }

    [MenuItem("Tools/Dungeon/Audio/Wire Skill Cast Clips")]
    internal static void WireSkillClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillDefinitionSO");
        int wiredCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (skill == null)
            {
                continue;
            }

            var so = new SerializedObject(skill);
            SerializedProperty castSfxProperty = so.FindProperty("castSfx");
            if (castSfxProperty.objectReferenceValue == null)
            {
                castSfxProperty.objectReferenceValue = LoadClip(CastSfxPathFor(skill.Mechanic));
                so.ApplyModifiedProperties();
                wiredCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioSetup] Đã gán castSfx cho {wiredCount}/{guids.Length} SkillDefinitionSO (chỉ những skill chưa có sẵn).");
    }

    private static string CastSfxPathFor(SkillMechanic mechanic)
    {
        switch (mechanic)
        {
            case SkillMechanic.Projectile: return "Weapons/shot_muffled.wav";
            case SkillMechanic.ChainStrike: return "Other/whoosh_2.wav";
            case SkillMechanic.AreaStrike: return "Retro/explosion_small.wav";
            case SkillMechanic.DamageZone: return "Retro/wobble.wav";
            case SkillMechanic.Trap: return "Materials/metal_blunt_tap.wav";
            case SkillMechanic.PullVortex: return "Other/white_noise_short.wav";
            case SkillMechanic.SelfShield: return "Items/item_equip.wav";
            case SkillMechanic.ExecuteStrike: return "Combat and Gore/crunch_splat.wav";
            default: return "Other/whoosh_1.wav";
        }
    }

    [MenuItem("Tools/Dungeon/Audio/Wire Current Scene")]
    internal static void WireCurrentScene()
    {
        WireBuildingSystem();
        WirePhaseAudio();
        WireCrystalAudio();
        WireLobbyController();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[AudioSetup] Đã gắn/gán component âm thanh cho scene đang mở (bỏ qua phần không tìm thấy).");
    }

    private static void WireBuildingSystem()
    {
        var buildingSystem = Object.FindAnyObjectByType<GridBuildingSystem>();
        if (buildingSystem == null)
        {
            return;
        }

        BuildingAudioPlayer audioPlayer = GetOrAddComponent<BuildingAudioPlayer>(buildingSystem.gameObject);
        var so = new SerializedObject(audioPlayer);
        AssignClipArray(so, "placedClips", LoadClips("Materials/metal_clang.wav", "Materials/wood_small_drop.wav"));
        AssignClipArray(so, "placementFailedClips", LoadClips("UI/sci_fi_error.wav"));
        AssignClipArray(so, "insufficientFundsClips", LoadClips("UI/sci_fi_disallow.wav"));
        AssignClipArray(so, "demolishedClips", LoadClips("Materials/cardboard_drop.wav", "Materials/cardboard_hit.wav"));
        AssignClipArray(so, "destroyedInCombatClips", LoadClips("Retro/explosion_medium.wav", "Retro/explosion_small.wav"));
        so.ApplyModifiedProperties();
    }

    private static void WirePhaseAudio()
    {
        var dungeonManager = Object.FindAnyObjectByType<DungeonManager>();
        var phaseManager = Object.FindAnyObjectByType<PhaseManager>();
        var phase2Director = Object.FindAnyObjectByType<Phase2Director>();
        if (dungeonManager == null && phaseManager == null && phase2Director == null)
        {
            return;
        }

        GameObject host = dungeonManager != null ? dungeonManager.gameObject
            : phaseManager != null ? phaseManager.gameObject
            : phase2Director.gameObject;

        PhaseAudioPlayer audioPlayer = GetOrAddComponent<PhaseAudioPlayer>(host);
        var so = new SerializedObject(audioPlayer);
        AssignReference(so, "dungeonManager", dungeonManager);
        AssignReference(so, "phaseManager", phaseManager);
        AssignReference(so, "phase2Director", phase2Director);
        AssignClip(so, "dungeonReadyClip", "Musical Effects/8_bit_level_start.wav");
        AssignClip(so, "phaseTransitionClip", "Musical Effects/8_bit_level_complete.wav");
        AssignClip(so, "victoryClip", "Musical Effects/8_bit_positive_long.wav");
        AssignClip(so, "defeatClip", "Musical Effects/8_bit_defeated.wav");
        so.ApplyModifiedProperties();
    }

    private static void WireCrystalAudio()
    {
        var anyCrystal = Object.FindAnyObjectByType<CrystalNode>();
        var dungeonManager = Object.FindAnyObjectByType<DungeonManager>();
        if (anyCrystal == null && dungeonManager == null)
        {
            return;
        }

        GameObject host = dungeonManager != null ? dungeonManager.gameObject : anyCrystal.gameObject;
        CrystalAudioPlayer audioPlayer = GetOrAddComponent<CrystalAudioPlayer>(host);
        var so = new SerializedObject(audioPlayer);
        AssignClipArray(so, "capturedClips", LoadClips("Musical Effects/8_bit_negative.wav", "Musical Effects/8_bit_negative_quick.wav"));
        AssignClipArray(so, "activatedClips", LoadClips("Musical Effects/8_bit_chime_positive.wav"));
        so.ApplyModifiedProperties();
    }

    private static void WireLobbyController()
    {
        var lobbyController = Object.FindAnyObjectByType<LobbyController>();
        if (lobbyController == null)
        {
            return;
        }

        var so = new SerializedObject(lobbyController);
        AssignClip(so, "clickSfx", "UI/sci_fi_select.wav");
        AssignClip(so, "confirmSfx", "UI/sci_fi_confirm.wav");
        AssignClip(so, "deniedSfx", "UI/sci_fi_disallow.wav");
        AssignClip(so, "startRunSfx", "UI/sci_fi_select_big.wav");
        so.ApplyModifiedProperties();
    }

    private static void AssignClip(SerializedObject so, string propertyName, string relativeClipPath)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = LoadClip(relativeClipPath);
        }
    }

    private static void AssignReference(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void AssignClipArray(SerializedObject so, string propertyName, AudioClip[] clips)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.arraySize = clips.Length;
        for (int i = 0; i < clips.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }
    }

    private static AudioClip[] LoadClips(params string[] relativePaths)
    {
        var clips = new AudioClip[relativePaths.Length];
        for (int i = 0; i < relativePaths.Length; i++)
        {
            clips[i] = LoadClip(relativePaths[i]);
        }
        return clips;
    }

    private static AudioClip LoadClip(string relativePath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + relativePath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioSetup] Không tải được clip: {AudioRoot + relativePath}");
        }
        return clip;
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }
}
