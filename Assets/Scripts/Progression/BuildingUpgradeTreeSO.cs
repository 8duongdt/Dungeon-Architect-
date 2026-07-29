using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Hai nhánh của cây công trình - mỗi nhánh một cột trong UI Lobby.</summary>
public enum BuildingBranch
{
    Economy,       // Kinh tế: tối ưu sản lượng/thời gian khai thác Vàng-Mana.
    CombatSupport  // Hỗ trợ chiến đấu: buff nhân vật chính trong Phase 2.
}

/// <summary>Hiệu ứng mà một node cây công trình cộng dồn theo bậc.</summary>
public enum BuildingUpgradeEffect
{
    GoldOutput,         // +% sản lượng Vàng mỗi chu kỳ.
    ManaOutput,         // +% sản lượng Mana mỗi chu kỳ.
    CycleTimeReduction, // -% thời gian chu kỳ sản xuất (cap cứng ở BuildingProgressionEffects).
    Phase2Damage,       // +% sát thương skill của người chơi trong Phase 2.
    Phase2Shield,       // Khiên HP cấp sẵn khi vào Phase 2.
    Phase2Regen,        // HP hồi mỗi giây trong Phase 2.
    ObeliskAuraBonus    // +% cộng thêm vào buff lãnh thổ Obelisk (dmg/HP/tốc-đánh/tốc-chạy).
}

/// <summary>
/// Dữ liệu TĨNH của cây công trình: danh sách node (id + nhánh + hiệu ứng + lợi ích mỗi bậc).
/// Một asset duy nhất tại Assets/Resources/BuildingUpgradeTree.asset (dựng bằng
/// Tools > Dungeon > Setup Building Tree Asset) để BuildingProgressionEffects
/// Resources.Load được từ code tĩnh. Bậc đã mua nằm ở PlayerProgression.
/// Lợi ích tăng TUYẾN TÍNH theo bậc; giá nâng tăng lũy tiến qua UpgradeCostFormula.
/// </summary>
[CreateAssetMenu(fileName = "BuildingUpgradeTree", menuName = "Progression/Building Upgrade Tree")]
public class BuildingUpgradeTreeSO : ScriptableObject
{
    [Serializable]
    public class BuildingUpgradeNode
    {
        [Tooltip("Khóa lưu bậc trong PlayerProgression - không đổi sau khi phát hành.")]
        public string id;

        [Tooltip("Tên hiển thị trong Lobby.")]
        public string displayName;

        [Tooltip("Mô tả ngắn cho người chơi.")]
        [TextArea] public string description;

        [Tooltip("Nhánh chứa node - quyết định cột hiển thị trong Lobby.")]
        public BuildingBranch branch;

        [Tooltip("Bậc trong nhánh (1 = gốc) - quyết định hàng hiển thị.")]
        public int tier = 1;

        [Tooltip("Bậc tối đa của node.")]
        public int maxLevel = 3;

        [Tooltip("Hiệu ứng node cộng dồn.")]
        public BuildingUpgradeEffect effect;

        [Tooltip("Lợi ích mỗi bậc (tuyến tính): 0.2 = +20%/bậc, 25 = 25 HP khiên/bậc...")]
        public float valuePerLevel;
    }

    [Tooltip("Toàn bộ node của cây - dựng bằng editor tool, không sửa tay.")]
    [SerializeField] private List<BuildingUpgradeNode> nodes = new List<BuildingUpgradeNode>();

    public IReadOnlyList<BuildingUpgradeNode> Nodes => nodes;

    /// <summary>Tra node theo id lưu trong PlayerProgression (null nếu không có).</summary>
    public BuildingUpgradeNode FindNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (BuildingUpgradeNode node in nodes)
        {
            if (node.id == id)
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Node đầu tiên mang hiệu ứng - mỗi hiệu ứng chỉ nên có một node (null nếu không có).</summary>
    public BuildingUpgradeNode FindNodeByEffect(BuildingUpgradeEffect effect)
    {
        foreach (BuildingUpgradeNode node in nodes)
        {
            if (node.effect == effect)
            {
                return node;
            }
        }

        return null;
    }
}
