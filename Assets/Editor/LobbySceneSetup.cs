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
/// Scene gồm: tiêu đề + nhãn điểm kỹ năng, cây kỹ năng 3 cột (layout đọc thẳng từ
/// Assets/Skills/SkillTree.asset - nhánh -> cột, bậc -> hàng), thanh 4 ô trang bị,
/// nút "Bắt đầu" vào Phase 1. Wire toàn bộ SerializeField của LobbyController qua
/// SerializedObject. Lưu Assets/Scenes/Lobby.unity + thêm vào Build Settings sau MainMenu.
/// Chạy lại được nhiều lần (dựng lại từ đầu). Yêu cầu chạy Setup Skill Tree Asset trước.
/// </summary>
public static class LobbySceneSetup
{
    private const string ScenePath = "Assets/Scenes/Lobby.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string TreeAssetPath = "Assets/Skills/SkillTree.asset";
    private const string UiPackFolder = "Assets/Sprites/Vector_UI_Pack_dobo_ui";

    private const float ColumnSpacing = 480f;
    private const float TierRowHeight = 165f;
    private const float FirstTierY = 210f;
    private const float NodeSize = 96f;
    private const float NodeIconSize = 64f;
    private const float SiblingSpacing = 130f;
    private const float EquipSlotSize = 88f;
    private const float EquipSlotSpacing = 14f;

    private static readonly Color BackgroundColor = new Color(0.06f, 0.05f, 0.09f, 1f);
    private static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color TitleColor = new Color(0.96f, 0.9f, 0.72f);
    private static readonly Color BranchLabelColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color ConnectorColor = new Color(0.6f, 0.55f, 0.5f, 0.8f);
    private static readonly Color StartButtonTint = new Color(0.16f, 0.32f, 0.16f, 0.95f);

    private static readonly Dictionary<SkillBranch, string> BranchTitles = new Dictionary<SkillBranch, string>
    {
        { SkillBranch.Fire, "Hỏa - Hủy Diệt" },
        { SkillBranch.Lightning, "Lôi - Tốc Độ" },
        { SkillBranch.Control, "Khống Chế - Sinh Tồn" },
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
        EnsureCamera();
        EnsureEventSystem();
        Canvas canvas = CreateCanvas();
        RectTransform root = (RectTransform)canvas.transform;

        BuildBackground(root);
        TMP_Text skillPointsLabel = BuildHeader(root);
        (SkillNodeView[] nodeViews, string[] nodeSkillNames) = BuildSkillTreeColumns(root, tree);
        EquipSlotView[] equipSlots = BuildEquipBar(root);
        Button startButton = BuildStartButton(root);

        var controller = canvas.gameObject.AddComponent<LobbyController>();
        WireController(controller, tree, nodeViews, nodeSkillNames, equipSlots, skillPointsLabel, startButton);

        SaveSceneAndRegister(scene);
        Debug.Log($"[LobbySceneSetup] Đã dựng scene Lobby ({nodeViews.Length} node) và thêm vào Build Settings.");
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
        TMP_Text title = CreateText("Title", root, "SẢNH CHỜ - CÂY KỸ NĂNG", 44, TitleColor);
        SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(900f, 70f));

        TMP_Text points = CreateText("SkillPointsLabel", root, "Điểm kỹ năng: 0", 32, Color.white);
        SetAnchored((RectTransform)points.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 50f));
        return points;
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
        Image frameImage = AddImage(frame, LoadUiSprite("Item Slots/itemSlot_cyan.png"), Color.white);
        frameImage.raycastTarget = true;
        Button button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = frameImage;

        RectTransform icon = CreateRect("Icon", frame, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(NodeIconSize, NodeIconSize));
        Image iconImage = AddImage(icon, node.skill.Icon, Color.white);

        TMP_Text costLabel = CreateText("Cost", frame, node.cost.ToString(), 22, new Color(0.95f, 0.78f, 0.25f));
        SetAnchored((RectTransform)costLabel.transform, new Vector2(1f, 0f), new Vector2(-14f, 14f), new Vector2(40f, 30f));

        var view = frame.gameObject.AddComponent<SkillNodeView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("frame").objectReferenceValue = frameImage;
            so.FindProperty("icon").objectReferenceValue = iconImage;
            so.FindProperty("costLabel").objectReferenceValue = costLabel;
            so.FindProperty("button").objectReferenceValue = button;
        });
        return view;
    }

    private static EquipSlotView[] BuildEquipBar(RectTransform root)
    {
        int slotCount = PlayerSkillCaster.SlotCount;
        float barWidth = slotCount * EquipSlotSize + (slotCount + 1) * EquipSlotSpacing;
        RectTransform bar = CreateRect("EquipBar", root, new Vector2(0.5f, 0f),
            new Vector2(0f, 96f), new Vector2(barWidth, EquipSlotSize + 2f * EquipSlotSpacing));
        AddImage(bar, LoadUiSprite("Panels/panel_black.png"), PanelTint);

        TMP_Text hint = CreateText("Hint", bar, "Ô kỹ năng (phím 1-4) - click node đã mở để trang bị, click ô để gỡ", 20,
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
        Image slotImage = AddImage(slot, LoadUiSprite("Item Slots/itemSlot_cyan.png"), Color.white);
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

    private static Button BuildStartButton(RectTransform root)
    {
        RectTransform rect = CreateRect("StartButton", root, new Vector2(1f, 0f),
            new Vector2(-170f, 110f), new Vector2(240f, 78f));
        Image image = AddImage(rect, LoadUiSprite("Panels/panel_black.png"), StartButtonTint);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text label = CreateText("Label", rect, "BẮT ĐẦU", 30, Color.white);
        SetStretch((RectTransform)label.transform);
        return button;
    }

    private static void WireController(LobbyController controller, SkillTreeSO tree,
        SkillNodeView[] nodeViews, string[] nodeSkillNames, EquipSlotView[] equipSlots,
        TMP_Text skillPointsLabel, Button startButton)
    {
        SetSerialized(controller, so =>
        {
            so.FindProperty("skillTree").objectReferenceValue = tree;
            so.FindProperty("skillPointsLabel").objectReferenceValue = skillPointsLabel;
            so.FindProperty("startButton").objectReferenceValue = startButton;

            SerializedProperty views = so.FindProperty("nodeViews");
            SerializedProperty names = so.FindProperty("nodeSkillNames");
            views.arraySize = nodeViews.Length;
            names.arraySize = nodeSkillNames.Length;
            for (int i = 0; i < nodeViews.Length; i++)
            {
                views.GetArrayElementAtIndex(i).objectReferenceValue = nodeViews[i];
                names.GetArrayElementAtIndex(i).stringValue = nodeSkillNames[i];
            }

            SerializedProperty slots = so.FindProperty("equipSlots");
            slots.arraySize = equipSlots.Length;
            for (int i = 0; i < equipSlots.Length; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = equipSlots[i];
            }
        });
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

    private static Sprite LoadUiSprite(string relativePath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiPackFolder}/{relativePath}");
        if (sprite == null)
        {
            Debug.LogWarning($"[LobbySceneSetup] Không tìm thấy sprite UI: {relativePath}");
        }
        return sprite;
    }
}
