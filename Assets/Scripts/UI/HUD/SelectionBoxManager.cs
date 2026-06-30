using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Vẽ khung kéo chuột (RTS selection box) trên Canvas Screen Space - Overlay khi giữ-kéo chuột trái.
/// Chỉ lo phần HÌNH ẢNH của khung; việc chọn unit đã do <see cref="UnitController"/> đảm nhiệm nên khung
/// và vùng chọn luôn khớp nhau. Khung dùng <see cref="RectTransform"/> với pivot/anchor Bottom-Left, đặt
/// vị trí theo toạ độ màn hình (đã chia tỉ lệ Canvas). Dùng new Input System (<see cref="Mouse"/>).
/// </summary>
[DisallowMultipleComponent]
public class SelectionBoxManager : MonoBehaviour
{
    [Tooltip("RectTransform của UI Image làm khung chọn (pivot & anchor Bottom-Left).")]
    [SerializeField] private RectTransform selectionBox;

    [Tooltip("Canvas chứa khung (để quy đổi toạ độ màn hình theo scaleFactor). Tự tìm nếu bỏ trống.")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Quãng kéo tối thiểu (pixel) trước khi hiện khung - tránh nhấp nháy khi chỉ click.")]
    [SerializeField] [Min(0f)] private float dragThresholdPixels = 8f;

    private Vector2 startScreenPosition;
    private bool isDragging;
    private bool suppressed;

    private void Awake()
    {
        if (canvas == null && selectionBox != null)
        {
            canvas = selectionBox.GetComponentInParent<Canvas>();
        }
        HideBox();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || selectionBox == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginPotentialDrag(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.isPressed)
        {
            UpdateDrag(mouse.position.ReadValue());
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            HideBox();
        }
    }

    private void BeginPotentialDrag(Vector2 screenPosition)
    {
        startScreenPosition = screenPosition;
        isDragging = false;
        // Bắt đầu kéo từ trên một UI khác (bảng điều khiển) thì bỏ qua, không vẽ khung.
        suppressed = IsPointerOverUI();
    }

    private void UpdateDrag(Vector2 currentScreenPosition)
    {
        if (suppressed)
        {
            return;
        }

        if (!isDragging && Vector2.Distance(currentScreenPosition, startScreenPosition) >= dragThresholdPixels)
        {
            isDragging = true;
            selectionBox.gameObject.SetActive(true);
        }

        if (isDragging)
        {
            ResizeBox(startScreenPosition, currentScreenPosition);
        }
    }

    private void ResizeBox(Vector2 cornerA, Vector2 cornerB)
    {
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        Vector2 lowerLeft = Vector2.Min(cornerA, cornerB) / scale;
        Vector2 upperRight = Vector2.Max(cornerA, cornerB) / scale;

        selectionBox.anchoredPosition = lowerLeft;
        selectionBox.sizeDelta = upperRight - lowerLeft;
    }

    private void HideBox()
    {
        isDragging = false;
        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
