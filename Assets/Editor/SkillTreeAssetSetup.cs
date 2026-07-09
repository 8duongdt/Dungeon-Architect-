using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dựng asset cây kỹ năng (Assets/Skills/SkillTree.asset) từ bảng thiết kế 3 nhánh:
///   Fire:      FireBall T1 (miễn phí) -> FireWall T2 -> Explosion T3 -> SunStrike T4
///   Lightning: Lightning T1 -> LightningBolt T2
///   Control:   Shield T1 -> (Spikes T2 | MidasTouch T2) -> BlackHole T3 (cần 1 trong 2)
///
///   Menu: Tools > Dungeon > Setup Skill Tree Asset
///
/// Prerequisite lưu theo SkillDefinitionSO.SkillName ĐỌC TỪ ASSET đã load
/// (không hardcode lại chuỗi) để không lệch với PlayerProgression. Chạy lại an toàn.
/// </summary>
public static class SkillTreeAssetSetup
{
    private const string TreeAssetPath = "Assets/Skills/SkillTree.asset";
    private const string SkillAssetPathFormat = "Assets/Skills/Skill_{0}.asset";

    // (tên file asset, nhánh, bậc, giá, các asset điều kiện - any-of)
    private static readonly (string assetName, SkillBranch branch, int tier, int cost, string[] prerequisites)[] NodeTable =
    {
        ("FireBall",      SkillBranch.Fire,      1, 0, new string[0]),
        ("FireWall",      SkillBranch.Fire,      2, 1, new[] { "FireBall" }),
        ("Explosion",     SkillBranch.Fire,      3, 1, new[] { "FireWall" }),
        ("SunStrike",     SkillBranch.Fire,      4, 1, new[] { "Explosion" }),
        ("Lightning",     SkillBranch.Lightning, 1, 1, new string[0]),
        ("LightningBolt", SkillBranch.Lightning, 2, 1, new[] { "Lightning" }),
        ("Shield",        SkillBranch.Control,   1, 1, new string[0]),
        ("Spikes",        SkillBranch.Control,   2, 1, new[] { "Shield" }),
        ("MidasTouch",    SkillBranch.Control,   2, 1, new[] { "Shield" }),
        ("BlackHole",     SkillBranch.Control,   3, 1, new[] { "Spikes", "MidasTouch" }),
    };

    [MenuItem("Tools/Dungeon/Setup Skill Tree Asset")]
    public static void CreateOrUpdateTreeAsset()
    {
        Dictionary<string, SkillDefinitionSO> skillsByAssetName = LoadAllSkillAssets();
        if (skillsByAssetName == null)
        {
            return;
        }

        SkillTreeSO tree = LoadOrCreateTreeAsset();
        WriteNodes(tree, skillsByAssetName);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SkillTreeAssetSetup] Hoàn tất: {NodeTable.Length} node trong {TreeAssetPath}.");
    }

    private static Dictionary<string, SkillDefinitionSO> LoadAllSkillAssets()
    {
        var skills = new Dictionary<string, SkillDefinitionSO>();
        foreach ((string assetName, _, _, _, _) in NodeTable)
        {
            string path = string.Format(SkillAssetPathFormat, assetName);
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(path);
            if (skill == null)
            {
                Debug.LogError($"[SkillTreeAssetSetup] Thiếu asset {path} - chạy Tools > Dungeon > Setup Ranged Skills trước.");
                return null;
            }

            skills[assetName] = skill;
        }

        return skills;
    }

    private static SkillTreeSO LoadOrCreateTreeAsset()
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeSO>(TreeAssetPath);
        if (tree == null)
        {
            tree = ScriptableObject.CreateInstance<SkillTreeSO>();
            AssetDatabase.CreateAsset(tree, TreeAssetPath);
        }

        return tree;
    }

    private static void WriteNodes(SkillTreeSO tree, Dictionary<string, SkillDefinitionSO> skillsByAssetName)
    {
        var serialized = new SerializedObject(tree);
        SerializedProperty nodes = serialized.FindProperty("nodes");
        nodes.arraySize = NodeTable.Length;

        for (int i = 0; i < NodeTable.Length; i++)
        {
            (string assetName, SkillBranch branch, int tier, int cost, string[] prerequisites) = NodeTable[i];
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            node.FindPropertyRelative("skill").objectReferenceValue = skillsByAssetName[assetName];
            node.FindPropertyRelative("branch").enumValueIndex = (int)branch;
            node.FindPropertyRelative("tier").intValue = tier;
            node.FindPropertyRelative("cost").intValue = cost;
            WritePrerequisites(node, prerequisites, skillsByAssetName);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Prerequisite lưu bằng SkillName thật của asset - nguồn duy nhất, không hardcode lại.
    private static void WritePrerequisites(SerializedProperty node, string[] prerequisiteAssetNames,
        Dictionary<string, SkillDefinitionSO> skillsByAssetName)
    {
        SerializedProperty prerequisites = node.FindPropertyRelative("prerequisiteSkillNames");
        prerequisites.arraySize = prerequisiteAssetNames.Length;
        for (int i = 0; i < prerequisiteAssetNames.Length; i++)
        {
            prerequisites.GetArrayElementAtIndex(i).stringValue =
                skillsByAssetName[prerequisiteAssetNames[i]].SkillName;
        }
    }
}
