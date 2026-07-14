using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dựng HUD cho scene Phase 2:
///   - Thanh kỹ năng 4 ô (icon phép, phím 1-4, lớp phủ hồi chiêu quét tròn, mana) giữa-dưới.
///   - Bảng trạng thái người chơi góc trên-trái: khung chân dung (character_panel) với 3 vạch
///     máu/mana/khiên vẽ đè lên đúng rãnh của sprite; máu tụt/đầy chậm qua Bar (như thanh máu
///     trên đầu unit prefab, UnitHealth đẩy thẳng vào), số nằm giữa thanh, khiên để trống tới
///     khi có phép (tự thêm UnitHealth cho Player_1).
///   - Canvas + EventSystem (InputSystemUIInputModule - project dùng new Input System).
///
///   Menu: Tools > Dungeon > Setup Phase 2 HUD
///
/// Scene Phase 2 mở ADDITIVE nếu chưa mở (không đụng scene đang làm việc), lưu rồi
/// đóng lại nếu do tool mở. Chạy lại an toàn: canvas cũ cùng tên bị thay mới.
/// </summary>
public static class PlayerHudSetup
{
    private const string Phase2ScenePath = "Assets/Scenes/Phase 2.unity";
    private const string CanvasName = "Phase2HudCanvas";
    private const string UiPackFolder = "Assets/Sprites/Vector_UI_Pack_dobo_ui";

    private const float PlayerMaxHealth = 100f;
    private const int PlayerStartMana = 50;
    private const float PlayerManaRegenPerSecond = 8f;
    private const float DisplayMaxMana = 100f;

    private const float SlotSize = 72f;
    private const float SlotSpacing = 8f;
    private const float BarPadding = 12f;
    private const float SkillBarBottomMargin = 24f;
    private const float SlotIconSize = 56f;
    private const float CooldownOverlayAlpha = 0.6f;

    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    private static readonly Vector2 StatusPanelOffset = new Vector2(16f, -16f);

    // Khung chân dung TRỐNG character_panel_2 (84x30 px gốc, không có vạch vẽ sẵn) phóng to;
    // fill là các strip sprite cùng sheet, đặt theo Rect PIXEL SPRITE GỐC quy ra anchor cho
    // đúng rãnh trống trên khung. Không dùng khung có vạch vẽ sẵn - vạch tĩnh nằm sau fill
    // sẽ làm thanh trông như không bao giờ tụt.
    private const string CharacterPanelPath = "Assets/Sprites/Free-Basic-Pixel-Art-UI-for-RPG/character_panel.png";
    private const string CharacterPanelFrameSprite = "character_panel_2";
    private const string HealthStripSprite = "character_panel_10";
    private const string ManaStripSprite = "character_panel_11";
    private const string ShieldStripSprite = "character_panel_12";
    private const float FrameScale = 4f;
    private static readonly Vector2 FrameNativeSize = new Vector2(84f, 30f);
    private static readonly Rect HealthStripPixels = new Rect(28f, 20f, 52f, 2f);
    private static readonly Rect ManaStripPixels = new Rect(30f, 15f, 42f, 2f);
    private static readonly Rect ShieldStripPixels = new Rect(29f, 10f, 38f, 2f);
    // Ghost = chính strip đỏ tint xám tối -> vệt đỏ sẫm "máu vừa mất" tụt chậm sau fill.
    private static readonly Color HealthGhostTint = new Color(0.45f, 0.45f, 0.45f);
    private const float HealthLabelFontSize = 15f;
    private const float ManaLabelFontSize = 12f;
    private const float HealthBarAnimationSpeed = 5f;

    [MenuItem("Tools/Dungeon/Setup Phase 2 HUD")]
    public static void SetupPhaseTwoHud()
    {
        (Scene scene, bool openedByTool) = EnsurePhaseTwoLoaded();
        if (!scene.IsValid())
        {
            Debug.LogError($"[PlayerHudSetup] Không mở được scene: {Phase2ScenePath}");
            return;
        }

        try
        {
            GameObject avatar = FindPlayerAvatarIn(scene);
            if (avatar == null)
            {
                Debug.LogError("[PlayerHudSetup] Không tìm thấy avatar có PlayerControll trong Phase 2.");
                return;
            }

            UnitHealth playerHealth = EnsureUnitHealth(avatar);
            ResourceManager resourceManager = EnsureResourceManager(scene);
            EnsurePlayerManaRegen(avatar);
            EnsureEventSystem(scene);
            RectTransform canvasRoot = EnsureHudCanvas(scene);
            BuildSkillBar(canvasRoot, avatar.GetComponent<PlayerSkillCaster>());
            BuildPlayerStatus(canvasRoot, playerHealth, resourceManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerHudSetup] Hoàn tất: HUD Phase 2 (thanh 4 skill + máu/khiên/mana) đã dựng và lưu scene.");
        }
        finally
        {
            if (openedByTool)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    // ---------- Scene & thành phần nền ----------

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

        return (EditorSceneManager.OpenScene(Phase2ScenePath, OpenSceneMode.Additive), true);
    }

    private static GameObject FindPlayerAvatarIn(Scene scene)
    {
        foreach (PlayerControll controller in UnityEngine.Object.FindObjectsByType<PlayerControll>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (controller.gameObject.scene == scene)
            {
                return controller.gameObject;
            }
        }

        return null;
    }

    private static UnitHealth EnsureUnitHealth(GameObject avatar)
    {
        var health = avatar.GetComponent<UnitHealth>();
        if (health == null)
        {
            health = avatar.AddComponent<UnitHealth>();
        }

        SetSerialized(health, so =>
        {
            so.FindProperty("baseMaxHealth").floatValue = PlayerMaxHealth;
            so.FindProperty("currentHealth").floatValue = PlayerMaxHealth;
        });
        return health;
    }

    /// <summary>
    /// Phase 2 không có công trình khai thác Mana nào -> tạo ResourceManager riêng cho scene
    /// với Mana khởi đầu đủ dùng ngay, hồi dần qua PlayerManaRegen.
    /// </summary>
    private static ResourceManager EnsureResourceManager(Scene scene)
    {
        ResourceManager existing = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            existing = root.GetComponentInChildren<ResourceManager>(true);
            if (existing != null)
            {
                break;
            }
        }

        if (existing == null)
        {
            var go = new GameObject("ResourceManager");
            SceneManager.MoveGameObjectToScene(go, scene);
            existing = go.AddComponent<ResourceManager>();
        }

        SetSerialized(existing, so => so.FindProperty("startMana").intValue = PlayerStartMana);
        return existing;
    }

    private static void EnsurePlayerManaRegen(GameObject avatar)
    {
        var regen = avatar.GetComponent<PlayerManaRegen>();
        if (regen == null)
        {
            regen = avatar.AddComponent<PlayerManaRegen>();
        }

        SetSerialized(regen, so => so.FindProperty("regenPerSecond").floatValue = PlayerManaRegenPerSecond);
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<EventSystem>(true) != null)
            {
                return;
            }
        }

        var eventSystemGo = new GameObject("EventSystem");
        SceneManager.MoveGameObjectToScene(eventSystemGo, scene);
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();
    }

    private static RectTransform EnsureHudCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == CanvasName)
            {
                UnityEngine.Object.DestroyImmediate(root);
                break;
            }
        }

        var canvasGo = new GameObject(CanvasName);
        SceneManager.MoveGameObjectToScene(canvasGo, scene);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        return (RectTransform)canvasGo.transform;
    }

    // ---------- Thanh kỹ năng ----------

    private static void BuildSkillBar(RectTransform canvasRoot, PlayerSkillCaster caster)
    {
        int slotCount = PlayerSkillCaster.SlotCount;
        float barWidth = slotCount * SlotSize + (slotCount - 1) * SlotSpacing + 2f * BarPadding;
        float barHeight = SlotSize + 2f * BarPadding;

        RectTransform bar = CreateRect("SkillBar", canvasRoot,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, SkillBarBottomMargin), new Vector2(barWidth, barHeight));
        AddImage(bar, LoadUiSprite("Panels/panel_black.png"), new Color(1f, 1f, 1f, 0.85f));

        var slotViews = new SkillSlotView[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            SkillDefinitionSO skill = caster != null ? caster.GetSlotSkill(i) : null;
            slotViews[i] = BuildSkillSlot(bar, i, skill);
        }

        var barView = bar.gameObject.AddComponent<SkillBarView>();
        SetSerialized(barView, so =>
        {
            so.FindProperty("caster").objectReferenceValue = caster;
            SerializedProperty slots = so.FindProperty("slots");
            slots.arraySize = slotCount;
            for (int i = 0; i < slotCount; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slotViews[i];
            }
        });
    }

    private static SkillSlotView BuildSkillSlot(RectTransform bar, int slotIndex, SkillDefinitionSO skill)
    {
        float slotX = BarPadding + slotIndex * (SlotSize + SlotSpacing) + SlotSize * 0.5f;
        RectTransform slot = CreateRect($"Slot{slotIndex + 1}", bar,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(slotX, 0f), new Vector2(SlotSize, SlotSize));
        AddImage(slot, LoadUiSprite("Item Slots/itemSlot_cyan.png"), Color.white);

        RectTransform icon = CreateRect("Icon", slot,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(SlotIconSize, SlotIconSize));
        Image iconImage = AddImage(icon, skill != null ? skill.Icon : null, Color.white);
        iconImage.preserveAspect = true;
        iconImage.enabled = skill != null && skill.Icon != null;

        Image cooldownOverlay = BuildCooldownOverlay(slot);
        TMP_Text keyLabel = CreateText("Key", slot, (slotIndex + 1).ToString(), 14f, TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(6f, -4f), new Vector2(24f, 18f));
        TMP_Text manaLabel = CreateText("Mana", slot,
            skill != null && skill.ManaCost > 0 ? skill.ManaCost.ToString() : string.Empty,
            13f, TextAlignmentOptions.Bottom,
            new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(SlotSize, 16f));
        manaLabel.color = new Color(0.45f, 0.8f, 1f);

        var view = slot.gameObject.AddComponent<SkillSlotView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("cooldownOverlay").objectReferenceValue = cooldownOverlay;
            so.FindProperty("keyLabel").objectReferenceValue = keyLabel;
            so.FindProperty("manaLabel").objectReferenceValue = manaLabel;
        });
        return view;
    }

    private static Image BuildCooldownOverlay(RectTransform slot)
    {
        RectTransform overlay = CreateRect("Cooldown", slot,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(SlotSize, SlotSize));
        Image image = AddImage(overlay, LoadUiSprite("Panels/panel_black.png"), new Color(0f, 0f, 0f, CooldownOverlayAlpha));

        // AddImage tự đặt Sliced cho sprite có border -> ép lại Filled cho hiệu ứng quét tròn.
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = true;
        image.fillAmount = 0f;
        return image;
    }

    // ---------- Bảng trạng thái: khung chân dung + máu/mana/khiên ----------

    private static void BuildPlayerStatus(RectTransform canvasRoot, UnitHealth playerHealth, ResourceManager resourceManager)
    {
        RectTransform frame = CreateRect("PlayerStatus", canvasRoot,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            StatusPanelOffset, FrameNativeSize * FrameScale);
        AddImage(frame, LoadCharacterPanelSprite(CharacterPanelFrameSprite), Color.white);

        // Ghost nằm dưới fill đỏ: khi mất máu fill tụt ngay còn ghost tụt chậm (idiom Bar).
        Image healthGhostFill = BuildStripFill(frame, "HealthFillDelayed", HealthStripPixels, HealthStripSprite, HealthGhostTint);
        Image healthFill = BuildStripFill(frame, "HealthFill", HealthStripPixels, HealthStripSprite, Color.white);
        Image manaFill = BuildStripFill(frame, "ManaFill", ManaStripPixels, ManaStripSprite, Color.white);
        Image shieldFill = BuildStripFill(frame, "ShieldFill", ShieldStripPixels, ShieldStripSprite, Color.white);
        shieldFill.fillAmount = 0f; // khiên để trống, chỉ đầy lên khi có phép

        TMP_Text healthLabel = CreateStripLabel(frame, "HealthLabel",
            $"{PlayerMaxHealth:0}/{PlayerMaxHealth:0}", HealthLabelFontSize, HealthStripPixels);
        TMP_Text manaLabel = CreateStripLabel(frame, "ManaLabel",
            PlayerStartMana.ToString(), ManaLabelFontSize, ManaStripPixels);

        Bar healthBar = frame.gameObject.AddComponent<Bar>();
        SetSerialized(healthBar, so =>
        {
            so.FindProperty("<MaxValue>k__BackingField").intValue = Mathf.RoundToInt(PlayerMaxHealth);
            so.FindProperty("<Value>k__BackingField").intValue = Mathf.RoundToInt(PlayerMaxHealth);
            so.FindProperty("_topBarImage").objectReferenceValue = healthFill;
            so.FindProperty("_middleBarImage").objectReferenceValue = healthGhostFill;
            so.FindProperty("_animationspeed").floatValue = HealthBarAnimationSpeed;
        });

        // UnitHealth đẩy máu thẳng vào Bar y như thanh máu trên đầu unit prefab.
        SetSerialized(playerHealth, so => so.FindProperty("healthBar").objectReferenceValue = healthBar);

        var view = frame.gameObject.AddComponent<PlayerStatusHudView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("shieldFill").objectReferenceValue = shieldFill;
            so.FindProperty("healthLabel").objectReferenceValue = healthLabel;
        });

        var manaView = frame.gameObject.AddComponent<ManaBarView>();
        SetSerialized(manaView, so =>
        {
            so.FindProperty("displayMaxMana").floatValue = DisplayMaxMana;
            so.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            so.FindProperty("manaFill").objectReferenceValue = manaFill;
            so.FindProperty("manaLabel").objectReferenceValue = manaLabel;
        });
    }

    private static Image BuildStripFill(RectTransform frame, string name, Rect stripPixels,
        string stripSpriteName, Color tint)
    {
        RectTransform rect = CreateStripRect(name, frame, stripPixels);
        var fillImage = rect.gameObject.AddComponent<Image>();
        fillImage.sprite = LoadCharacterPanelSprite(stripSpriteName);
        fillImage.color = tint;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
    }

    private static TMP_Text CreateStripLabel(RectTransform frame, string name, string content,
        float fontSize, Rect stripPixels)
    {
        RectTransform rect = CreateStripRect(name, frame, stripPixels);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateStripRect(string name, RectTransform frame, Rect stripPixels)
    {
        var anchorMin = new Vector2(stripPixels.xMin / FrameNativeSize.x, stripPixels.yMin / FrameNativeSize.y);
        var anchorMax = new Vector2(stripPixels.xMax / FrameNativeSize.x, stripPixels.yMax / FrameNativeSize.y);
        return CreateRect(name, frame, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private static Sprite LoadCharacterPanelSprite(string spriteName)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(CharacterPanelPath))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        Debug.LogWarning($"[PlayerHudSetup] Không tìm thấy sprite {spriteName} trong {CharacterPanelPath}");
        return null;
    }

    // ---------- Helpers (theo shape của GameplayHudSetup, chép riêng để không sửa file gốc) ----------

    private static RectTransform CreateRect(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, string content, float fontSize,
        TextAlignmentOptions alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, anchor, anchoredPosition, sizeDelta);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite LoadUiSprite(string relativePath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiPackFolder}/{relativePath}");
        if (sprite == null)
        {
            Debug.LogWarning($"[PlayerHudSetup] Không load được sprite UI: {UiPackFolder}/{relativePath}");
        }

        return sprite;
    }

    private static void SetSerialized(UnityEngine.Object target, Action<SerializedObject> apply)
    {
        var serialized = new SerializedObject(target);
        apply(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
