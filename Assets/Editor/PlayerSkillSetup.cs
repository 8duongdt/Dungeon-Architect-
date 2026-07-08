using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn hệ skill người chơi cho avatar "Player_1" trong SCENE PHASE 2 và gán 4 ô skill:
///   Phím 1 = FireBall (đạn bay, spam) | Phím 2 = Lightning (nảy chuỗi)
///   Phím 3 = Explosion (nuke)         | Phím 4 = SunStrike (nuke trễ 1.5s)
///
///   Menu: Tools > Dungeon > Setup Player Skills (Phase 2)
///
/// Scene Phase 2 được mở ADDITIVE nếu chưa mở (không đụng scene đang làm việc),
/// avatar được đảm bảo có UnitFaction (phe Player) rồi gắn PlayerSkillCaster,
/// lưu scene và đóng lại nếu do tool mở. Chạy lại an toàn.
/// </summary>
public static class PlayerSkillSetup
{
    private const string Phase2ScenePath = "Assets/Scenes/Phase 2.unity";
    private const float DefaultMagicPower = 10f;

    private static readonly string[] SlotSkillNames = { "FireBall", "Lightning", "Explosion", "SunStrike" };

    [MenuItem("Tools/Dungeon/Setup Player Skills (Phase 2)")]
    public static void SetupPhaseTwo()
    {
        (Scene scene, bool openedByTool) = EnsurePhaseTwoLoaded();
        if (!scene.IsValid())
        {
            Debug.LogError($"[PlayerSkillSetup] Không mở được scene: {Phase2ScenePath}");
            return;
        }

        try
        {
            GameObject avatar = FindPlayerAvatarIn(scene);
            if (avatar == null)
            {
                Debug.LogError("[PlayerSkillSetup] Không tìm thấy avatar có PlayerControll trong Phase 2.");
                return;
            }

            EnsurePlayerFaction(avatar);
            AttachAndConfigureCaster(avatar);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PlayerSkillSetup] Hoàn tất: '{avatar.name}' trong Phase 2 có PlayerSkillCaster "
                + $"(1={SlotSkillNames[0]}, 2={SlotSkillNames[1]}, 3={SlotSkillNames[2]}, 4={SlotSkillNames[3]}).");
        }
        finally
        {
            if (openedByTool)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static (Scene scene, bool openedByTool) EnsurePhaseTwoLoaded()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loaded = SceneManager.GetSceneAt(i);
            if (loaded.path == Phase2ScenePath)
            {
                return (loaded, false);
            }
        }

        Scene opened = EditorSceneManager.OpenScene(Phase2ScenePath, OpenSceneMode.Additive);
        return (opened, true);
    }

    private static GameObject FindPlayerAvatarIn(Scene scene)
    {
        foreach (PlayerControll controller in Object.FindObjectsByType<PlayerControll>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (controller.gameObject.scene == scene)
            {
                return controller.gameObject;
            }
        }

        return null;
    }

    /// <summary>Avatar cần UnitFaction phe Player để cổng CanAttack của skill hoạt động.</summary>
    private static void EnsurePlayerFaction(GameObject avatar)
    {
        var faction = avatar.GetComponent<UnitFaction>();
        if (faction == null)
        {
            faction = avatar.AddComponent<UnitFaction>();
        }

        var serialized = new SerializedObject(faction);
        serialized.FindProperty("faction").enumValueIndex = (int)FactionType.Player;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AttachAndConfigureCaster(GameObject avatar)
    {
        var caster = avatar.GetComponent<PlayerSkillCaster>();
        if (caster == null)
        {
            caster = avatar.AddComponent<PlayerSkillCaster>();
        }

        var serialized = new SerializedObject(caster);
        SerializedProperty slots = serialized.FindProperty("skillSlots");
        slots.arraySize = SlotSkillNames.Length;
        for (int i = 0; i < SlotSkillNames.Length; i++)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>($"Assets/Skills/Skill_{SlotSkillNames[i]}.asset");
            if (skill == null)
            {
                Debug.LogWarning($"[PlayerSkillSetup] Thiếu asset Skill_{SlotSkillNames[i]} - chạy Tools > Dungeon > Setup Ranged Skills trước.");
            }
            slots.GetArrayElementAtIndex(i).objectReferenceValue = skill;
        }

        serialized.FindProperty("magicPower").floatValue = DefaultMagicPower;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
