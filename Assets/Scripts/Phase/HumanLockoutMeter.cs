using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Theo dõi việc Human (phe Enemy) khống chế tinh thể Progress ở Phase 1 - đối trọng của
/// <see cref="PhaseManager"/>. Nếu Human giữ LIÊN TỤC TOÀN BỘ tinh thể Progress trong
/// <see cref="lossDurationSeconds"/> giây, coi như thua và về sảnh chờ (không qua Phase 2).
/// Nếu Human chỉ giữ MỘT PHẦN, thanh thua không đổi nhưng tốc độ tích thanh Thức tỉnh của người
/// chơi (<see cref="ProgressCrystalChannel"/>) bị giảm còn <see cref="partialLockoutRateMultiplier"/>.
/// Khi người chơi đang thực sự kênh (giữ) ít nhất một tinh thể Progress, thanh thua giảm dần thay vì
/// đứng yên hay reset ngay lập tức - xem <see cref="RecoverLoss"/>.
/// </summary>
public class HumanLockoutMeter : MonoBehaviour
{
    public static HumanLockoutMeter Instance { get; private set; }

    [Header("Khống chế toàn bộ -> thua")]
    [Tooltip("Số giây Human giữ liên tục TOÀN BỘ tinh thể Progress thì thanh thua đầy 100%.")]
    [SerializeField, Min(0.01f)] private float lossDurationSeconds = 120f;

    [Tooltip("Số giây để thanh thua giảm từ 100% về 0% khi người chơi đang giữ lại một tinh thể Progress.")]
    [SerializeField, Min(0.01f)] private float lossRecoverySeconds = 120f;

    [Header("Khống chế một phần -> giảm tốc Thức tỉnh")]
    [Tooltip("Hệ số nhân tốc độ tích thanh Thức tỉnh khi Human giữ MỘT PHẦN (không phải tất cả) " +
        "tinh thể Progress. 0.4 = còn 40% tốc độ, bất kể giữ bao nhiêu cục.")]
    [SerializeField, Range(0f, 1f)] private float partialLockoutRateMultiplier = 0.4f;

    [Header("Kết quả khi thua")]
    [Tooltip("Panel kết quả (ẩn sẵn) hiện khi thua Phase 1.")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("Nhãn chữ trên panel kết quả.")]
    [SerializeField] private TMP_Text resultLabel;

    [Tooltip("Thời gian hiện kết quả trước khi về sảnh chờ (giây).")]
    [SerializeField, Min(0f)] private float returnDelaySeconds = 3f;

    [Tooltip("Tên scene sảnh chờ - phải có trong Build Settings.")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private const string DefeatMessage = "DEFEAT - The Humans have overrun every Progress Crystal.";

    private bool hasTriggeredLoss;

    public float LossProgress01 { get; private set; }
    public float PlayerProgressRateMultiplier { get; private set; } = 1f;

    /// <summary>Bắn khi tiến độ thua đổi (0..1).</summary>
    public event Action<float> LossProgressChanged;

    /// <summary>Bắn đúng một lần khi Phase 1 thua.</summary>
    public event Action PhaseOneLost;

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
        LossProgressChanged?.Invoke(LossProgress01);
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
        if (hasTriggeredLoss)
        {
            return;
        }

        CountProgressCrystals(out int total, out int captured, out bool playerHoldingAny);
        UpdateRateMultiplier(total, captured);
        UpdateLossProgress(total, captured, playerHoldingAny);
    }

    // Human giữ = trạng thái Captured (CrystalCaptureZone tự Reactivate ngay khi hết địch trong
    // vùng, nên State == Captured luôn là tín hiệu "còn đang bị giữ", không cần theo dõi thêm).
    // Người chơi giữ = đang thực sự kênh tiến trình (occupancy còn sống), không chỉ State == Active.
    private static void CountProgressCrystals(out int total, out int captured, out bool playerHoldingAny)
    {
        total = 0;
        captured = 0;
        playerHoldingAny = false;

        foreach (CrystalNode node in CrystalNodeRegistry.All)
        {
            if (node == null || node.Type != CrystalType.Progress)
            {
                continue;
            }

            total++;

            if (node.State == CrystalState.Captured)
            {
                captured++;
                continue;
            }

            ProgressCrystalChannel channel = node.GetComponent<ProgressCrystalChannel>();
            if (channel != null && channel.IsChanneling)
            {
                playerHoldingAny = true;
            }
        }
    }

    private void UpdateRateMultiplier(int total, int captured)
    {
        bool isPartialLockout = captured > 0 && captured < total;
        PlayerProgressRateMultiplier = isPartialLockout ? partialLockoutRateMultiplier : 1f;
    }

    private void UpdateLossProgress(int total, int captured, bool playerHoldingAny)
    {
        bool isFullLockout = total > 0 && captured == total;
        if (isFullLockout)
        {
            AccumulateLoss();
        }
        else if (playerHoldingAny)
        {
            RecoverLoss();
        }
    }

    private void AccumulateLoss()
    {
        SetLossProgress(LossProgress01 + Time.deltaTime / lossDurationSeconds);

        if (LossProgress01 >= 1f)
        {
            TriggerLoss();
        }
    }

    private void RecoverLoss()
    {
        if (LossProgress01 <= 0f)
        {
            return;
        }

        SetLossProgress(LossProgress01 - Time.deltaTime / lossRecoverySeconds);
    }

    private void SetLossProgress(float value)
    {
        float clamped = Mathf.Clamp01(value);
        if (Mathf.Approximately(clamped, LossProgress01))
        {
            return;
        }

        LossProgress01 = clamped;
        LossProgressChanged?.Invoke(LossProgress01);
    }

    private void TriggerLoss()
    {
        hasTriggeredLoss = true;
        ShowResult(DefeatMessage);
        PhaseOneLost?.Invoke();
        StartCoroutine(ReturnToLobbyAfterDelay());
    }

    private void ShowResult(string message)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultLabel != null)
        {
            resultLabel.text = message;
        }
    }

    private IEnumerator ReturnToLobbyAfterDelay()
    {
        yield return new WaitForSeconds(returnDelaySeconds);
        SceneManager.LoadScene(lobbySceneName);
    }
}
