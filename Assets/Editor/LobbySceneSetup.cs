using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dựng SCENE SẢNH CHỜ (Lobby) một thao tác:
///   Menu: Tools > Dungeon > Setup Lobby Scene
///
/// Scene gồm: tiêu đề + nhãn hai loại điểm, hai TAB chuyển bằng nút:
///   - Tab KỸ NĂNG: cây 3 cột (layout đọc thẳng từ Assets/Skills/SkillTree.asset) + 4 ô trang bị.
///   - Tab CÔNG TRÌNH: cây 2 cột (Kinh tế / Hỗ trợ chiến đấu, đọc từ BuildingUpgradeTree.asset).
/// Cùng nút "Bắt đầu" vào Phase 1. Wire toàn bộ SerializeField của LobbyController qua
/// SerializedObject. Lưu Assets/Scenes/Lobby.unity + thêm vào Build Settings sau MainMenu.
/// Chạy lại được nhiều lần (dựng lại từ đầu). Yêu cầu chạy Setup Skill Tree Asset
/// và Setup Building Tree Asset trước.
/// </summary>
public static class LobbySceneSetup
{
    private const string ScenePath = "Assets/Scenes/Lobby.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string TreeAssetPath = "Assets/Skills/SkillTree.asset";
    private const string BuildingTreeAssetPath = "Assets/Resources/BuildingUpgradeTree.asset";

    // Pixel-art pack (theo bản Lobby dựng tay trước đây) - Main_tiles là sheet Multiple sprite nên
    // phải LoadAllAssetsAtPath rồi so tên, LoadAssetAtPath<Sprite> không trả đúng sub-sprite.
    private const string PixelUiSheetPath = "Assets/Sprites/Free-Basic-Pixel-Art-UI-for-RPG/Main_tiles.png";
    private const string PixelPanelSpriteName = "Main_tiles_42";

    private const float ColumnSpacing = 480f;
    private const float TierRowHeight = 165f;
    private const float FirstTierY = 210f;
    private const float NodeSize = 96f;
    private const float NodeIconSize = 64f;
    private const float SiblingSpacing = 130f;
    private const float EquipSlotSize = 88f;
    private const float EquipSlotSpacing = 14f;

    private const float BuildingColumnSpacing = 560f;
    private const float BuildingRowHeight = 170f;
    private const float BuildingFirstRowY = 180f;
    private const float BuildingCardWidth = 420f;
    private const float BuildingCardHeight = 140f;
    private const float TabButtonWidth = 200f;
    private const float TabButtonHeight = 54f;

    private static readonly Color BackgroundColor = new Color(0.06f, 0.05f, 0.09f, 1f);
    private static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color TitleColor = new Color(0.96f, 0.9f, 0.72f);
    private static readonly Color BranchLabelColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color ConnectorColor = new Color(0.6f, 0.55f, 0.5f, 0.8f);

    private static readonly Dictionary<SkillBranch, string> BranchTitles = new Dictionary<SkillBranch, string>
    {
        { SkillBranch.Fire, "Fire - Destruction" },
        { SkillBranch.Lightning, "Lightning - Speed" },
        { SkillBranch.Control, "Control - Survival" },
    };

    private static readonly Dictionary<BuildingBranch, string> BuildingBranchTitles =
        new Dictionary<BuildingBranch, string>
    {
        { BuildingBranch.Economy, "ECONOMY" },
        { BuildingBranch.CombatSupport, "COMBAT SUPPORT" },
    };

    [MenuItem("Tools/Dungeon/Setup Lobby Scene")]
    public static void SetupLobbyScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Nạp asset SAU khi tạo scene mới: NewScene có thể unload asset đã nạp trước đó,
        // biến tham chiếu thành fake-null và wire ra fileID 0.
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeSO>(TreeAssetPath);
        if (tree == null || tree.Nodes.Count == 0)
        {
            Debug.LogError($"[LobbySceneSetup] Thiếu {TreeAssetPath} - chạy Tools > Dungeon > Setup Skill Tree Asset trước.");
            return;
        }

        var buildingTree = AssetDatabase.LoadAssetAtPath<BuildingUpgradeTreeSO>(BuildingTreeAssetPath);
        if (buildingTree == null || buildingTree.Nodes.Count == 0)
        {
            Debug.LogError($"[LobbySceneSetup] Thiếu {BuildingTreeAssetPath} - chạy Tools > Dungeon > Setup Building Tree Asset trước.");
            return;
        }

        EnsureCamera();
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform root = (RectTransform)canvas.transform;

        BuildBackground(root);
        TMP_Text pointsLabel = BuildHeader(root);

        RectTransform skillTreePanel = CreatePanel("SkillTreePanel", root);
        (SkillNodeView[] nodeViews, string[] nodeSkillNames) = BuildSkillTreeColumns(skillTreePanel, tree);
        EquipSlotView[] equipSlots = BuildEquipBar(skillTreePanel);

        RectTransform buildingTreePanel = CreatePanel("BuildingTreePanel", root);
        (BuildingNodeView[] buildingNodeViews, string[] buildingNodeIds) =
            BuildBuildingTreeColumns(buildingTreePanel, buildingTree);
        buildingTreePanel.gameObject.SetActive(false);

        (Button skillTabButton, Button buildingTabButton) = BuildTabButtons(root);
        Button startButton = BuildStartButton(root);

        var controller = canvas.gameObject.AddComponent<LobbyController>();
        WireController(controller, new LobbyWiring
        {
            tree = tree,
            nodeViews = nodeViews,
            nodeSkillNames = nodeSkillNames,
            buildingTree = buildingTree,
            buildingNodeViews = buildingNodeViews,
            buildingNodeIds = buildingNodeIds,
            skillTreePanel = skillTreePanel.gameObject,
            buildingTreePanel = buildingTreePanel.gameObject,
            skillTabButton = skillTabButton,
            buildingTabButton = buildingTabButton,
            equipSlots = equipSlots,
            pointsLabel = pointsLabel,
            startButton = startButton,
        });

        SaveSceneAndRegister(scene);
        Debug.Log($"[LobbySceneSetup] Đã dựng scene Lobby ({nodeViews.Length} node kỹ năng, "
            + $"{buildingNodeViews.Length} node công trình) và thêm vào Build Settings.");
    }

    // ---------------------------------------------------------------- Scene scaffolding

    private static void EnsureCamera()
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.orthographic = true;
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void EnsureEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private static Canvas CreateCanvas()
    {
        var go = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ---------------------------------------------------------------- UI regions

    private static void BuildBackground(RectTransform root)
    {
        RectTransform rect = CreateRect("Background", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(rect);
        AddImage(rect, null, BackgroundColor);
    }

    private static TMP_Text BuildHeader(RectTransform root)
    {
        TMP_Text title = CreateText("Title", root, "LOBBY", 44, TitleColor);
        SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(900f, 70f));

        TMP_Text points = CreateText("PointsLabel", root, "Skill Points: 0   |   Building Points: 0", 32, Color.white);
        SetAnchored((RectTransform)points.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 50f));
        return points;
    }

    // Panel gốc của một tab - RectTransform trống phủ kín canvas, LobbyController bật/tắt cả cụm.
    private static RectTransform CreatePanel(string name, RectTransform root)
    {
        RectTransform panel = CreateRect(name, root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(panel);
        return panel;
    }

    private static (Button, Button) BuildTabButtons(RectTransform root)
    {
        Button skillTab = BuildTabButton(root, "SkillTabButton", "SKILLS", -780f);
        Button buildingTab = BuildTabButton(root, "BuildingTabButton", "BUILDINGS", -560f);
        return (skillTab, buildingTab);
    }

    private static Button BuildTabButton(RectTransform root, string name, string label, float offsetX)
    {
        RectTransform rect = CreateRect(name, root, new Vector2(0.5f, 1f),
            new Vector2(offsetX, -52f), new Vector2(TabButtonWidth, TabButtonHeight));
        Image image = AddImage(rect, LoadUiSprite(PixelPanelSpriteName), PanelTint);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateText("Label", rect, label, 24, Color.white);
        SetStretch((RectTransform)text.transform);
        return button;
    }

    /// <summary>
    /// Dựng 3 cột nhánh từ dữ liệu cây: node cùng nhánh cùng bậc đứng cạnh nhau,
    /// giữa hai hàng có nhãn "▼" gợi ý thứ tự mở khóa.
    /// </summary>
    private static (SkillNodeView[], string[]) BuildSkillTreeColumns(RectTransform root, SkillTreeSO tree)
    {
        var nodeViews = new List<SkillNodeView>();
        var nodeSkillNames = new List<string>();

        foreach (SkillBranch branch in System.Enum.GetValues(typeof(SkillBranch)))
        {
            var branchNodes = tree.Nodes.Where(n => n.branch == branch).ToList();
            if (branchNodes.Count == 0)
            {
                continue;
            }

            float columnX = ((int)branch - 1) * ColumnSpacing;
            BuildBranchTitle(root, branch, columnX);
            BuildBranchTiers(root, branchNodes, columnX, nodeViews, nodeSkillNames);
        }

        return (nodeViews.ToArray(), nodeSkillNames.ToArray());
    }

    private static void BuildBranchTitle(RectTransform root, SkillBranch branch, float columnX)
    {
        TMP_Text label = CreateText($"BranchTitle_{branch}", root, BranchTitles[branch], 28, BranchLabelColor);
        SetAnchored((RectTransform)label.transform, new Vector2(0.5f, 0.5f),
            new Vector2(columnX, FirstTierY + 100f), new Vector2(420f, 44f));
    }

    private static void BuildBranchTiers(RectTransform root, List<SkillTreeSO.SkillTreeNode> branchNodes,
        float columnX, List<SkillNodeView> nodeViews, List<string> nodeSkillNames)
    {
        int maxTier = branchNodes.Max(n => n.tier);
        for (int tier = 1; tier <= maxTier; tier++)
        {
            float rowY = FirstTierY - (tier - 1) * TierRowHeight;
            var tierNodes = branchNodes.Where(n => n.tier == tier).ToList();
            for (int i = 0; i < tierNodes.Count; i++)
            {
                // Node cùng bậc dàn đều quanh tâm cột (2 node -> lệch trái/phải).
                float siblingOffset = (i - (tierNodes.Count - 1) * 0.5f) * SiblingSpacing;
                SkillNodeView view = BuildSkillNode(root, tierNodes[i],
                    new Vector2(columnX + siblingOffset, rowY));
                nodeViews.Add(view);
                nodeSkillNames.Add(tierNodes[i].skill.SkillName);
            }

            if (tier < maxTier)
            {
                TMP_Text connector = CreateText($"Connector_{columnX}_{tier}", root, "▼", 26, ConnectorColor);
                SetAnchored((RectTransform)connector.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(columnX, rowY - TierRowHeight * 0.5f), new Vector2(50f, 36f));
            }
        }
    }

    private static SkillNodeView BuildSkillNode(RectTransform root, SkillTreeSO.SkillTreeNode node, Vector2 position)
    {
        string skillName = node.skill.SkillName;
        RectTransform frame = CreateRect($"Node_{skillName}", root, new Vector2(0.5f, 0.5f),
            position, new Vector2(NodeSize, NodeSize));
        Image frameImage = AddImage(frame, LoadUiSprite(PixelPanelSpriteName), Color.white);
        frameImage.raycastTarget = true;
        Button button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = frameImage;

        RectTransform icon = CreateRect("Icon", frame, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(NodeIconSize, NodeIconSize));
        Image iconImage = AddImage(icon, node.skill.Icon, Color.white);

        TMP_Text costLabel = CreateText("Cost", frame, node.cost.ToString(), 22, new Color(0.95f, 0.78f, 0.25f));
        SetAnchored((RectTransform)costLabel.transform, new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(40f, 30f));

        TMP_Text levelLabel = CreateText("Level", frame, string.Empty, 18, Color.white);
        SetAnchored((RectTransform)levelLabel.transform, new Vector2(0.5f, 0f), new Vector2(0f, -18f), new Vector2(170f, 26f));

        Button upgradeButton = BuildNodeUpgradeButton(frame);

        var view = frame.gameObject.AddComponent<SkillNodeView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("frame").objectReferenceValue = frameImage;
            so.FindProperty("icon").objectReferenceValue = iconImage;
            so.FindProperty("costLabel").objectReferenceValue = costLabel;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
        });
        return view;
    }

    // Nút "+" nâng bậc góc trên-phải của node (dùng chung cho node kỹ năng lẫn công trình).
    private static Button BuildNodeUpgradeButton(RectTransform frame)
    {
        RectTransform rect = CreateRect("UpgradeButton", frame, new Vector2(1f, 1f),
            new Vector2(4f, 4f), new Vector2(34f, 34f));
        Image image = AddImage(rect, LoadUiSprite(PixelPanelSpriteName), Color.white);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text plus = CreateText("Plus", rect, "+", 24, new Color(0.95f, 0.78f, 0.25f));
        SetStretch((RectTransform)plus.transform);
        return button;
    }

    private static EquipSlotView[] BuildEquipBar(RectTransform root)
    {
        int slotCount = PlayerSkillCaster.SlotCount;
        float barWidth = slotCount * EquipSlotSize + (slotCount + 1) * EquipSlotSpacing;
        RectTransform bar = CreateRect("EquipBar", root, new Vector2(0.5f, 0f),
            new Vector2(0f, 96f), new Vector2(barWidth, EquipSlotSize + 2f * EquipSlotSpacing));
        AddImage(bar, LoadUiSprite(PixelPanelSpriteName), PanelTint);

        TMP_Text hint = CreateText("Hint", bar, "Skill slots (keys 1-4) - click an unlocked node to equip, click a slot to unequip", 20,
            BranchLabelColor);
        SetAnchored((RectTransform)hint.transform, new Vector2(0.5f, 1f), new Vector2(0f, 26f), new Vector2(900f, 32f));

        var slots = new EquipSlotView[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = BuildEquipSlot(bar, i);
        }
        return slots;
    }

    private static EquipSlotView BuildEquipSlot(RectTransform bar, int slotIndex)
    {
        float slotX = EquipSlotSpacing + slotIndex * (EquipSlotSize + EquipSlotSpacing) + EquipSlotSize * 0.5f;
        RectTransform slot = CreateRect($"EquipSlot{slotIndex + 1}", bar, new Vector2(0f, 0.5f),
            new Vector2(slotX, 0f), new Vector2(EquipSlotSize, EquipSlotSize));
        Image slotImage = AddImage(slot, LoadUiSprite(PixelPanelSpriteName), Color.white);
        slotImage.raycastTarget = true;
        Button button = slot.gameObject.AddComponent<Button>();
        button.targetGraphic = slotImage;

        RectTransform icon = CreateRect("Icon", slot, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(NodeIconSize, NodeIconSize));
        Image iconImage = AddImage(icon, null, Color.white);
        iconImage.enabled = false;

        TMP_Text keyLabel = CreateText("Key", slot, (slotIndex + 1).ToString(), 20, Color.white);
        SetAnchored((RectTransform)keyLabel.transform, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(28f, 28f));

        var view = slot.gameObject.AddComponent<EquipSlotView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("icon").objectReferenceValue = iconImage;
            so.FindProperty("keyLabel").objectReferenceValue = keyLabel;
            so.FindProperty("button").objectReferenceValue = button;
        });
        return view;
    }

    /// <summary>
    /// Dựng 2 cột nhánh cây công trình: node xếp dọc theo bậc hiển thị (tier),
    /// mỗi node là một card văn bản (tên + hiệu ứng + bậc + giá + nút "+").
    /// </summary>
    private static (BuildingNodeView[], string[]) BuildBuildingTreeColumns(
        RectTransform panel, BuildingUpgradeTreeSO tree)
    {
        var nodeViews = new List<BuildingNodeView>();
        var nodeIds = new List<string>();

        foreach (BuildingBranch branch in System.Enum.GetValues(typeof(BuildingBranch)))
        {
            var branchNodes = tree.Nodes.Where(n => n.branch == branch).OrderBy(n => n.tier).ToList();
            if (branchNodes.Count == 0)
            {
                continue;
            }

            float columnX = ((int)branch - 0.5f) * BuildingColumnSpacing;
            TMP_Text title = CreateText($"BuildingBranchTitle_{branch}", panel,
                BuildingBranchTitles[branch], 28, BranchLabelColor);
            SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 0.5f),
                new Vector2(columnX, BuildingFirstRowY + 110f), new Vector2(460f, 44f));

            for (int row = 0; row < branchNodes.Count; row++)
            {
                float rowY = BuildingFirstRowY - row * BuildingRowHeight;
                BuildingNodeView view = BuildBuildingNode(panel, branchNodes[row], new Vector2(columnX, rowY));
                nodeViews.Add(view);
                nodeIds.Add(branchNodes[row].id);
            }
        }

        return (nodeViews.ToArray(), nodeIds.ToArray());
    }

    private static BuildingNodeView BuildBuildingNode(RectTransform panel,
        BuildingUpgradeTreeSO.BuildingUpgradeNode node, Vector2 position)
    {
        RectTransform frame = CreateRect($"BuildingNode_{node.id}", panel, new Vector2(0.5f, 0.5f),
            position, new Vector2(BuildingCardWidth, BuildingCardHeight));
        Image frameImage = AddImage(frame, LoadUiSprite(PixelPanelSpriteName), Color.white);

        TMP_Text nameLabel = CreateText("Name", frame, node.displayName, 26, TitleColor);
        SetAnchored((RectTransform)nameLabel.transform, new Vector2(0.5f, 1f), new Vector2(-20f, -28f),
            new Vector2(BuildingCardWidth - 90f, 34f));

        TMP_Text effectLabel = CreateText("Effect", frame, string.Empty, 20, Color.white);
        SetAnchored((RectTransform)effectLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(-20f, -2f),
            new Vector2(BuildingCardWidth - 90f, 30f));

        TMP_Text levelLabel = CreateText("Level", frame, string.Empty, 20, BranchLabelColor);
        SetAnchored((RectTransform)levelLabel.transform, new Vector2(0f, 0f), new Vector2(80f, 26f),
            new Vector2(130f, 28f));

        TMP_Text costLabel = CreateText("Cost", frame, string.Empty, 20, new Color(0.95f, 0.78f, 0.25f));
        SetAnchored((RectTransform)costLabel.transform, new Vector2(1f, 0f), new Vector2(-130f, 26f),
            new Vector2(190f, 28f));

        Button upgradeButton = BuildNodeUpgradeButton(frame);
        ((RectTransform)upgradeButton.transform).anchoredPosition = new Vector2(-26f, -26f);

        var view = frame.gameObject.AddComponent<BuildingNodeView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("frame").objectReferenceValue = frameImage;
            so.FindProperty("nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("effectLabel").objectReferenceValue = effectLabel;
            so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("costLabel").objectReferenceValue = costLabel;
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
        });
        return view;
    }

    private static Button BuildStartButton(RectTransform root)
    {
        RectTransform rect = CreateRect("StartButton", root, new Vector2(1f, 0f),
            new Vector2(-170f, 110f), new Vector2(240f, 78f));
        Image image = AddImage(rect, LoadUiSprite(PixelPanelSpriteName), Color.white);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateText("Label", rect, "START", 30, Color.white);
        SetStretch((RectTransform)label.transform);
        return button;
    }

    // Gom mọi tham chiếu cần wire vào LobbyController - tránh chữ ký hàm 13 tham số.
    private class LobbyWiring
    {
        public SkillTreeSO tree;
        public SkillNodeView[] nodeViews;
        public string[] nodeSkillNames;
        public BuildingUpgradeTreeSO buildingTree;
        public BuildingNodeView[] buildingNodeViews;
        public string[] buildingNodeIds;
        public GameObject skillTreePanel;
        public GameObject buildingTreePanel;
        public Button skillTabButton;
        public Button buildingTabButton;
        public EquipSlotView[] equipSlots;
        public TMP_Text pointsLabel;
        public Button startButton;
    }

    private static void WireController(LobbyController controller, LobbyWiring wiring)
    {
        SetSerialized(controller, so =>
        {
            so.FindProperty("skillTree").objectReferenceValue = wiring.tree;
            so.FindProperty("buildingTree").objectReferenceValue = wiring.buildingTree;
            so.FindProperty("skillTreePanel").objectReferenceValue = wiring.skillTreePanel;
            so.FindProperty("buildingTreePanel").objectReferenceValue = wiring.buildingTreePanel;
            so.FindProperty("skillTabButton").objectReferenceValue = wiring.skillTabButton;
            so.FindProperty("buildingTabButton").objectReferenceValue = wiring.buildingTabButton;
            so.FindProperty("pointsLabel").objectReferenceValue = wiring.pointsLabel;
            so.FindProperty("startButton").objectReferenceValue = wiring.startButton;

            WriteViewArray(so, "nodeViews", "nodeSkillNames", wiring.nodeViews, wiring.nodeSkillNames);
            WriteViewArray(so, "buildingNodeViews", "buildingNodeIds", wiring.buildingNodeViews, wiring.buildingNodeIds);

            SerializedProperty slots = so.FindProperty("equipSlots");
            slots.arraySize = wiring.equipSlots.Length;
            for (int i = 0; i < wiring.equipSlots.Length; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = wiring.equipSlots[i];
            }
        });
    }

    // Ghi cặp mảng song song (view + khóa chuỗi) vào SerializedObject.
    private static void WriteViewArray(SerializedObject so, string viewsProperty, string keysProperty,
        Object[] views, string[] keys)
    {
        SerializedProperty viewsProp = so.FindProperty(viewsProperty);
        SerializedProperty keysProp = so.FindProperty(keysProperty);
        viewsProp.arraySize = views.Length;
        keysProp.arraySize = keys.Length;
        for (int i = 0; i < views.Length; i++)
        {
            viewsProp.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            keysProp.GetArrayElementAtIndex(i).stringValue = keys[i];
        }
    }

    // ---------------------------------------------------------------- Save & Build Settings

    private static void SaveSceneAndRegister(Scene scene)
    {
        EditorSceneManager.SaveScene(scene, ScenePath);
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath))
        {
            return;
        }

        var lobbyScene = new EditorBuildSettingsScene(ScenePath, true);
        int mainMenuIndex = scenes.FindIndex(s => s.path == MainMenuScenePath);
        scenes.Insert(mainMenuIndex >= 0 ? mainMenuIndex + 1 : scenes.Count, lobbyScene);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------------------------------------------------------------- UI helpers (theo StoryIntroSetup)

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null && sprite.border.sqrMagnitude > 0f)
        {
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 60f));
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private static void SetSerialized(Object target, System.Action<SerializedObject> apply)
    {
        var so = new SerializedObject(target);
        apply(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Dictionary<string, Sprite> pixelUiSpriteCache;

    // Sheet Multiple sprite - nạp hết rồi so tên một lần, cache lại vì hàm này gọi nhiều lần trong 1 lượt dựng scene.
    private static Sprite LoadUiSprite(string spriteName)
    {
        if (pixelUiSpriteCache == null)
        {
            pixelUiSpriteCache = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(PixelUiSheetPath))
            {
                if (asset is Sprite sprite)
                {
                    pixelUiSpriteCache[sprite.name] = sprite;
                }
            }
        }

        if (!pixelUiSpriteCache.TryGetValue(spriteName, out Sprite found))
        {
            Debug.LogWarning($"[LobbySceneSetup] Không tìm thấy sprite UI: {spriteName} trong {PixelUiSheetPath}");
        }

        return found;
    }
}
