using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bộ điều phối duy nhất của SẢNH CHỜ (Lobby): cây kỹ năng 3 nhánh + 4 ô trang bị +
/// nút Bắt đầu. Toàn bộ logic mua/trang bị nằm ở đây, các view chỉ hiển thị và báo click.
///   - Click node khóa: đủ điều kiện + đủ điểm thì mua (trừ điểm, mở skill).
///   - Click node đã mở: gắn vào ô trống đầu tiên; đang trang bị thì gỡ ra (toggle).
///   - Click ô trang bị: xóa skill khỏi ô.
/// Trạng thái lưu trong PlayerProgression (PlayerPrefs) - sống qua mọi scene/lần chơi.
/// </summary>
public class LobbyController : MonoBehaviour
{
    private const string PhaseOneSceneName = "Phase 1";
    private const string PhaseTwoSceneName = "Phase 2";
    private const int UpgradeCost = 1;

    [Tooltip("Asset cây kỹ năng - nguồn node/giá/điều kiện.")]
    [SerializeField] private SkillTreeSO skillTree;

    [Tooltip("View của từng node - song song với nodeSkillNames.")]
    [SerializeField] private SkillNodeView[] nodeViews;

    [Tooltip("Tên skill của từng node - song song với nodeViews (editor tool gán).")]
    [SerializeField] private string[] nodeSkillNames;

    [Tooltip("4 ô trang bị ứng với phím 1-4.")]
    [SerializeField] private EquipSlotView[] equipSlots;

    [Tooltip("Nhãn hiển thị điểm kỹ năng đang có.")]
    [SerializeField] private TMP_Text skillPointsLabel;

    [Tooltip("Nút vào Phase 1.")]
    [SerializeField] private Button startButton;

    private void Start()
    {
        PlayerProgression.EnsureDefaults();
        BindViews();
        RefreshAll();
    }

    private void BindViews()
    {
        for (int i = 0; i < nodeViews.Length; i++)
        {
            SkillTreeSO.SkillTreeNode node = FindNode(nodeSkillNames[i]);
            if (node == null)
            {
                continue;
            }

            nodeViews[i].Bind(nodeSkillNames[i], node.skill.Icon, node.cost, OnNodeClicked, OnUpgradeClicked);
        }

        for (int i = 0; i < equipSlots.Length; i++)
        {
            equipSlots[i].Bind(i, OnEquipSlotClicked);
        }

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartRun);
    }

    private void OnNodeClicked(string skillName)
    {
        if (PlayerProgression.IsUnlocked(skillName))
        {
            ToggleEquip(skillName);
        }
        else
        {
            TryUnlock(skillName);
        }

        RefreshAll();
    }

    private void OnUpgradeClicked(string skillName)
    {
        int level = PlayerProgression.GetSkillLevel(skillName);
        bool canUpgrade = PlayerProgression.IsUnlocked(skillName) && level < PlayerProgression.MaxSkillLevel;
        if (canUpgrade && PlayerProgression.TrySpendSkillPoints(UpgradeCost))
        {
            PlayerProgression.SetSkillLevel(skillName, level + 1);
        }

        RefreshAll();
    }

    private void OnEquipSlotClicked(int slotIndex)
    {
        PlayerProgression.SetEquippedSlot(slotIndex, string.Empty);
        RefreshAll();
    }

    // Bắt đầu tại phase đã lưu (checkpoint); chưa có run thì mặc định Phase 1.
    private void StartRun()
    {
        int targetPhase = PlayerProgression.CurrentPhase >= 1 ? PlayerProgression.CurrentPhase : 1;
        PlayerProgression.CurrentPhase = targetPhase;
        SceneManager.LoadScene(targetPhase >= 2 ? PhaseTwoSceneName : PhaseOneSceneName);
    }

    private void TryUnlock(string skillName)
    {
        SkillTreeSO.SkillTreeNode node = FindNode(skillName);
        bool canUnlock = node != null && skillTree.ArePrerequisitesMet(node);
        if (canUnlock && PlayerProgression.TrySpendSkillPoints(node.cost))
        {
            PlayerProgression.Unlock(skillName);
        }
    }

    // Đang trang bị thì gỡ; chưa thì gắn vào ô trống đầu tiên (hết ô trống = không làm gì).
    private void ToggleEquip(string skillName)
    {
        string[] slots = PlayerProgression.GetEquippedSlots();
        int equippedIndex = Array.IndexOf(slots, skillName);
        if (equippedIndex >= 0)
        {
            PlayerProgression.SetEquippedSlot(equippedIndex, string.Empty);
            return;
        }

        int firstEmptyIndex = Array.FindIndex(slots, string.IsNullOrEmpty);
        if (firstEmptyIndex >= 0)
        {
            PlayerProgression.SetEquippedSlot(firstEmptyIndex, skillName);
        }
    }

    private void RefreshAll()
    {
        skillPointsLabel.text = $"Skill Points: {PlayerProgression.SkillPoints}";
        string[] equipped = PlayerProgression.GetEquippedSlots();

        bool canAffordUpgrade = PlayerProgression.SkillPoints >= UpgradeCost;
        for (int i = 0; i < nodeViews.Length; i++)
        {
            string name = nodeSkillNames[i];
            nodeViews[i].SetState(ResolveNodeState(name, equipped));
            nodeViews[i].SetProgress(
                PlayerProgression.GetSkillLevel(name), PlayerProgression.MaxSkillLevel, canAffordUpgrade);
        }

        for (int i = 0; i < equipSlots.Length; i++)
        {
            SkillDefinitionSO skill = skillTree.FindSkillByName(equipped[i]);
            equipSlots[i].SetSkill(skill != null ? skill.Icon : null);
        }
    }

    private SkillNodeState ResolveNodeState(string skillName, string[] equippedSlots)
    {
        if (PlayerProgression.IsUnlocked(skillName))
        {
            bool isEquipped = Array.IndexOf(equippedSlots, skillName) >= 0;
            return isEquipped ? SkillNodeState.Equipped : SkillNodeState.Unlocked;
        }

        SkillTreeSO.SkillTreeNode node = FindNode(skillName);
        bool isUnlockable = node != null
            && skillTree.ArePrerequisitesMet(node)
            && PlayerProgression.SkillPoints >= node.cost;
        return isUnlockable ? SkillNodeState.Unlockable : SkillNodeState.Locked;
    }

    private SkillTreeSO.SkillTreeNode FindNode(string skillName)
    {
        foreach (SkillTreeSO.SkillTreeNode node in skillTree.Nodes)
        {
            bool isMatch = node.skill != null && node.skill.SkillName == skillName;
            if (isMatch)
            {
                return node;
            }
        }

        return null;
    }
}
