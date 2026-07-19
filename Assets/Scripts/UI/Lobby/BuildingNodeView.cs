using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View "ngu" của một node cây công trình: tên + hiệu ứng hiện tại + bậc + giá nâng + nút "+".
/// Mọi logic mua nằm ở LobbyController - node chỉ báo click qua callback.
/// Khác SkillNodeView: không có trạng thái trang bị, node thuần văn bản (không icon).
/// </summary>
[DisallowMultipleComponent]
public class BuildingNodeView : MonoBehaviour
{
    private static readonly Color UnpurchasedFrame = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color AffordableFrame = new Color(0.95f, 0.78f, 0.25f, 1f);
    private static readonly Color OwnedFrame = Color.white;
    private static readonly Color MaxedFrame = new Color(0.35f, 0.9f, 0.4f, 1f);

    private const string MaxedCostText = "MAX";

    [Tooltip("Khung nền của node - đổi màu theo trạng thái.")]
    [SerializeField] private Image frame;

    [Tooltip("Tên công trình nâng cấp.")]
    [SerializeField] private TMP_Text nameLabel;

    [Tooltip("Hiệu ứng cộng dồn hiện tại (vd '+40% sản lượng Vàng').")]
    [SerializeField] private TMP_Text effectLabel;

    [Tooltip("Bậc hiện tại / tối đa (vd 'Bậc 2/5').")]
    [SerializeField] private TMP_Text levelLabel;

    [Tooltip("Giá nâng bậc kế tiếp, hoặc 'TỐI ĐA'.")]
    [SerializeField] private TMP_Text costLabel;

    [Tooltip("Nút '+' nâng bậc - chỉ bấm được khi đủ điểm và chưa max.")]
    [SerializeField] private Button upgradeButton;

    private string upgradeId;
    private Action<string> upgradeCallback;

    public void Bind(string boundUpgradeId, string displayName, Action<string> onUpgrade)
    {
        upgradeId = boundUpgradeId;
        upgradeCallback = onUpgrade;
        nameLabel.text = displayName;

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(NotifyUpgrade);
    }

    /// <summary>Cập nhật toàn bộ hiển thị của node theo bậc/giá/khả năng chi trả hiện tại.</summary>
    public void SetProgress(int level, int maxLevel, string effectText, int nextCost, bool canAfford)
    {
        bool isMaxed = level >= maxLevel;
        effectLabel.text = effectText;
        levelLabel.text = $"Lv {level}/{maxLevel}";
        costLabel.text = isMaxed ? MaxedCostText : $"Upgrade: {nextCost} pts";
        upgradeButton.gameObject.SetActive(!isMaxed);
        upgradeButton.interactable = !isMaxed && canAfford;
        frame.color = FrameColorFor(level, isMaxed, canAfford);
    }

    private void NotifyUpgrade()
    {
        upgradeCallback?.Invoke(upgradeId);
    }

    private static Color FrameColorFor(int level, bool isMaxed, bool canAfford)
    {
        if (isMaxed)
        {
            return MaxedFrame;
        }

        if (canAfford)
        {
            return AffordableFrame;
        }

        return level >= 1 ? OwnedFrame : UnpurchasedFrame;
    }
}
