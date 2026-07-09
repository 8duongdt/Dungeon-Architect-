using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Ba nhánh của cây kỹ năng - mỗi nhánh một cột trong UI Lobby.</summary>
public enum SkillBranch
{
    Fire,
    Lightning,
    Control
}

/// <summary>
/// Dữ liệu TĨNH của cây kỹ năng: danh sách node (skill + nhánh + bậc + giá + điều kiện).
/// Một asset duy nhất (Assets/Skills/SkillTree.asset, dựng bằng Tools > Dungeon >
/// Setup Skill Tree Asset) - vừa là registry tên->SkillDefinitionSO cho runtime,
/// vừa là nguồn layout cho UI Lobby. Trạng thái mở khóa nằm ở PlayerProgression.
/// </summary>
[CreateAssetMenu(fileName = "SkillTree", menuName = "Skills/Skill Tree")]
public class SkillTreeSO : ScriptableObject
{
    [Serializable]
    public class SkillTreeNode
    {
        [Tooltip("Asset skill mà node này mở khóa.")]
        public SkillDefinitionSO skill;

        [Tooltip("Nhánh chứa node - quyết định cột hiển thị trong Lobby.")]
        public SkillBranch branch;

        [Tooltip("Bậc trong nhánh (1 = gốc) - quyết định hàng hiển thị.")]
        public int tier = 1;

        [Tooltip("Giá điểm kỹ năng để mở node (0 = miễn phí).")]
        public int cost = 1;

        [Tooltip("Tên các skill điều kiện - chỉ cần mở ÍT NHẤT MỘT. Rỗng = node gốc.")]
        public string[] prerequisiteSkillNames = Array.Empty<string>();
    }

    [Tooltip("Toàn bộ node của cây - dựng bằng editor tool, không sửa tay.")]
    [SerializeField] private List<SkillTreeNode> nodes = new List<SkillTreeNode>();

    public IReadOnlyList<SkillTreeNode> Nodes => nodes;

    /// <summary>Tra SkillDefinitionSO theo tên lưu trong PlayerProgression (null nếu không có).</summary>
    public SkillDefinitionSO FindSkillByName(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return null;
        }

        foreach (SkillTreeNode node in nodes)
        {
            bool isMatch = node.skill != null && node.skill.SkillName == skillName;
            if (isMatch)
            {
                return node.skill;
            }
        }

        return null;
    }

    /// <summary>Node đủ điều kiện mở khi đã mở ít nhất một prerequisite (rỗng = luôn đủ).</summary>
    public bool ArePrerequisitesMet(SkillTreeNode node)
    {
        if (node.prerequisiteSkillNames == null || node.prerequisiteSkillNames.Length == 0)
        {
            return true;
        }

        foreach (string prerequisite in node.prerequisiteSkillNames)
        {
            if (PlayerProgression.IsUnlocked(prerequisite))
            {
                return true;
            }
        }

        return false;
    }
}
