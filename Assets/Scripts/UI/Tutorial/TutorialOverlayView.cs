using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lớp hiển thị của tutorial: một panel chỉ dẫn nổi trên cùng, nút Next (chỉ cho bước "chỉ đọc"),
/// nút Skip Tutorial, và một mũi tên tùy chọn chỉ vào mục tiêu (world hoặc UI).
///
/// Overlay KHÔNG được chặn thao tác gameplay: đặt <see cref="CanvasGroup.blocksRaycasts"/> = false
/// trên lớp nền để người chơi vẫn click chọn lính / click bản đồ để xây; chỉ các nút mới nhận click.
/// </summary>
public class TutorialOverlayView : MonoBehaviour
{
    [Header("Gốc overlay")]
    [Tooltip("CanvasGroup của toàn overlay - dùng bật/tắt bằng alpha, để blocksRaycasts = false.")]
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Panel chỉ dẫn")]
    [SerializeField] private TMP_Text instructionText;

    [Header("Nút")]
    [Tooltip("Nút Next - chỉ hiện ở bước chỉ đọc.")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipTutorialButton;

    [Header("Mũi tên chỉ dẫn (tùy chọn)")]
    [SerializeField] private RectTransform highlightArrow;
    [Tooltip("Camera đổi tọa độ world -> screen cho mũi tên. Trống thì dùng Camera.main.")]
    [SerializeField] private Camera worldCamera;

    /// <summary>Bắn khi bấm Next (dùng qua bước chỉ đọc).</summary>
    public event Action NextRequested;

    /// <summary>Bắn khi bấm Skip Tutorial (bỏ qua toàn bộ tutorial của scene).</summary>
    public event Action SkipRequested;

    private System.Func<Transform> worldTargetProvider;
    private RectTransform uiTarget;

    private void OnEnable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(RaiseNext);
        }
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.AddListener(RaiseSkip);
        }
    }

    private void OnDisable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(RaiseNext);
        }
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.RemoveListener(RaiseSkip);
        }
    }

    private void Update()
    {
        UpdateArrow();
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (rootGroup == null)
        {
            return;
        }

        rootGroup.alpha = visible ? 1f : 0f;
        // blocksRaycasts=false trên CanvasGroup tắt raycast của TOÀN BỘ con - kể cả nút Next/Skip
        // (bug từng làm tutorial Phase 2 kẹt vì không bấm nổi Next). Phải bật khi hiện; việc
        // "click xuyên qua overlay" lo bằng raycastTarget=false trên panel/chữ/mũi tên trong prefab -
        // chỉ Graphic của nút giữ raycastTarget nên gameplay vẫn nhận chuột ở mọi chỗ khác.
        rootGroup.blocksRaycasts = visible;
        rootGroup.interactable = visible;
    }

    /// <summary>Đổi câu chỉ dẫn; chỉ bước "chỉ đọc" mới bật nút Next.</summary>
    public void SetInstruction(string text, bool showNextButton)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(showNextButton);
        }
    }

    /// <summary>
    /// Đặt nguồn mục tiêu cho mũi tên. <paramref name="worldTargetProvider"/> được gọi LẠI mỗi frame
    /// nên mũi tên bám theo cả mục tiêu di chuyển hoặc đổi (vd tinh thể gần nhất). World ưu tiên trước
    /// UI; cả hai trả về null thì ẩn mũi tên.
    /// </summary>
    public void SetHighlight(System.Func<Transform> worldTargetProvider, RectTransform uiTarget)
    {
        this.worldTargetProvider = worldTargetProvider;
        this.uiTarget = uiTarget;
        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (highlightArrow == null)
        {
            return;
        }

        Transform worldTarget = worldTargetProvider != null ? worldTargetProvider() : null;
        bool hasWorld = worldTarget != null;
        bool hasUi = uiTarget != null;
        highlightArrow.gameObject.SetActive(hasWorld || hasUi);

        if (hasWorld)
        {
            Camera activeCamera = worldCamera != null ? worldCamera : Camera.main;
            if (activeCamera != null)
            {
                highlightArrow.position = activeCamera.WorldToScreenPoint(worldTarget.position);
            }
        }
        else if (hasUi)
        {
            highlightArrow.position = uiTarget.position;
        }
    }

    private void RaiseNext()
    {
        NextRequested?.Invoke();
    }

    private void RaiseSkip()
    {
        SkipRequested?.Invoke();
    }
}
