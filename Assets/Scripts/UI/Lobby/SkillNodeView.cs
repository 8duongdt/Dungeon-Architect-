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
/// View "ngu" của một node cây kỹ năng: icon + giá + khung đổi màu theo trạng thái.
/// Mọi logic mở khóa/trang bị nằm ở LobbyController - node chỉ báo click qua callback.
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

    [Tooltip("Khung nền của node - đổi màu theo trạng thái.")]
    [SerializeField] private Image frame;

    [Tooltip("Icon của skill.")]
    [SerializeField] private Image icon;

    [Tooltip("Nhãn giá điểm (ẩn khi đã mở).")]
    [SerializeField] private TMP_Text costLabel;

    [Tooltip("Nút nhận click của node.")]
    [SerializeField] private Button button;

    private string skillName;
    private Action<string> clickCallback;

    public void Bind(string boundSkillName, Sprite skillIcon, int cost, Action<string> onClicked)
    {
        skillName = boundSkillName;
        clickCallback = onClicked;
        icon.sprite = skillIcon;
        costLabel.text = cost > 0 ? cost.ToString() : string.Empty;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(NotifyClicked);
    }

    public void SetState(SkillNodeState state)
    {
        frame.color = FrameColorFor(state);
        icon.color = state == SkillNodeState.Locked ? LockedIcon : NormalIcon;
        bool showCost = state == SkillNodeState.Locked || state == SkillNodeState.Unlockable;
        costLabel.gameObject.SetActive(showCost);
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(skillName);
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
