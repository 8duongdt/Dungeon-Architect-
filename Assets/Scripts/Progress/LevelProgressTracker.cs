using System;
using UnityEngine;

/// <summary>
/// Thanh tiến độ màn chơi/wave, tăng khi người chơi kênh (đứng trong) tinh thể Progress đang
/// Active (xem <see cref="ProgressCrystalChannel"/>). KHÔNG phải tài nguyên tiêu được như
/// Vàng/Mana (<see cref="ResourceManager"/>) - đây chỉ là một thước đo tiến trình.
///
/// Việc đạt 100% sẽ làm gì (mở wave kế tiếp, mở khoá gì đó...) CHƯA được nối dây ở đây - đây là
/// điểm mở rộng để lượt sau quyết định, cứ nghe sự kiện <see cref="ProgressCompleted"/>.
///
/// Singleton tối giản theo đúng mẫu <see cref="ResourceManager"/>.
/// </summary>
public class LevelProgressTracker : MonoBehaviour
{
    public static LevelProgressTracker Instance { get; private set; }

    [SerializeField] private float maxProgress = 100f;

    public float CurrentProgress { get; private set; }
    public float ProgressFraction => maxProgress > 0f ? CurrentProgress / maxProgress : 0f;

    /// <summary>Bắn khi tiến trình đổi (giá trị mới).</summary>
    public event Action<float> ProgressChanged;

    /// <summary>Bắn đúng một lần khi tiến trình chạm mức tối đa.</summary>
    public event Action ProgressCompleted;

    private bool hasCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddProgress(float amount)
    {
        if (amount <= 0f || hasCompleted)
        {
            return;
        }

        CurrentProgress = Mathf.Min(maxProgress, CurrentProgress + amount);
        ProgressChanged?.Invoke(CurrentProgress);

        if (CurrentProgress >= maxProgress)
        {
            hasCompleted = true;
            ProgressCompleted?.Invoke();
        }
    }
}
