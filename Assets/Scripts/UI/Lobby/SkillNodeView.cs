using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Trạng thái hiển thị của một node trong cây kỹ năng.</summary>
public enum SkillNodeState
{
    Locked,     // Chưa đủ điều kiện hoặc chưa đủ điểm - xám mờ.
    Unlockable, // Đủ điều kiện mở, chờ người chơi mua - viền vàng.
    Unlocked,   // Đã mở, click để trang bị - sáng trắng.
    Equipped    // Đã mở và đang nằm trên thanh skill - viền xanh lá.
}

/// <summary>
/// View "ngu" của một node cây kỹ năng: icon + giá + khung đổi màu + nhãn bậc + nút "+" nâng cấp.
/// Mọi logic mở khóa/trang bị/nâng nằm ở LobbyController - node chỉ báo click qua callback.
///   - Click thân node: mở khóa (nếu khóa) hoặc gắn/gỡ khỏi thanh skill (nếu đã mở).
///   - Click nút "+": nâng bậc skill (chỉ bật khi đã mở, chưa max, đủ điểm).
/// </summary>
[DisallowMultipleComponent]
public class SkillNodeView : MonoBehaviour
{
    private static readonly Color LockedFrame = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color UnlockableFrame = new Color(0.95f, 0.78f, 0.25f, 1f);
    private static readonly Color UnlockedFrame = Color.white;
    private static readonly Color EquippedFrame = new Color(0.35f, 0.9f, 0.4f, 1f);

    private static readonly Color LockedIcon = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    private static readonly Color NormalIcon = Color.white;

    // Hệ số nhân theo bậc, khớp với PlayerProgression (chỉ để hiển thị nhãn).
    private static readonly float[] LevelMultipliers = { 1f, 1.5f, 2f };

    [Tooltip("Khung nền của node - đổi màu theo trạng thái.")]
    [SerializeField] private Image frame;

    [Tooltip("Icon của skill.")]
    [SerializeField] private Image icon;

    [Tooltip("Nhãn giá điểm (ẩn khi đã mở).")]
    [SerializeField] private TMP_Text costLabel;

    [Tooltip("Nút nhận click của node.")]
    [SerializeField] private Button button;

    [Tooltip("Nhãn bậc + hệ số (vd 'Lv 2/3  x1.5'). Ẩn khi chưa mở.")]
    [SerializeField] private TMP_Text levelLabel;

    [Tooltip("Nút '+' nâng bậc skill. Ẩn khi chưa mở hoặc đã đạt bậc tối đa.")]
    [SerializeField] private Button upgradeButton;

    private string skillName;
    private Action<string> clickCallback;
    private Action<string> upgradeCallback;

    public void Bind(string boundSkillName, Sprite skillIcon, int cost,
        Action<string> onClicked, Action<string> onUpgrade)
    {
        skillName = boundSkillName;
        clickCallback = onClicked;
        upgradeCallback = onUpgrade;
        icon.sprite = skillIcon;
        costLabel.text = cost > 0 ? cost.ToString() : string.Empty;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(NotifyClicked);

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(NotifyUpgrade);
        }
    }

    public void SetState(SkillNodeState state)
    {
        frame.color = FrameColorFor(state);
        icon.color = state == SkillNodeState.Locked ? LockedIcon : NormalIcon;
        bool showCost = state == SkillNodeState.Locked || state == SkillNodeState.Unlockable;
        costLabel.gameObject.SetActive(showCost);
    }

    /// <summary>Cập nhật nhãn bậc + nút "+"; canUpgrade = đã mở, chưa max, đủ điểm.</summary>
    public void SetProgress(int level, int maxLevel, bool canUpgrade)
    {
        bool isUnlocked = level >= 1;
        if (levelLabel != null)
        {
            levelLabel.gameObject.SetActive(isUnlocked);
            if (isUnlocked)
            {
                float multiplier = LevelMultipliers[Mathf.Clamp(level - 1, 0, LevelMultipliers.Length - 1)];
                levelLabel.text = $"Lv{level} x{multiplier.ToString("0.#")}";
            }
        }

        if (upgradeButton != null)
        {
            bool showUpgrade = isUnlocked && level < maxLevel;
            upgradeButton.gameObject.SetActive(showUpgrade);
            upgradeButton.interactable = showUpgrade && canUpgrade;
        }
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(skillName);
    }

    private void NotifyUpgrade()
    {
        upgradeCallback?.Invoke(skillName);
    }

    private static Color FrameColorFor(SkillNodeState state)
    {
        switch (state)
        {
            case SkillNodeState.Unlockable: return UnlockableFrame;
            case SkillNodeState.Unlocked: return UnlockedFrame;
            case SkillNodeState.Equipped: return EquippedFrame;
            default: return LockedFrame;
        }
    }
}
