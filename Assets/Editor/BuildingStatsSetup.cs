using UnityEditor;
using UnityEngine;

/// <summary>
/// Gắn và cấu hình BuildingDurability + BuildingUpgrade cho mọi prefab công trình trong
/// Assets/Prefabs/Placement (máy đào, lò mana, trại huấn luyện). Bảng nâng cấp 3 cấp:
///   - Độ bền: 1000 / 1500 / 2000.
///   - Máy khai thác: sản lượng x1 / x2 / x3 (chi phí 0 / 200 / 400 Vàng).
///   - Trại huấn luyện: chu kỳ x1 / x0.7 / x0.5 và +0 / +2 / +4 lính (chi phí Vàng + Mana).
/// Chạy lại nhiều lần được (ghi đè cấu hình).
/// </summary>
public static class BuildingStatsSetup
{
    private const string PrefabFolder = "Assets/Prefabs/Placement";
    private const float BaseDurability = 1000f;

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
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        if (contents == null)
        {
            Debug.LogWarning($"[BuildingStatsSetup] Thiếu prefab {path}");
            return false;
        }

        ConfigureDurability(contents);
        ConfigureUpgrade(contents, levels);

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
}
