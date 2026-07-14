using UnityEditor;
using UnityEngine;

/// <summary>
/// Dựng asset cây công trình (Assets/Resources/BuildingUpgradeTree.asset) từ bảng thiết kế 2 nhánh:
///   Kinh tế:            GoldOutput T1 | ManaOutput T1 -> CycleTime T2
///   Hỗ trợ chiến đấu:   Phase2Damage T1 | Phase2Shield T1 -> Phase2Regen T2
///
///   Menu: Tools > Dungeon > Setup Building Tree Asset
///
/// Đặt trong Resources để BuildingProgressionEffects (static) Resources.Load được lúc runtime.
/// Lợi ích tuyến tính mỗi bậc; giá nâng theo UpgradeCostFormula (lũy tiến). Chạy lại an toàn.
/// </summary>
public static class BuildingTreeAssetSetup
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string TreeAssetPath = "Assets/Resources/BuildingUpgradeTree.asset";

    // (id, tên, mô tả, nhánh, bậc hiển thị, bậc tối đa, hiệu ứng, lợi ích mỗi bậc)
    private static readonly (string id, string displayName, string description, BuildingBranch branch,
        int tier, int maxLevel, BuildingUpgradeEffect effect, float valuePerLevel)[] NodeTable =
    {
        ("GoldOutput", "Efficient Gold Mine",
            "Each level increases Gold output of all mines by 20%.",
            BuildingBranch.Economy, 1, 5, BuildingUpgradeEffect.GoldOutput, 0.2f),
        ("ManaOutput", "Bountiful Mana Well",
            "Each level increases Mana output of all wells by 20%.",
            BuildingBranch.Economy, 1, 5, BuildingUpgradeEffect.ManaOutput, 0.2f),
        ("CycleTime", "Accelerated Production",
            "Each level reduces production cycle time by 10% (never exceeds 50%).",
            BuildingBranch.Economy, 2, 3, BuildingUpgradeEffect.CycleTimeReduction, 0.1f),
        ("Phase2Damage", "Empowered Arsenal",
            "Each level increases the main character's skill damage by 10% in Phase 2.",
            BuildingBranch.CombatSupport, 1, 5, BuildingUpgradeEffect.Phase2Damage, 0.1f),
        ("Phase2Shield", "Fortress Shield",
            "Each level grants the main character 25 shield HP at the start of Phase 2.",
            BuildingBranch.CombatSupport, 1, 4, BuildingUpgradeEffect.Phase2Shield, 25f),
        ("Phase2Regen", "Blessing of Recovery",
            "Each level regenerates 1 HP per second for the main character in Phase 2.",
            BuildingBranch.CombatSupport, 2, 3, BuildingUpgradeEffect.Phase2Regen, 1f),
    };

    [MenuItem("Tools/Dungeon/Setup Building Tree Asset")]
    public static void CreateOrUpdateTreeAsset()
    {
        EnsureResourcesFolder();
        BuildingUpgradeTreeSO tree = LoadOrCreateTreeAsset();
        WriteNodes(tree);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildingTreeAssetSetup] Hoàn tất: {NodeTable.Length} node trong {TreeAssetPath}.");
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static BuildingUpgradeTreeSO LoadOrCreateTreeAsset()
    {
        var tree = AssetDatabase.LoadAssetAtPath<BuildingUpgradeTreeSO>(TreeAssetPath);
        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<BuildingUpgradeTreeSO>();
            AssetDatabase.CreateAsset(tree, TreeAssetPath);
        }

        return tree;
    }

    private static void WriteNodes(BuildingUpgradeTreeSO tree)
    {
        var serialized = new SerializedObject(tree);
        SerializedProperty nodes = serialized.FindProperty("nodes");
        nodes.arraySize = NodeTable.Length;

        for (int i = 0; i < NodeTable.Length; i++)
        {
            (string id, string displayName, string description, BuildingBranch branch,
                int tier, int maxLevel, BuildingUpgradeEffect effect, float valuePerLevel) = NodeTable[i];
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            node.FindPropertyRelative("id").stringValue = id;
            node.FindPropertyRelative("displayName").stringValue = displayName;
            node.FindPropertyRelative("description").stringValue = description;
            node.FindPropertyRelative("branch").enumValueIndex = (int)branch;
            node.FindPropertyRelative("tier").intValue = tier;
            node.FindPropertyRelative("maxLevel").intValue = maxLevel;
            node.FindPropertyRelative("effect").enumValueIndex = (int)effect;
            node.FindPropertyRelative("valuePerLevel").floatValue = valuePerLevel;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
