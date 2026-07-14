using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dựng TRẬN ĐÁNH PHASE 2 một thao tác:
///   Menu: Tools > Dungeon > Setup Phase 2 Battle
///
/// Thêm vào scene Phase 2: 3 cổng sinh quái phe Human quanh người chơi (hạn ngạch hữu hạn
/// để trận có hồi kết), panel kết quả ẩn dưới HUD canvas, Phase2Director xét thắng/thua
/// (diệt sạch = thắng +điểm kỹ năng, chết = thua) rồi về sảnh chờ, và EquippedSkillLoader
/// nạp skill đã trang bị ở Lobby vào PlayerSkillCaster. Scene mở ADDITIVE nếu chưa mở;
/// object do tool tạo được dựng lại từ đầu mỗi lần chạy - chạy lại an toàn.
/// Yêu cầu: Setup Player Skills (Phase 2), Setup Phase 2 HUD, Setup Skill Tree Asset chạy trước.
/// </summary>
public static class Phase2BattleSetup
{
    private const string Phase2ScenePath = "Assets/Scenes/Phase 2.unity";
    private const string TreeAssetPath = "Assets/Skills/SkillTree.asset";
    private const string HudCanvasName = "Phase2HudCanvas";
    private const string SpawnerRootName = "EnemySpawners";
    private const string DirectorName = "Phase2Director";
    private const string ResultPanelName = "BattleResultPanel";

    private const int MaxTotalSpawnsPerGate = 8;
    private const float InitialSpawnDelay = 5f;
    private const float SpawnInterval = 15f;
    private const int MaxAliveEnemiesPerGate = 6;
    private const int WaveBudget = 6;

    // Thắng Phase 2 = kết thúc một run: thưởng +1 điểm cho mỗi cây (kỹ năng + công trình).
    private const int WinSkillPointReward = 1;
    private const int WinBuildingPointReward = 1;

    // (prefab, giá điểm ngân sách) - quái phe Human (faction Enemy) làm mục tiêu cho người chơi.
    private static readonly (string prefabPath, int cost)[] EnemyBudgetEntries =
    {
        ("Assets/Prefabs/Human/Swordsman_lvl1.prefab", 1),
        ("Assets/Prefabs/Human/Swordsman_lvl2.prefab", 2),
        ("Assets/Prefabs/Human/TheTank_Apprentice.prefab", 3),
        ("Assets/Prefabs/Human/Pastor_Apprentice.prefab", 3),
    };

    // Cổng đặt lệch quanh người chơi - đủ xa để không bị vây ngay khi vào trận.
    private static readonly Vector2[] SpawnerOffsets =
    {
        new Vector2(9f, 6f),
        new Vector2(-9f, 6f),
        new Vector2(0f, -8f),
    };

    private static readonly Color ResultPanelColor = new Color(0f, 0f, 0f, 0.78f);

    [MenuItem("Tools/Dungeon/Setup Phase 2 Battle")]
    public static void SetupBattle()
    {
        (Scene scene, bool openedByTool) = EnsurePhaseTwoLoaded();
        if (!scene.IsValid())
        {
            Debug.LogError($"[Phase2BattleSetup] Không mở được scene: {Phase2ScenePath}");
            return;
        }

        try
        {
            GameObject avatar = FindPlayerAvatarIn(scene);
            var tree = AssetDatabase.LoadAssetAtPath<SkillTreeSO>(TreeAssetPath);
            if (avatar == null || tree == null)
            {
                Debug.LogError("[Phase2BattleSetup] Thiếu avatar (PlayerControll) trong Phase 2 hoặc SkillTree.asset "
                    + "- chạy Setup Player Skills và Setup Skill Tree Asset trước.");
                return;
            }

            BuildSpawners(scene, avatar.transform.position);
            (GameObject resultPanel, TMP_Text resultLabel) = BuildResultPanel(scene);
            BuildDirector(scene, avatar, resultPanel, resultLabel);
            AttachEquippedSkillLoader(avatar, tree);
            AttachPhase2BuffApplier(avatar);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Phase2BattleSetup] Hoàn tất: {SpawnerOffsets.Length} cổng sinh quái + Phase2Director trong Phase 2.");
        }
        finally
        {
            if (openedByTool)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    // ---------------------------------------------------------------- Spawners

    private static void BuildSpawners(Scene scene, Vector3 playerPosition)
    {
        DestroyExisting(scene, SpawnerRootName);
        var root = new GameObject(SpawnerRootName);
        SceneManager.MoveGameObjectToScene(root, scene);

        for (int i = 0; i < SpawnerOffsets.Length; i++)
        {
            var gate = new GameObject($"EnemyGate_{i + 1}");
            gate.transform.SetParent(root.transform);
            gate.transform.position = playerPosition + (Vector3)SpawnerOffsets[i];
            ConfigureSpawner(gate.AddComponent<EnemySpawner>());
        }
    }

    private static void ConfigureSpawner(EnemySpawner spawner)
    {
        var serialized = new SerializedObject(spawner);
        SerializedProperty entries = serialized.FindProperty("budgetEntries");
        entries.arraySize = EnemyBudgetEntries.Length;
        for (int i = 0; i < EnemyBudgetEntries.Length; i++)
        {
            (string prefabPath, int cost) = EnemyBudgetEntries[i];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Phase2BattleSetup] Thiếu prefab quái: {prefabPath}");
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            entry.FindPropertyRelative("cost").intValue = cost;
        }

        serialized.FindProperty("maxTotalSpawns").intValue = MaxTotalSpawnsPerGate;
        serialized.FindProperty("initialSpawnDelay").floatValue = InitialSpawnDelay;
        serialized.FindProperty("spawnInterval").floatValue = SpawnInterval;
        serialized.FindProperty("maxEnemies").intValue = MaxAliveEnemiesPerGate;
        serialized.FindProperty("waveBudget").intValue = WaveBudget;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- Result panel

    private static (GameObject panel, TMP_Text label) BuildResultPanel(Scene scene)
    {
        GameObject canvas = FindInScene(scene, HudCanvasName);
        if (canvas == null)
        {
            Debug.LogError($"[Phase2BattleSetup] Không thấy '{HudCanvasName}' - chạy Tools > Dungeon > Setup Phase 2 HUD trước.");
            return (null, null);
        }

        Transform existing = canvas.transform.Find(ResultPanelName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var panel = new GameObject(ResultPanelName, typeof(RectTransform), typeof(Image));
        var panelRect = (RectTransform)panel.transform;
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = ResultPanelColor;

        var labelGo = new GameObject("ResultLabel", typeof(RectTransform));
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(panelRect, false);
        labelRect.sizeDelta = new Vector2(1200f, 300f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 72;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.9f, 0.72f);
        label.textWrappingMode = TextWrappingModes.Normal;

        panel.SetActive(false);
        return (panel, label);
    }

    // ---------------------------------------------------------------- Director & loader

    private static void BuildDirector(Scene scene, GameObject avatar, GameObject resultPanel, TMP_Text resultLabel)
    {
        DestroyExisting(scene, DirectorName);
        var directorGo = new GameObject(DirectorName);
        SceneManager.MoveGameObjectToScene(directorGo, scene);

        var director = directorGo.AddComponent<Phase2Director>();
        var serialized = new SerializedObject(director);
        serialized.FindProperty("playerHealth").objectReferenceValue = avatar.GetComponent<UnitHealth>();
        serialized.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        serialized.FindProperty("resultLabel").objectReferenceValue = resultLabel;
        // Ghi đè giá trị bake sẵn trong scene cũ (trước đây +2 điểm kỹ năng).
        serialized.FindProperty("winSkillPointReward").intValue = WinSkillPointReward;
        serialized.FindProperty("winBuildingPointReward").intValue = WinBuildingPointReward;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Buff nhánh Hỗ trợ chiến đấu của cây công trình (khiên/hồi máu/sát thương) áp lúc vào trận.
    private static void AttachPhase2BuffApplier(GameObject avatar)
    {
        var applier = avatar.GetComponent<Phase2BuffApplier>();
        if (applier == null)
        {
            applier = avatar.AddComponent<Phase2BuffApplier>();
        }

        var serialized = new SerializedObject(applier);
        serialized.FindProperty("playerHealth").objectReferenceValue = avatar.GetComponent<UnitHealth>();
        serialized.FindProperty("caster").objectReferenceValue = avatar.GetComponent<PlayerSkillCaster>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AttachEquippedSkillLoader(GameObject avatar, SkillTreeSO tree)
    {
        var loader = avatar.GetComponent<EquippedSkillLoader>();
        if (loader == null)
        {
            loader = avatar.AddComponent<EquippedSkillLoader>();
        }

        var serialized = new SerializedObject(loader);
        serialized.FindProperty("skillTree").objectReferenceValue = tree;
        serialized.FindProperty("caster").objectReferenceValue = avatar.GetComponent<PlayerSkillCaster>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- Scene helpers (theo PlayerSkillSetup)

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

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static void DestroyExisting(Scene scene, string name)
    {
        GameObject existing = FindInScene(scene, name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
