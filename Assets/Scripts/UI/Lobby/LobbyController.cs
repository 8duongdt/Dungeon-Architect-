using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bộ điều phối duy nhất của SẢNH CHỜ (Lobby): hai tab KỸ NĂNG / CÔNG TRÌNH + 4 ô trang bị +
/// nút Bắt đầu. Toàn bộ logic mua/trang bị/nâng nằm ở đây, các view chỉ hiển thị và báo click.
///   - Tab Kỹ năng: click node khóa để mua, node đã mở để gắn/gỡ ô trang bị, nút "+" để nâng bậc.
///   - Tab Công trình: nút "+" nâng bậc node (2 nhánh Kinh tế / Hỗ trợ chiến đấu, không cần mở khóa).
///   - Giá nâng bậc CẢ HAI cây theo UpgradeCostFormula (lũy tiến), trả bằng hai loại điểm riêng.
/// Trạng thái lưu trong PlayerProgression (PlayerPrefs) - sống qua mọi scene/lần chơi.
/// </summary>
public class LobbyController : MonoBehaviour
{
    private const string PhaseOneSceneName = "Phase 1";
    private const string PhaseTwoSceneName = "Phase 2";

    [Tooltip("Asset cây kỹ năng - nguồn node/giá/điều kiện.")]
    [SerializeField] private SkillTreeSO skillTree;

    [Tooltip("View của từng node kỹ năng - song song với nodeSkillNames.")]
    [SerializeField] private SkillNodeView[] nodeViews;

    [Tooltip("Tên skill của từng node - song song với nodeViews (editor tool gán).")]
    [SerializeField] private string[] nodeSkillNames;

    [Tooltip("Asset cây công trình - nguồn node/hiệu ứng/bậc tối đa.")]
    [SerializeField] private BuildingUpgradeTreeSO buildingTree;

    [Tooltip("View của từng node công trình - song song với buildingNodeIds.")]
    [SerializeField] private BuildingNodeView[] buildingNodeViews;

    [Tooltip("Id node công trình - song song với buildingNodeViews (editor tool gán).")]
    [SerializeField] private string[] buildingNodeIds;

    [Tooltip("Panel chứa cây kỹ năng + thanh trang bị.")]
    [SerializeField] private GameObject skillTreePanel;

    [Tooltip("Panel chứa cây công trình (ẩn sẵn).")]
    [SerializeField] private GameObject buildingTreePanel;

    [Tooltip("Nút chuyển sang tab kỹ năng.")]
    [SerializeField] private Button skillTabButton;

    [Tooltip("Nút chuyển sang tab công trình.")]
    [SerializeField] private Button buildingTabButton;

    [Tooltip("4 ô trang bị ứng với phím 1-4.")]
    [SerializeField] private EquipSlotView[] equipSlots;

    [Tooltip("Nhãn hiển thị điểm kỹ năng + điểm công trình đang có.")]
    [SerializeField] private TMP_Text pointsLabel;

    [Tooltip("Nút vào Phase 1.")]
    [SerializeField] private Button startButton;

    [Header("Âm thanh")]
    [Tooltip("Click chung: đổi tab, gắn/gỡ trang bị.")]
    [SerializeField] private AudioClip clickSfx;
    [Tooltip("Mua/nâng bậc thành công.")]
    [SerializeField] private AudioClip confirmSfx;
    [Tooltip("Thao tác bị từ chối (chưa đủ điều kiện/điểm).")]
    [SerializeField] private AudioClip deniedSfx;
    [Tooltip("Bấm nút Bắt đầu.")]
    [SerializeField] private AudioClip startRunSfx;

    private void Start()
    {
        PlayerProgression.EnsureDefaults();
        BindViews();
        ShowSkillTab();
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

        for (int i = 0; i < buildingNodeViews.Length; i++)
        {
            BuildingUpgradeTreeSO.BuildingUpgradeNode node = buildingTree.FindNode(buildingNodeIds[i]);
            if (node == null)
            {
                continue;
            }

            buildingNodeViews[i].Bind(node.id, node.displayName, OnBuildingUpgradeClicked);
        }

        for (int i = 0; i < equipSlots.Length; i++)
        {
            equipSlots[i].Bind(i, OnEquipSlotClicked);
        }

        skillTabButton.onClick.RemoveAllListeners();
        skillTabButton.onClick.AddListener(ShowSkillTab);
        buildingTabButton.onClick.RemoveAllListeners();
        buildingTabButton.onClick.AddListener(ShowBuildingTab);
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(StartRun);
    }

    private void ShowSkillTab()
    {
        AudioManager.Instance.PlayOneShot(clickSfx);
        SetActiveTab(isSkillTab: true);
    }

    private void ShowBuildingTab()
    {
        AudioManager.Instance.PlayOneShot(clickSfx);
        SetActiveTab(isSkillTab: false);
    }

    // Nút của tab đang mở bị vô hiệu để người chơi biết mình đang đứng ở đâu.
    private void SetActiveTab(bool isSkillTab)
    {
        skillTreePanel.SetActive(isSkillTab);
        buildingTreePanel.SetActive(!isSkillTab);
        skillTabButton.interactable = !isSkillTab;
        buildingTabButton.interactable = isSkillTab;
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
        if (canUpgrade && PlayerProgression.TrySpendSkillPoints(UpgradeCostFormula.NextLevelCost(level)))
        {
            PlayerProgression.SetSkillLevel(skillName, level + 1);
            AudioManager.Instance.PlayOneShot(confirmSfx);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(deniedSfx);
        }

        RefreshAll();
    }

    private void OnBuildingUpgradeClicked(string upgradeId)
    {
        BuildingUpgradeTreeSO.BuildingUpgradeNode node = buildingTree.FindNode(upgradeId);
        int level = PlayerProgression.GetBuildingLevel(upgradeId);
        bool canUpgrade = node != null && level < node.maxLevel;
        if (canUpgrade && PlayerProgression.TrySpendBuildingPoints(UpgradeCostFormula.NextLevelCost(level)))
        {
            PlayerProgression.SetBuildingLevel(upgradeId, level + 1);
            AudioManager.Instance.PlayOneShot(confirmSfx);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(deniedSfx);
        }

        RefreshAll();
    }

    private void OnEquipSlotClicked(int slotIndex)
    {
        AudioManager.Instance.PlayOneShot(clickSfx);
        PlayerProgression.SetEquippedSlot(slotIndex, string.Empty);
        RefreshAll();
    }

    // Bắt đầu tại phase đã lưu (checkpoint); chưa có run thì mặc định Phase 1.
    private void StartRun()
    {
        AudioManager.Instance.PlayOneShot(startRunSfx);
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
            AudioManager.Instance.PlayOneShot(confirmSfx);
        }
        else
        {
            AudioManager.Instance.PlayOneShot(deniedSfx);
        }
    }

    // Đang trang bị thì gỡ; chưa thì gắn vào ô trống đầu tiên (hết ô trống = không làm gì).
    private void ToggleEquip(string skillName)
    {
        AudioManager.Instance.PlayOneShot(clickSfx);
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
        pointsLabel.text =
            $"Skill Points: {PlayerProgression.SkillPoints}   |   Building Points: {PlayerProgression.BuildingPoints}";
        RefreshSkillNodes();
        RefreshBuildingNodes();
        RefreshEquipSlots();
    }

    private void RefreshSkillNodes()
    {
        string[] equipped = PlayerProgression.GetEquippedSlots();
        for (int i = 0; i < nodeViews.Length; i++)
        {
            string name = nodeSkillNames[i];
            int level = PlayerProgression.GetSkillLevel(name);
            // Giá hiển thị, điều kiện bật nút và số điểm bị trừ cùng đi qua một công thức.
            int nextCost = UpgradeCostFormula.NextLevelCost(level);
            nodeViews[i].SetState(ResolveNodeState(name, equipped));
            nodeViews[i].SetProgress(
                level, PlayerProgression.MaxSkillLevel, nextCost, PlayerProgression.SkillPoints >= nextCost);
        }
    }

    private void RefreshBuildingNodes()
    {
        for (int i = 0; i < buildingNodeViews.Length; i++)
        {
            BuildingUpgradeTreeSO.BuildingUpgradeNode node = buildingTree.FindNode(buildingNodeIds[i]);
            if (node == null)
            {
                continue;
            }

            int level = PlayerProgression.GetBuildingLevel(node.id);
            int nextCost = UpgradeCostFormula.NextLevelCost(level);
            bool canAfford = PlayerProgression.BuildingPoints >= nextCost;
            buildingNodeViews[i].SetProgress(level, node.maxLevel, FormatEffectText(node, level), nextCost, canAfford);
        }
    }

    private void RefreshEquipSlots()
    {
        string[] equipped = PlayerProgression.GetEquippedSlots();
        for (int i = 0; i < equipSlots.Length; i++)
        {
            SkillDefinitionSO skill = skillTree.FindSkillByName(equipped[i]);
            equipSlots[i].SetSkill(skill != null ? skill.Icon : null);
        }
    }

    // Chưa nâng bậc nào thì mô tả lợi ích MỖI BẬC để người chơi biết mình sắp mua gì.
    private static string FormatEffectText(BuildingUpgradeTreeSO.BuildingUpgradeNode node, int level)
    {
        bool isPreview = level <= 0;
        float value = isPreview ? node.valuePerLevel : level * node.valuePerLevel;
        string suffix = isPreview ? "/lv" : string.Empty;
        int percent = Mathf.RoundToInt(value * 100f);

        switch (node.effect)
        {
            case BuildingUpgradeEffect.GoldOutput: return $"+{percent}% Gold output{suffix}";
            case BuildingUpgradeEffect.ManaOutput: return $"+{percent}% Mana output{suffix}";
            case BuildingUpgradeEffect.CycleTimeReduction: return $"-{percent}% production time{suffix}";
            case BuildingUpgradeEffect.Phase2Damage: return $"+{percent}% damage (Phase 2){suffix}";
            case BuildingUpgradeEffect.Phase2Shield: return $"{value:0.#} HP shield at Phase 2 start{suffix}";
            case BuildingUpgradeEffect.Phase2Regen: return $"Regen {value:0.#} HP/s (Phase 2){suffix}";
            default: return string.Empty;
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
