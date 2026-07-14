using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn và cấu hình BuildingDurability + BuildingUpgrade cho mọi prefab công trình trong
/// Assets/Prefabs/Placement (máy đào, lò mana, trại huấn luyện). Bảng nâng cấp 3 cấp:
///   - Độ bền: 1000 / 1500 / 2000.
///   - Máy khai thác: sản lượng x1 / x2 / x3 (chi phí 0 / 200 / 400 Vàng).
///   - Trại huấn luyện: chu kỳ x1 / x0.7 / x0.5 và +0 / +2 / +4 lính (chi phí Vàng + Mana).
/// Đồng thời gắn khả năng NHẬN SÁT THƯƠNG từ quái: BoxCollider2D dạng trigger (để unit đi xuyên,
/// không kẹt vì hệ di chuyển không có pathfinding), UnitFaction phe Player, UnitHealth làm nguồn
/// máu chiến đấu duy nhất (khớp trần độ bền hiện tại), BuildingCombatDeath giải phóng ô lưới khi
/// công trình bị phá sập, và một thanh máu xanh lá world-space phía trên công trình.
/// Chạy lại nhiều lần được (ghi đè cấu hình).
/// </summary>
public static class BuildingStatsSetup
{
    private const string PrefabFolder = "Assets/Prefabs/Placement";
    private const float BaseDurability = 1000f;

    private const string HealthBarChildName = "BuildingHealthBar";
    private const float HealthBarWorldWidth = 1.6f;
    private const float HealthBarWorldHeight = 0.22f;
    private const float HealthBarClearance = 0.3f;
    private static readonly Color HealthBarBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
    // Khớp màu thanh máu xanh lá đã dùng cho unit (Slime.prefab MiddleBar) - nhất quán toàn game.
    private static readonly Color HealthBarFillColor = new Color(0.4862745f, 0.9882354f, 0f, 1f);

    private static readonly string[] ProducerPrefabs =
    {
        "Drill_Yellow", "Drill_Blue", "Drill_Purple",
        "MagicMachine_Purple", "MagicMachine_Dark", "MagicMachine_Green",
    };

    private static readonly string[] BarracksPrefabs =
    {
        "Crypt_of_Ruin", "Hellforge_Barracks", "Dark_Zenith_Altar",
    };

    private struct LevelData
    {
        public int Gold;
        public int Mana;
        public float MaxDurability;
        public float ProductionMultiplier;
        public float TrainIntervalMultiplier;
        public int MaxUnitsBonus;
        public string Benefit;
    }

    [MenuItem("Tools/Dungeon/Configure Building Stats")]
    public static void ConfigureAll()
    {
        int done = 0;
        foreach (string name in ProducerPrefabs)
        {
            done += Configure(name, ProducerLevels()) ? 1 : 0;
        }
        foreach (string name in BarracksPrefabs)
        {
            done += Configure(name, BarracksLevels()) ? 1 : 0;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BuildingStatsSetup] Đã cấu hình độ bền + nâng cấp cho {done} công trình.");
    }

    private static LevelData[] ProducerLevels()
    {
        return new[]
        {
            new LevelData { Gold = 0, Mana = 0, MaxDurability = 1000f, ProductionMultiplier = 1f, TrainIntervalMultiplier = 1f, Benefit = "Cấp gốc." },
            new LevelData { Gold = 200, Mana = 0, MaxDurability = 1500f, ProductionMultiplier = 2f, TrainIntervalMultiplier = 1f, Benefit = "Level 2: HP 1500, sản lượng x2." },
            new LevelData { Gold = 400, Mana = 0, MaxDurability = 2000f, ProductionMultiplier = 3f, TrainIntervalMultiplier = 1f, Benefit = "Level 3: HP 2000, sản lượng x3." },
        };
    }

    private static LevelData[] BarracksLevels()
    {
        return new[]
        {
            new LevelData { Gold = 0, Mana = 0, MaxDurability = 1000f, ProductionMultiplier = 1f, TrainIntervalMultiplier = 1f, MaxUnitsBonus = 0, Benefit = "Cấp gốc." },
            new LevelData { Gold = 200, Mana = 50, MaxDurability = 1500f, ProductionMultiplier = 1f, TrainIntervalMultiplier = 0.7f, MaxUnitsBonus = 2, Benefit = "Level 2: HP 1500, huấn luyện nhanh hơn, +2 lính." },
            new LevelData { Gold = 400, Mana = 150, MaxDurability = 2000f, ProductionMultiplier = 1f, TrainIntervalMultiplier = 0.5f, MaxUnitsBonus = 4, Benefit = "Level 3: HP 2000, huấn luyện rất nhanh, +4 lính." },
        };
    }

    private static bool Configure(string prefabName, LevelData[] levels)
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";
        // LoadPrefabContents ném ArgumentException (không trả null) nếu path không tồn tại -
        // kiểm tra trước để bỏ qua êm những prefab chưa được tạo, thay vì làm hỏng cả lượt chạy.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            Debug.LogWarning($"[BuildingStatsSetup] Thiếu prefab {path}");
            return false;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null)
        {
            Debug.LogWarning($"[BuildingStatsSetup] Thiếu prefab {path}");
            return false;
        }

        ConfigureDurability(contents);
        ConfigureUpgrade(contents, levels);
        ConfigureCombat(contents);

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        return true;
    }

    private static void ConfigureDurability(GameObject contents)
    {
        BuildingDurability durability = contents.GetComponent<BuildingDurability>()
            ?? contents.AddComponent<BuildingDurability>();
        var so = new SerializedObject(durability);
        so.FindProperty("maxDurability").floatValue = BaseDurability;
        so.FindProperty("currentDurability").floatValue = BaseDurability;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureUpgrade(GameObject contents, LevelData[] levels)
    {
        BuildingUpgrade upgrade = contents.GetComponent<BuildingUpgrade>()
            ?? contents.AddComponent<BuildingUpgrade>();
        var so = new SerializedObject(upgrade);

        SerializedProperty levelsProp = so.FindProperty("levels");
        levelsProp.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
        {
            LevelData data = levels[i];
            SerializedProperty element = levelsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("goldCost").intValue = data.Gold;
            element.FindPropertyRelative("manaCost").intValue = data.Mana;
            element.FindPropertyRelative("maxDurability").floatValue = data.MaxDurability;
            element.FindPropertyRelative("productionMultiplier").floatValue = data.ProductionMultiplier;
            element.FindPropertyRelative("trainIntervalMultiplier").floatValue = data.TrainIntervalMultiplier;
            element.FindPropertyRelative("maxUnitsBonus").intValue = data.MaxUnitsBonus;
            element.FindPropertyRelative("benefitDescription").stringValue = data.Benefit;
        }
        so.FindProperty("currentLevel").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- Combat (nhận sát thương)

    private static void ConfigureCombat(GameObject contents)
    {
        ConfigureCollider(contents);
        ConfigureFaction(contents);
        UnitHealth health = ConfigureHealth(contents);
        EnsureCombatDeathBridge(contents);
        ConfigureHealthBar(contents, health);
    }

    // Trigger để unit đi xuyên qua (hệ di chuyển không có pathfinding, solid collider sẽ làm quân kẹt).
    // Đi qua SerializedObject thay vì set thuộc tính native trực tiếp - AddComponent<BoxCollider2D>
    // trong ngữ cảnh LoadPrefabContents (prefab stage ẩn) chưa hoàn tất khởi tạo native ngay lập tức,
    // gọi thẳng collider.isTrigger = true có thể ném MissingComponentException.
    private static void ConfigureCollider(GameObject contents)
    {
        BoxCollider2D collider = contents.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = contents.AddComponent<BoxCollider2D>();
        }

        var so = new SerializedObject(collider);
        so.FindProperty("m_IsTrigger").boolValue = true;

        SpriteRenderer spriteRenderer = contents.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.sprite.bounds;
            so.FindProperty("m_Size").vector2Value = bounds.size;
            so.FindProperty("m_Offset").vector2Value = bounds.center;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureFaction(GameObject contents)
    {
        UnitFaction faction = contents.GetComponent<UnitFaction>()
            ?? contents.AddComponent<UnitFaction>();
        var so = new SerializedObject(faction);
        so.FindProperty("faction").enumValueIndex = (int)FactionType.Player;
        so.FindProperty("canBeTargeted").boolValue = true;
        // Công trình không va chạm vật lý với ai (collider chỉ để phát hiện/nhận sát thương)
        // nên không cần cơ chế bỏ va chạm giữa đồng minh.
        so.FindProperty("ignoreCollisionWithAllies").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // UnitHealth trở thành nguồn máu chiến đấu DUY NHẤT (quái chỉ gây sát thương lên UnitHealth) -
    // khớp trần với BuildingDurability lúc khởi tạo; hai hệ số không đồng bộ tiếp sau đó (ngoài phạm vi).
    private static UnitHealth ConfigureHealth(GameObject contents)
    {
        UnitHealth health = contents.GetComponent<UnitHealth>()
            ?? contents.AddComponent<UnitHealth>();
        var so = new SerializedObject(health);
        so.FindProperty("baseMaxHealth").floatValue = BaseDurability;
        so.FindProperty("currentHealth").floatValue = BaseDurability;
        so.FindProperty("defense").floatValue = 0f;
        so.FindProperty("destroyOnDeath").boolValue = true;
        so.FindProperty("deathDestroyDelay").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return health;
    }

    private static void EnsureCombatDeathBridge(GameObject contents)
    {
        if (contents.GetComponent<BuildingCombatDeath>() == null)
        {
            contents.AddComponent<BuildingCombatDeath>();
        }
    }

    // Thanh máu world-space phía trên công trình - dựng lại từ đầu mỗi lần chạy để không lệch
    // khi trần HP đổi. Cả top lẫn middle bar trỏ về CÙNG MỘT Image xanh lá (không cần hiệu ứng rút
    // máu 2 lớp như unit, chỉ cần một thanh fill rõ ràng).
    private static void ConfigureHealthBar(GameObject contents, UnitHealth health)
    {
        Transform existingBar = contents.transform.Find(HealthBarChildName);
        if (existingBar != null)
        {
            Object.DestroyImmediate(existingBar.gameObject);
        }

        var barGo = new GameObject(HealthBarChildName, typeof(RectTransform));
        barGo.transform.SetParent(contents.transform, false);
        barGo.transform.localPosition = new Vector3(0f, GetSpriteTopExtent(contents) + HealthBarClearance, 0f);

        var canvas = barGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        barGo.AddComponent<CanvasScaler>();
        barGo.AddComponent<GraphicRaycaster>();

        const float PixelWidth = 160f;
        const float PixelHeight = 22f;
        var barRect = (RectTransform)barGo.transform;
        barRect.sizeDelta = new Vector2(PixelWidth, PixelHeight);
        barRect.localScale = new Vector3(HealthBarWorldWidth / PixelWidth, HealthBarWorldHeight / PixelHeight, 1f);

        CreateBarImage(barRect, "Background", HealthBarBackgroundColor, filled: false);
        Image fill = CreateBarImage(barRect, "Fill", HealthBarFillColor, filled: true);

        Bar bar = barGo.AddComponent<Bar>();
        var barSo = new SerializedObject(bar);
        barSo.FindProperty("_topBarImage").objectReferenceValue = fill;
        barSo.FindProperty("_middleBarImage").objectReferenceValue = fill;
        barSo.ApplyModifiedPropertiesWithoutUndo();

        var healthSo = new SerializedObject(health);
        healthSo.FindProperty("healthBar").objectReferenceValue = bar;
        healthSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static float GetSpriteTopExtent(GameObject contents)
    {
        SpriteRenderer spriteRenderer = contents.GetComponent<SpriteRenderer>();
        return spriteRenderer != null && spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.extents.y
            : 1f;
    }

    private static Image CreateBarImage(RectTransform parent, string name, Color color, bool filled)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = color;
        if (filled)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
        }

        return image;
    }
}
