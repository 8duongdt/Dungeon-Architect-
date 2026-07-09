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
///   - Thanh máu + khiên người chơi góc trên-trái (tự thêm UnitHealth cho Player_1).
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
    private static readonly Vector2 HealthBarSize = new Vector2(320f, 28f);
    private static readonly Vector2 ShieldBarSize = new Vector2(320f, 14f);
    private static readonly Vector2 ManaBarSize = new Vector2(320f, 14f);
    private const float BarFillInset = 4f;
    private const float StatusBarGap = 6f;

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

    // ---------- Thanh máu + khiên ----------

    private static void BuildPlayerStatus(RectTransform canvasRoot, UnitHealth playerHealth, ResourceManager resourceManager)
    {
        float panelHeight = HealthBarSize.y + StatusBarGap + ShieldBarSize.y + StatusBarGap + ManaBarSize.y;
        RectTransform panel = CreateRect("PlayerStatus", canvasRoot,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            StatusPanelOffset, new Vector2(HealthBarSize.x, panelHeight));

        Image healthFill = BuildFilledBar(panel, "HealthBar", Vector2.zero, HealthBarSize, "ProgressBars/progressBar_red.png");
        float shieldY = -(HealthBarSize.y + StatusBarGap);
        Image shieldFill = BuildFilledBar(panel, "ShieldBar", new Vector2(0f, shieldY), ShieldBarSize, "ProgressBars/progressBar_cyan.png");
        float manaY = shieldY - (ShieldBarSize.y + StatusBarGap);
        Image manaFill = BuildFilledBar(panel, "ManaBar", new Vector2(0f, manaY), ManaBarSize, "ProgressBars/progressBar_purple.png");

        TMP_Text healthLabel = CreateText("HealthLabel", (RectTransform)healthFill.transform.parent,
            $"{PlayerMaxHealth:0}/{PlayerMaxHealth:0}", 16f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, HealthBarSize);
        TMP_Text manaLabel = CreateText("ManaLabel", (RectTransform)manaFill.transform.parent,
            PlayerStartMana.ToString(), 12f, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, ManaBarSize);

        var view = panel.gameObject.AddComponent<PlayerStatusHudView>();
        SetSerialized(view, so =>
        {
            so.FindProperty("playerHealth").objectReferenceValue = playerHealth;
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("shieldFill").objectReferenceValue = shieldFill;
            so.FindProperty("healthLabel").objectReferenceValue = healthLabel;
        });

        var manaView = panel.gameObject.AddComponent<ManaBarView>();
        SetSerialized(manaView, so =>
        {
            so.FindProperty("displayMaxMana").floatValue = DisplayMaxMana;
            so.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            so.FindProperty("manaFill").objectReferenceValue = manaFill;
            so.FindProperty("manaLabel").objectReferenceValue = manaLabel;
        });
    }

    private static Image BuildFilledBar(RectTransform parent, string name, Vector2 position, Vector2 size, string fillSpriteFile)
    {
        RectTransform background = CreateRect(name, parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        AddImage(background, LoadUiSprite("ProgressBars/progressBarBase_black.png"), Color.white);

        RectTransform fill = CreateRect("Fill", background,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-2f * BarFillInset, -2f * BarFillInset));
        Image fillImage = AddImage(fill, LoadUiSprite(fillSpriteFile), Color.white);

        // Filled trên sprite 9-slice render như Simple bị crop ngang - đủ tốt cho HUD,
        // đổi lại chỉ cần 1 tham chiếu Image đúng idiom fillAmount của repo.
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        return fillImage;
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
