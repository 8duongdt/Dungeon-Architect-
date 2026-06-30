using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Theo dõi tiến độ "thức tỉnh" của nhân vật chính (0..1). Khi đạt 100% thì chuyển sang Phase 2 -
/// nhân vật giải phóng toàn bộ sức mạnh.
///
/// Singleton tối giản để nguồn tăng tiến độ (cỗ máy thức tỉnh - chưa làm) có thể gọi tới từ bất kỳ
/// đâu. Hiện cung cấp sẵn API <see cref="AddProgress"/> và phím debug để test thanh tiến trình.
/// </summary>
public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    [Header("Chuyển Phase")]
    [Tooltip("Tên scene Phase 2 - phải có trong Build Settings.")]
    [SerializeField] private string phaseTwoSceneName = "Phase 2";

    [Header("Debug")]
    [Tooltip("Bật để tăng tiến độ bằng phím (test nhanh khi chưa có cỗ máy thức tỉnh).")]
    [SerializeField] private bool enableDebugKey = true;
    [SerializeField] private float debugStepPerPress = 0.1f;

    private bool hasTriggeredPhaseTwo;

    public float Progress01 { get; private set; }

    /// <summary>Bắn khi tiến độ đổi (0..1).</summary>
    public event Action<float> ProgressChanged;

    /// <summary>Bắn đúng một lần khi tiến độ đạt 100%.</summary>
    public event Action PhaseTwoTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ProgressChanged?.Invoke(Progress01);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!enableDebugKey || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            AddProgress(debugStepPerPress);
        }
    }

    /// <summary>Cộng tiến độ thức tỉnh (giá trị 0..1). Tới 1 thì kích hoạt Phase 2.</summary>
    public void AddProgress(float amount01)
    {
        if (hasTriggeredPhaseTwo || amount01 <= 0f)
        {
            return;
        }

        Progress01 = Mathf.Clamp01(Progress01 + amount01);
        ProgressChanged?.Invoke(Progress01);

        if (Progress01 >= 1f)
        {
            TriggerPhaseTwo();
        }
    }

    public void ResetProgress()
    {
        Progress01 = 0f;
        hasTriggeredPhaseTwo = false;
        ProgressChanged?.Invoke(Progress01);
    }

    private void TriggerPhaseTwo()
    {
        hasTriggeredPhaseTwo = true;
        PhaseTwoTriggered?.Invoke();

        if (!string.IsNullOrEmpty(phaseTwoSceneName))
        {
            SceneManager.LoadScene(phaseTwoSceneName);
        }
    }
}
