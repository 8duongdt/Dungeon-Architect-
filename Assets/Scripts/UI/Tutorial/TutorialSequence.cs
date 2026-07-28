using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Điều phối chuỗi hướng dẫn của MỘT scene gameplay (Lobby / Phase 1 / Phase 2). Các bước là những
/// component <see cref="TutorialStep"/> con, chạy lần lượt theo thứ tự trên cây phân cấp.
///
/// Chỉ hiện LẦN ĐẦU vào scene: nếu <see cref="PlayerProgression.HasSeenTutorial"/> đã đánh dấu
/// <see cref="tutorialId"/> thì tự tắt. Xong hết bước (hoặc bấm Skip Tutorial) thì
/// <see cref="PlayerProgression.MarkTutorialSeen"/> để lần sau vào scene không hiện lại nữa.
/// </summary>
public class TutorialSequence : MonoBehaviour
{
    [Tooltip("Khóa nhận diện tutorial của scene: \"lobby\" / \"phase1\" / \"phase2\".")]
    [SerializeField] private string tutorialId;

    [SerializeField] private TutorialOverlayView overlay;

    private readonly List<TutorialStep> steps = new List<TutorialStep>();
    private int currentIndex = -1;

    private void Start()
    {
        CollectSteps();

        bool cannotRun = string.IsNullOrEmpty(tutorialId) || overlay == null || steps.Count == 0;
        if (cannotRun || PlayerProgression.HasSeenTutorial(tutorialId))
        {
            HideOverlay();
            enabled = false;
            return;
        }

        overlay.NextRequested += HandleNextRequested;
        overlay.SkipRequested += HandleSkipRequested;
        overlay.Show();
        BeginStep(0);
    }

    private void OnDisable()
    {
        if (overlay != null)
        {
            overlay.NextRequested -= HandleNextRequested;
            overlay.SkipRequested -= HandleSkipRequested;
        }

        UnsubscribeCurrentStep();
    }

    // Lấy các bước là component con theo đúng thứ tự cây phân cấp (kể cả object đang tắt).
    private void CollectSteps()
    {
        steps.Clear();
        steps.AddRange(GetComponentsInChildren<TutorialStep>(true));
    }

    private void BeginStep(int index)
    {
        currentIndex = index;
        TutorialStep step = steps[index];

        overlay.SetInstruction(step.Instruction, step.IsInfoOnly);
        overlay.SetHighlight(step.ResolveHighlightTarget, step.UiHighlightTarget);

        step.Completed += HandleStepCompleted;
        step.Begin();
    }

    private void HandleStepCompleted()
    {
        AdvanceStep();
    }

    private void AdvanceStep()
    {
        // Tính bước kế TRƯỚC khi hủy đăng ký (UnsubscribeCurrentStep đặt lại currentIndex = -1).
        int nextIndex = currentIndex + 1;
        UnsubscribeCurrentStep();

        if (nextIndex >= steps.Count)
        {
            Finish();
            return;
        }

        BeginStep(nextIndex);
    }

    // Nút Next chỉ dùng để qua bước "chỉ đọc"; bước tương tác phải làm đúng thao tác mới qua.
    private void HandleNextRequested()
    {
        bool onInfoStep = currentIndex >= 0 && steps[currentIndex].IsInfoOnly;
        if (onInfoStep)
        {
            AdvanceStep();
        }
    }

    private void HandleSkipRequested()
    {
        Finish();
    }

    private void Finish()
    {
        UnsubscribeCurrentStep();
        PlayerProgression.MarkTutorialSeen(tutorialId);
        HideOverlay();
        enabled = false;
    }

    private void UnsubscribeCurrentStep()
    {
        bool hasCurrentStep = currentIndex >= 0 && currentIndex < steps.Count;
        if (!hasCurrentStep)
        {
            return;
        }

        steps[currentIndex].Completed -= HandleStepCompleted;
        steps[currentIndex].End();
        currentIndex = -1;
    }

    private void HideOverlay()
    {
        if (overlay != null)
        {
            overlay.Hide();
        }
    }
}
