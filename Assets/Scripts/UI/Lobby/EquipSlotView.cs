using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View "ngu" của một ô trang bị (phím 1-4) ở Lobby: icon skill đang gán + nhãn phím.
/// Click ô = yêu cầu LobbyController xóa skill khỏi ô; ô trống thì icon ẩn.
/// </summary>
[DisallowMultipleComponent]
public class EquipSlotView : MonoBehaviour
{
    [Tooltip("Icon của skill đang trang bị (ẩn khi ô trống).")]
    [SerializeField] private Image icon;

    [Tooltip("Nhãn phím tắt (1-4).")]
    [SerializeField] private TMP_Text keyLabel;

    [Tooltip("Nút nhận click của ô.")]
    [SerializeField] private Button button;

    private int slotIndex;
    private Action<int> clickCallback;

    public void Bind(int boundSlotIndex, Action<int> onClicked)
    {
        slotIndex = boundSlotIndex;
        clickCallback = onClicked;
        keyLabel.text = (boundSlotIndex + 1).ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(NotifyClicked);
    }

    public void SetSkill(Sprite skillIcon)
    {
        icon.sprite = skillIcon;
        icon.enabled = skillIcon != null;
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(slotIndex);
    }
}
