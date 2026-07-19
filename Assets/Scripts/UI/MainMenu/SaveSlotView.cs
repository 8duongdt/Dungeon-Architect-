using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// View "ngu" của một ô save trên bảng chọn slot: một nhãn tóm tắt (slot mấy, phase, điểm, giờ lưu)
/// và một nút nhận click. Toàn bộ logic (nạp/tạo save, chuyển scene) do <see cref="SaveSlotPanel"/> giữ;
/// view chỉ hiển thị chuỗi tóm tắt và báo lại slot được bấm. Mô phỏng theo EquipSlotView ở Lobby.
///
/// Nút xóa dùng XÁC NHẬN HAI BƯỚC ngay trên nút (không cần dialog riêng): bấm lần 1 nút chuyển
/// "OK?" trong vài giây, bấm lần 2 trong lúc đó mới thật sự xóa - chống mất save vì lỡ tay.
/// </summary>
[DisallowMultipleComponent]
public class SaveSlotView : MonoBehaviour
{
    // Cửa sổ (giây, unscaled) chờ cú bấm xác nhận thứ hai trước khi nút tự trở lại trạng thái "X".
    private const float DeleteConfirmWindowSeconds = 2.5f;

    private const string DeleteIdleLabel = "X";
    private const string DeleteConfirmLabel = "OK?";

    // Màu nền nút xóa: đỏ sậm khi bình thường, cam sáng khi đang chờ xác nhận.
    private static readonly Color DeleteIdleColor = new Color(0.72f, 0.12f, 0.12f, 0.92f);
    private static readonly Color DeleteConfirmColor = new Color(0.95f, 0.55f, 0.1f, 1f);

    [Tooltip("Nhãn tóm tắt nội dung slot (nhiều dòng).")]
    [SerializeField] private TMP_Text summaryLabel;

    [Tooltip("Nút nhận click của ô.")]
    [SerializeField] private Button button;

    [Tooltip("Nút X xóa save của ô này (chỉ hiện khi slot có save).")]
    [SerializeField] private Button deleteButton;

    private int slotIndex;
    private Action<int> clickCallback;
    private Action<int> deleteCallback;

    private TMP_Text deleteLabel;
    private Image deleteBackground;
    private float confirmDeadline;
    private bool isAwaitingConfirm;

    public void Bind(int boundSlotIndex, Action<int> onClicked, Action<int> onDelete, bool hasSave)
    {
        slotIndex = boundSlotIndex;
        clickCallback = onClicked;
        deleteCallback = onDelete;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(NotifyClicked);

        if (deleteButton != null)
        {
            ResolveDeleteVisualReferences();
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(HandleDeleteClicked);
            // Chỉ cho xóa slot đã có save - slot trống không có gì để xóa.
            deleteButton.gameObject.SetActive(hasSave);
            ResetDeleteConfirm();
        }
    }

    public void SetSummary(string summary)
    {
        if (summaryLabel != null)
        {
            summaryLabel.text = summary;
        }
    }

    private void Update()
    {
        // Hết cửa sổ xác nhận mà không bấm lần hai -> nút tự trở lại "X".
        if (isAwaitingConfirm && Time.unscaledTime >= confirmDeadline)
        {
            ResetDeleteConfirm();
        }
    }

    private void NotifyClicked()
    {
        clickCallback?.Invoke(slotIndex);
    }

    // Bấm lần 1: chuyển sang trạng thái chờ xác nhận; bấm lần 2 trong cửa sổ: xóa thật.
    private void HandleDeleteClicked()
    {
        if (!isAwaitingConfirm)
        {
            isAwaitingConfirm = true;
            confirmDeadline = Time.unscaledTime + DeleteConfirmWindowSeconds;
            ApplyDeleteVisual(DeleteConfirmLabel, DeleteConfirmColor);
            return;
        }

        ResetDeleteConfirm();
        deleteCallback?.Invoke(slotIndex);
    }

    private void ResetDeleteConfirm()
    {
        isAwaitingConfirm = false;
        ApplyDeleteVisual(DeleteIdleLabel, DeleteIdleColor);
    }

    private void ApplyDeleteVisual(string label, Color backgroundColor)
    {
        if (deleteLabel != null)
        {
            deleteLabel.text = label;
        }
        if (deleteBackground != null)
        {
            deleteBackground.color = backgroundColor;
        }
    }

    private void ResolveDeleteVisualReferences()
    {
        if (deleteLabel == null)
        {
            deleteLabel = deleteButton.GetComponentInChildren<TMP_Text>(true);
        }
        if (deleteBackground == null)
        {
            deleteBackground = deleteButton.GetComponent<Image>();
        }
    }
}
