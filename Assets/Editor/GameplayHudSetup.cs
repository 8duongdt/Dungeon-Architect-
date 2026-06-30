using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng và nối dây HUD trong màn chơi (Phase 1) chỉ bằng một thao tác:
///   Menu Tools > UI > Build Gameplay HUD
///
/// Tạo (nếu thiếu) các manager kinh tế/giai đoạn, dựng 3 khu vực HUD trên Canvas có sẵn (góc trên:
/// tài nguyên + thanh thức tỉnh; cạnh dưới: bảng đơn vị + lưới lệnh xây), gán sprite tạm từ
/// Vector_UI_Pack_dobo_ui và nối tham chiếu các view tới manager/hệ thống qua SerializedObject.
/// Bảng minimap góc dưới phải được giữ nguyên. Chạy lại được nhiều lần (dựng lại từ đầu).
///
/// Toàn bộ sprite gán ở đây là tạm thời (placeholder) - thay trong Inspector sau mà không đụng code.
/// </summary>
public static class GameplayHudSetup
{
    private const string Pack = "Assets/Sprites/Vector_UI_Pack_dobo_ui";
    private const string BuildingDataFolder = "Assets/Data/Building";

    private const string HudRootName = "GameplayHUD";
    private const string ManagersName = "GameplayManagers";
    private const string CommanderName = "CommanderPlaceholder";

    // Bảng màu nhẹ theo mockup.
    private static readonly Color GoldColor = new Color(1f, 0.85f, 0.3f);
    private static readonly Color ManaColor = new Color(0.45f, 0.8f, 1f);
    private static readonly Color PanelTint = new Color(1f, 1f, 1f, 0.95f);

    [MenuItem("Tools/UI/Build Gameplay HUD")]
    public static void BuildHud()
    {
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameplayHudSetup] Không tìm thấy Canvas Overlay trong scene.");
            return;
        }

        GridBuildingSystem buildingSystem = Object.FindFirstObjectByType<GridBuildingSystem>();
        UnitController unitController = Object.FindFirstObjectByType<UnitController>();

        EnsureManagers(out ResourceManager resourceManager, out PhaseManager phaseManager);
        HudDisplayInfo commander = EnsureCommanderPlaceholder();

        RectTransform hudRoot = ResetHudRoot(canvas);

        BuildResourceRegion(hudRoot, resourceManager, out ResourceCounterView goldView, out ResourceCounterView manaView);
        BindResourceHud(hudRoot, resourceManager, goldView, manaView);
        BuildAwakeningRegion(hudRoot, phaseManager);
        BuildBottomPanel(hudRoot, buildingSystem, unitController, commander);
        BuildConstructStatusPanel(hudRoot, buildingSystem);

        DisableLegacyBuildingMenu();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[GameplayHudSetup] HUD đã dựng xong. Nhớ lưu scene (Ctrl+S).");
    }

    /// <summary>
    /// Cấu hình sản lượng theo cấp cho công trình khai thác (10/30/50 mỗi giây cho Lv1/2/3).
    /// Quy ước màu -> cấp: Drill Yellow=Lv1, Blue=Lv2, Purple=Lv3; Magic Purple=Lv1, Dark=Lv2, Green=Lv3.
    /// </summary>
    [MenuItem("Tools/UI/Configure Building Production")]
    public static void ConfigureBuildingProduction()
    {
        // kind: 0=Gold, 1=Mana ; amountPerCycle mỗi 1 giây.
        SetProducer("Drill_Yellow", 0, 10);
        SetProducer("Drill_Blue", 0, 30);
        SetProducer("Drill_Purple", 0, 50);
        SetProducer("MagicMachine_Purple", 1, 10);
        SetProducer("MagicMachine_Dark", 1, 30);
        SetProducer("MagicMachine_Green", 1, 50);
        AssetDatabase.SaveAssets();
        Debug.Log("[GameplayHudSetup] Đã cấu hình sản lượng công trình theo cấp (Lv1/2/3 = 10/30/50 mỗi giây).");
    }

    private static void SetProducer(string prefabName, int kind, int amountPerSecond)
    {
        string path = $"Assets/Prefabs/Placement/{prefabName}.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null)
        {
            Debug.LogWarning($"[GameplayHudSetup] Thiếu prefab {path}");
            return;
        }

        ResourceProducer producer = contents.GetComponent<ResourceProducer>() ?? contents.AddComponent<ResourceProducer>();
        SetSerialized(producer, so =>
        {
            so.FindProperty("kind").enumValueIndex = kind;
            so.FindProperty("amountPerCycle").intValue = amountPerSecond;
            so.FindProperty("cycleSeconds").floatValue = 1f;
        });

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    // ---------------------------------------------------------------- Managers / data

    private static void EnsureManagers(out ResourceManager resourceManager, out PhaseManager phaseManager)
    {
        resourceManager = Object.FindFirstObjectByType<ResourceManager>();
        phaseManager = Object.FindFirstObjectByType<PhaseManager>();

        if (resourceManager == null || phaseManager == null)
        {
            GameObject managers = GameObject.Find(ManagersName) ?? new GameObject(ManagersName);
            if (resourceManager == null)
            {
                resourceManager = managers.AddComponent<ResourceManager>();
            }
            if (phaseManager == null)
            {
                phaseManager = managers.AddComponent<PhaseManager>();
            }
        }

        // Giá trị khởi đầu theo yêu cầu: Vàng 500, Mana 0 (cả hai không giới hạn). Luôn ghi đè.
        SetSerialized(resourceManager, so =>
        {
            so.FindProperty("startGold").intValue = 500;
            so.FindProperty("startMana").intValue = 0;
        });
    }

    private static HudDisplayInfo EnsureCommanderPlaceholder()
    {
        GameObject existing = GameObject.Find(CommanderName);
        if (existing != null)
        {
            return existing.GetComponent<HudDisplayInfo>();
        }

        var go = new GameObject(CommanderName);
        UnitHealth health = go.AddComponent<UnitHealth>();
        SetSerialized(health, so =>
        {
            so.FindProperty("maxHealth").floatValue = 1000f;
            so.FindProperty("currentHealth").floatValue = 1000f;
            so.FindProperty("destroyOnDeath").boolValue = false;
        });

        HudDisplayInfo info = go.AddComponent<HudDisplayInfo>();
        SetSerialized(info, so =>
        {
            so.FindProperty("displayName").stringValue = "COMMANDER";
            so.FindProperty("attackOverride").floatValue = 75f;
            so.FindProperty("portrait").objectReferenceValue = LoadSprite("Icons/128px/sword_icon_128px.png");
        });
        return info;
    }

    // ---------------------------------------------------------------- Resource region (top-left)

    private static void BuildResourceRegion(RectTransform parent, ResourceManager rm,
        out ResourceCounterView goldView, out ResourceCounterView manaView)
    {
        RectTransform column = CreateRect("ResourceColumn", parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -20f), new Vector2(360f, 150f));
        AddVerticalLayout(column, 10f);

        goldView = CreateResourceCounter(column, "GoldCounter", "Icons/128px/coin_icon_128px.png", GoldColor);
        manaView = CreateResourceCounter(column, "ManaCounter", "Icons/128px/gem_icon_128px.png", ManaColor);
    }

    private static ResourceCounterView CreateResourceCounter(RectTransform parent, string name,
        string iconPath, Color labelColor)
    {
        RectTransform root = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, 64f));
        AddImage(root, LoadSprite("ProgressBars/progressBarBase_black.png"), PanelTint);

        Image icon = AddImage(CreateRect("Icon", root,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(34f, 0f), new Vector2(52f, 52f)), LoadSprite(iconPath), Color.white);

        TMP_Text label = CreateText("Value", root, "0", 26, TextAlignmentOptions.MidlineRight, labelColor);
        SetStretch((RectTransform)label.transform, new Vector2(70f, 0f), new Vector2(-20f, 0f));

        ResourceCounterView view = root.gameObject.AddComponent<ResourceCounterView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("valueText").objectReferenceValue = label;
        });
        return view;
    }

    private static void BindResourceHud(RectTransform hudRoot, ResourceManager rm,
        ResourceCounterView goldView, ResourceCounterView manaView)
    {
        ResourceHudBinder binder = hudRoot.gameObject.AddComponent<ResourceHudBinder>();
        SetSerialized(binder, so =>
        {
            so.FindProperty("resourceManager").objectReferenceValue = rm;
            so.FindProperty("goldView").objectReferenceValue = goldView;
            so.FindProperty("manaView").objectReferenceValue = manaView;
        });
    }

    // ---------------------------------------------------------------- Awakening bar (top-right)

    private static void BuildAwakeningRegion(RectTransform parent, PhaseManager pm)
    {
        RectTransform root = CreateRect("AwakeningBar", parent,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -24f), new Vector2(420f, 60f));
        AddImage(root, LoadSprite("ProgressBars/progressBarBase_black.png"), PanelTint);

        RectTransform fillRect = CreateRect("Fill", root, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        SetStretch(fillRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        Image fill = AddImage(fillRect, LoadSprite("ProgressBars/progressBar_purple.png"),
            new Color(0.7f, 0.4f, 1f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0.7f;

        TMP_Text label = CreateText("Percent", root, "70% COMPLETE", 22,
            TextAlignmentOptions.Center, Color.white);
        SetStretch((RectTransform)label.transform, Vector2.zero, Vector2.zero);

        AwakeningBarView view = root.gameObject.AddComponent<AwakeningBarView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("phaseManager").objectReferenceValue = pm;
            so.FindProperty("fillImage").objectReferenceValue = fill;
            so.FindProperty("percentText").objectReferenceValue = label;
        });
    }

    // ---------------------------------------------------------------- Bottom control panel

    private static void BuildBottomPanel(RectTransform parent, GridBuildingSystem buildingSystem,
        UnitController unitController, HudDisplayInfo commander)
    {
        RectTransform panel = CreateRect("ControlPanel", parent,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-120f, 16f), new Vector2(900f, 240f));
        // Nền bảng chặn raycast để click trong vùng bảng không lọt xuống bản đồ (vô tình xây).
        AddImage(panel, LoadSprite("Panels/panel_blue.png"), PanelTint).raycastTarget = true;

        BuildUnitInfoPanel(panel, unitController, commander);
        BuildActionGridArea(panel, buildingSystem);
    }

    private static void BuildUnitInfoPanel(RectTransform parent, UnitController unitController,
        HudDisplayInfo commander)
    {
        RectTransform root = CreateRect("UnitInfo", parent,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(20f, 0f), new Vector2(210f, -40f));

        RectTransform content = CreateRect("Content", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        SetStretch(content, Vector2.zero, Vector2.zero);

        RectTransform portraitFrame = CreateRect("Portrait", content,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(120f, 120f));
        AddImage(portraitFrame, LoadSprite("Item Slots/itemSlot_blue.png"), Color.white);
        Image portrait = AddImage(CreateRect("PortraitImage", portraitFrame, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), commander.Portrait, Color.white);
        SetStretch((RectTransform)portrait.transform, new Vector2(16f, 16f), new Vector2(-16f, -16f));

        TMP_Text nameText = CreateText("Name", content, "COMMANDER", 22, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)nameText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(200f, 28f));
        TMP_Text healthText = CreateText("Health", content, "HEALTH: 1000/1000", 18,
            TextAlignmentOptions.Center, new Color(0.5f, 1f, 0.5f));
        SetAnchored((RectTransform)healthText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(220f, 24f));
        TMP_Text attackText = CreateText("Attack", content, "ATTACK: 75", 18,
            TextAlignmentOptions.Center, new Color(1f, 0.7f, 0.4f));
        SetAnchored((RectTransform)attackText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(220f, 24f));

        Button collapse = CreateButton("CollapseButton", parent, LoadSprite("Arrows/arrow_blue.png"));
        SetAnchored((RectTransform)collapse.transform, new Vector2(0f, 0.5f), new Vector2(-16f, 0f), new Vector2(40f, 64f));

        UnitInfoPanelView view = root.gameObject.AddComponent<UnitInfoPanelView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("unitController").objectReferenceValue = unitController;
            so.FindProperty("commanderInfo").objectReferenceValue = commander;
            so.FindProperty("portraitImage").objectReferenceValue = portrait;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("attackText").objectReferenceValue = attackText;
            so.FindProperty("collapsibleContent").objectReferenceValue = content.gameObject;
            so.FindProperty("collapseButton").objectReferenceValue = collapse;
        });
    }

    private static void BuildActionGridArea(RectTransform parent, GridBuildingSystem buildingSystem)
    {
        RectTransform header = CreateRect("PanelTitle", parent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(60f, -6f), new Vector2(360f, 34f));
        TMP_Text title = CreateText("Title", header, "UNIT / BUILD PANEL", 20, TextAlignmentOptions.Center, Color.white);
        SetStretch((RectTransform)title.transform, Vector2.zero, Vector2.zero);

        RectTransform gridContainer = CreateRect("BuildGrid", parent,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(120f, -10f), new Vector2(-260f, 150f));
        HorizontalLayoutGroup layout = gridContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        BuildActionSlotView template = CreateBuildSlotTemplate(gridContainer);

        BuildActionGrid grid = parent.gameObject.AddComponent<BuildActionGrid>();
        PlacedObjectTypeSO[] types = LoadBuildTypes();
        SetSerialized(grid, so =>
        {
            so.FindProperty("buildingSystem").objectReferenceValue = buildingSystem;
            so.FindProperty("slotTemplate").objectReferenceValue = template;
            SerializedProperty list = so.FindProperty("types");
            list.arraySize = types.Length;
            for (int i = 0; i < types.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = types[i];
            }
        });
    }

    private static BuildActionSlotView CreateBuildSlotTemplate(RectTransform parent)
    {
        RectTransform root = CreateRect("SlotTemplate", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 140f));
        Image bg = AddImage(root, LoadSprite("Item Slots/itemSlot_cyan.png"), Color.white);
        bg.raycastTarget = true; // nền ô là vùng nhận click cho Button -> bấm chuột vào ô là xây
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        Image icon = AddImage(CreateRect("Icon", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(72f, 72f)), null, Color.white);

        TMP_Text nameText = CreateText("Name", root, "BUILD", 14, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)nameText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(116f, 22f));
        TMP_Text costText = CreateText("Cost", root, "COST: 0", 13, TextAlignmentOptions.Center,
            new Color(1f, 0.85f, 0.3f));
        SetAnchored((RectTransform)costText.transform, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(116f, 20f));
        TMP_Text hotkeyText = CreateText("Hotkey", root, "Q", 18, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)hotkeyText.transform, new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(26f, 26f));

        BuildActionSlotView slot = root.gameObject.AddComponent<BuildActionSlotView>();
        SetSerialized(slot, so =>
        {
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("hotkeyText").objectReferenceValue = hotkeyText;
            so.FindProperty("costText").objectReferenceValue = costText;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("canvasGroup").objectReferenceValue = group;
        });
        return slot;
    }

    // ---------------------------------------------------------------- Construct Status Panel

    /// <summary>Dựng riêng Cửa sổ Trạng thái Công trình mà không dựng lại toàn bộ HUD.</summary>
    [MenuItem("Tools/UI/Build Construct Status Panel")]
    public static void BuildConstructStatusPanelMenu()
    {
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
        {
            Debug.LogError("[GameplayHudSetup] Không tìm thấy Canvas Overlay trong scene.");
            return;
        }

        Transform hud = canvas.transform.Find(HudRootName);
        RectTransform parent = hud != null ? (RectTransform)hud : (RectTransform)canvas.transform;
        GridBuildingSystem buildingSystem = Object.FindFirstObjectByType<GridBuildingSystem>();

        Transform existing = parent.Find("ConstructStatusPanel");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        BuildConstructStatusPanel(parent, buildingSystem);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[GameplayHudSetup] Đã dựng Cửa sổ Trạng thái Công trình. Nhớ lưu scene (Ctrl+S).");
    }

    private static readonly Color HealthyBarColor = new Color(0.5f, 1f, 0.4f);

    private static void BuildConstructStatusPanel(RectTransform parent, GridBuildingSystem buildingSystem)
    {
        // Container luôn bật để giữ view nhận sự kiện; cửa sổ con (window) bật/tắt khi xem công trình.
        RectTransform container = CreateRect("ConstructStatusPanel", parent,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(container, Vector2.zero, Vector2.zero);

        RectTransform window = CreateRect("Window", container,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-20f, 0f), new Vector2(380f, 600f));
        AddImage(window, LoadSprite("Panels/panel_blue.png"), PanelTint).raycastTarget = true;

        TMP_Text nameText = CreateText("Name", window, "CONSTRUCT", 26, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)nameText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(340f, 36f));
        TMP_Text levelText = CreateText("Level", window, "Level 1", 18, TextAlignmentOptions.Center, GoldColor);
        SetAnchored((RectTransform)levelText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(340f, 24f));

        TMP_Text durabilityLabel = CreateText("DurabilityLabel", window, "DURABILITY (HP)", 15,
            TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)durabilityLabel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(340f, 20f));
        RectTransform barBase = CreateRect("DurabilityBar", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(340f, 26f));
        AddImage(barBase, LoadSprite("ProgressBars/progressBarBase_black.png"), PanelTint);
        RectTransform fillRect = CreateRect("Fill", barBase, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(fillRect, new Vector2(5f, 5f), new Vector2(-5f, -5f));
        Image durabilityFill = AddImage(fillRect, LoadSprite("ProgressBars/progressBar_green.png"), HealthyBarColor);
        durabilityFill.type = Image.Type.Filled;
        durabilityFill.fillMethod = Image.FillMethod.Horizontal;
        durabilityFill.fillAmount = 1f;
        TMP_Text durabilityText = CreateText("DurabilityText", window, "HP: 1000 / 1000", 14,
            TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)durabilityText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(340f, 20f));

        TMP_Text statsText = CreateText("Stats", window, string.Empty, 15, TextAlignmentOptions.TopLeft,
            new Color(0.85f, 0.95f, 1f));
        SetAnchored((RectTransform)statsText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(330f, 110f));
        statsText.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text typeText = CreateText("Type", window, "Type: --", 15, TextAlignmentOptions.Left, Color.white);
        SetAnchored((RectTransform)typeText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -284f), new Vector2(330f, 22f));
        TMP_Text descriptionText = CreateText("Description", window, string.Empty, 13, TextAlignmentOptions.TopLeft,
            new Color(0.8f, 0.8f, 0.8f));
        SetAnchored((RectTransform)descriptionText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(330f, 64f));
        descriptionText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform upgradeSection = CreateRect("UpgradeSection", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -412f), new Vector2(352f, 150f));
        AddImage(upgradeSection, LoadSprite("Panels/panel_black.png"), new Color(1f, 1f, 1f, 0.5f));
        TMP_Text upgradeBenefit = CreateText("Benefit", upgradeSection, string.Empty, 13, TextAlignmentOptions.Top,
            new Color(0.7f, 1f, 0.7f));
        SetAnchored((RectTransform)upgradeBenefit.transform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(320f, 54f));
        upgradeBenefit.textWrappingMode = TextWrappingModes.Normal;
        TMP_Text upgradeCost = CreateText("Cost", upgradeSection, "Cost: 0G 0M", 15, TextAlignmentOptions.Center, GoldColor);
        SetAnchored((RectTransform)upgradeCost.transform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(320f, 24f));
        Button upgradeButton = CreateLabeledButton("UpgradeButton", upgradeSection,
            LoadSprite("Buttons/buttonAdvanced_green.png"), "UPGRADE",
            new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(200f, 44f), out CanvasGroup upgradeGroup);

        Button demolishButton = CreateLabeledButton("DemolishButton", window,
            LoadSprite("Buttons/buttonAdvanced_red.png"), "DEMOLISH",
            new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(220f, 46f), out _);
        TMP_Text refundText = CreateText("Refund", window, string.Empty, 13, TextAlignmentOptions.Center, GoldColor);
        SetAnchored((RectTransform)refundText.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(360f, 20f));
        refundText.textWrappingMode = TextWrappingModes.Normal;

        Button closeButton = CreateLabeledButton("CloseButton", window,
            LoadSprite("Buttons/buttonCircle_red.png"), "X",
            new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(40f, 40f), out _);

        ConstructStatusPanelView view = container.gameObject.AddComponent<ConstructStatusPanelView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("buildingSystem").objectReferenceValue = buildingSystem;
            so.FindProperty("windowRoot").objectReferenceValue = window.gameObject;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("durabilityFill").objectReferenceValue = durabilityFill;
            so.FindProperty("durabilityText").objectReferenceValue = durabilityText;
            so.FindProperty("statsText").objectReferenceValue = statsText;
            so.FindProperty("typeText").objectReferenceValue = typeText;
            so.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            so.FindProperty("upgradeSection").objectReferenceValue = upgradeSection.gameObject;
            so.FindProperty("upgradeBenefitText").objectReferenceValue = upgradeBenefit;
            so.FindProperty("upgradeCostText").objectReferenceValue = upgradeCost;
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
            so.FindProperty("upgradeButtonGroup").objectReferenceValue = upgradeGroup;
            so.FindProperty("demolishButton").objectReferenceValue = demolishButton;
            so.FindProperty("refundText").objectReferenceValue = refundText;
        });

        window.gameObject.SetActive(false);
    }

    private static Button CreateLabeledButton(string name, Transform parent, Sprite sprite, string label,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, out CanvasGroup group)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), anchoredPos, size);
        Image image = AddImage(rect, sprite, Color.white);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        group = rect.gameObject.AddComponent<CanvasGroup>();

        TMP_Text text = CreateText("Label", rect, label, 18, TextAlignmentOptions.Center, Color.white);
        SetStretch((RectTransform)text.transform, Vector2.zero, Vector2.zero);
        return button;
    }

    // ---------------------------------------------------------------- Build-type assets

    private static PlacedObjectTypeSO[] LoadBuildTypes()
    {
        PlacedObjectTypeSO drill = ConfigureType("PlacedObjectType_Drill_Yellow", "Drill", "Q",
            100, 0, "Khai thác Vàng (Lv1: 10/giây).", "Icons/128px/coin_icon_128px.png");
        PlacedObjectTypeSO forge = ConfigureType("PlacedObjectType_MagicMachine_Purple", "Magic Forge", "W",
            100, 0, "Khai thác & tinh luyện Mana (Lv1: 10/giây).", "Icons/128px/gem_icon_128px.png");
        PlacedObjectTypeSO barracks = EnsureBarracksPlaceholder();
        return new[] { drill, forge, barracks };
    }

    private static PlacedObjectTypeSO ConfigureType(string assetName, string display, string hotkey,
        int goldCost, int manaCost, string description, string iconPath)
    {
        string path = $"{BuildingDataFolder}/{assetName}.asset";
        var type = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
        if (type == null)
        {
            Debug.LogWarning($"[GameplayHudSetup] Thiếu asset {path}");
            return null;
        }

        type.nameString = display;
        type.hotkey = hotkey;
        type.goldCost = goldCost;
        type.manaCost = manaCost;
        type.description = description;
        if (type.icon == null)
        {
            type.icon = LoadSprite(iconPath);
        }
        EditorUtility.SetDirty(type);
        return type;
    }

    private static PlacedObjectTypeSO EnsureBarracksPlaceholder()
    {
        string path = $"{BuildingDataFolder}/PlacedObjectType_Barracks.asset";
        var type = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
        if (type == null)
        {
            type = ScriptableObject.CreateInstance<PlacedObjectTypeSO>();
            AssetDatabase.CreateAsset(type, path);
        }

        type.nameString = "Barracks";
        type.hotkey = "E";
        type.goldCost = 100;
        type.manaCost = 0;
        type.description = "Huấn luyện lính (chưa thêm asset).";
        type.prefab = null; // placeholder -> ô bị làm mờ trong lưới
        if (type.icon == null)
        {
            type.icon = LoadSprite("Icons/128px/sword_icon_128px.png");
        }
        EditorUtility.SetDirty(type);
        return type;
    }

    // ---------------------------------------------------------------- Helpers

    private static Canvas FindOverlayCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.transform.parent == null)
            {
                return c;
            }
        }
        return Object.FindFirstObjectByType<Canvas>();
    }

    private static RectTransform ResetHudRoot(Canvas canvas)
    {
        Transform existing = canvas.transform.Find(HudRootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        RectTransform root = CreateRect(HudRootName, (RectTransform)canvas.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        SetStretch(root, Vector2.zero, Vector2.zero);
        return root;
    }

    private static void DisableLegacyBuildingMenu()
    {
        BuildingMenuUI legacy = Object.FindFirstObjectByType<BuildingMenuUI>();
        if (legacy != null)
        {
            legacy.gameObject.SetActive(false);
        }
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        Image image = rect.gameObject.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f));
        Image image = AddImage(rect, sprite, Color.white);
        image.raycastTarget = true;
        return rect.gameObject.AddComponent<Button>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 40f));
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void AddVerticalLayout(RectTransform rect, float spacing)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private static Sprite LoadSprite(string relativePath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{Pack}/{relativePath}");
    }

    private static void SetSerialized(Object target, System.Action<SerializedObject> apply)
    {
        var so = new SerializedObject(target);
        apply(so);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
