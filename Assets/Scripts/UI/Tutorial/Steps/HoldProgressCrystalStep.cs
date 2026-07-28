using UnityEngine;

/// <summary>
/// Bước hoàn thành khi thanh Awakening bắt đầu tăng - tức người chơi đã kích hoạt và đang giữ tinh
/// thể Progress (nghe <see cref="PhaseManager.ProgressChanged"/>).
/// </summary>
public class HoldProgressCrystalStep : TutorialStep
{
    [Tooltip("Ngưỡng tiến trình (0..1) coi là đã bắt đầu giữ tinh thể Progress.")]
    [SerializeField] private float requiredProgress01 = 0.01f;

    private PhaseManager phaseManager;

    // Trỏ mũi tên vào tinh thể Progress (tím) để người chơi tìm thấy mục tiêu thắng, bất kể trạng thái.
    public override Transform ResolveHighlightTarget()
    {
        return FindNearestCrystalTransform(CrystalType.Progress, null);
    }

    protected override void StartWatching()
    {
        phaseManager = FindFirstObjectByType<PhaseManager>();
        if (phaseManager != null)
        {
            phaseManager.ProgressChanged += HandleProgressChanged;
        }
    }

    protected override void StopWatching()
    {
        if (phaseManager != null)
        {
            phaseManager.ProgressChanged -= HandleProgressChanged;
        }
    }

    private void HandleProgressChanged(float progress01)
    {
        if (progress01 >= requiredProgress01)
        {
            NotifyComplete();
        }
    }
}
