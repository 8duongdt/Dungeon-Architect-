using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trọng tài của TRẬN ĐÁNH PHASE 2 - định nghĩa thắng/thua của phase:
///   THẮNG: mọi cổng sinh quái đã cạn hạn ngạch VÀ không còn quái sống
///          -> thưởng điểm kỹ năng rồi về sảnh chờ.
///   THUA:  người chơi chết -> về sảnh chờ tay trắng.
/// Kiểm tra thắng theo nhịp thưa (không cần mỗi frame); kết quả hiện trên panel HUD
/// một khoảng ngắn trước khi chuyển scene. Scene-local, không singleton.
/// </summary>
public class Phase2Director : MonoBehaviour
{
    private const float WinCheckInterval = 0.5f;

    [Tooltip("Máu của người chơi - chết là thua.")]
    [SerializeField] private UnitHealth playerHealth;

    [Tooltip("Panel kết quả (ẩn sẵn) hiện khi trận kết thúc.")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("Nhãn chữ trên panel kết quả.")]
    [SerializeField] private TMP_Text resultLabel;

    [Tooltip("Điểm kỹ năng thưởng khi thắng Phase 2.")]
    [SerializeField, Min(0)] private int winSkillPointReward = 2;

    [Tooltip("Thời gian hiện kết quả trước khi về sảnh chờ (giây).")]
    [SerializeField, Min(0f)] private float returnDelaySeconds = 3f;

    [Tooltip("Tên scene sảnh chờ - phải có trong Build Settings.")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private EnemySpawner[] spawners;
    private float nextWinCheckTime;
    private bool hasEnded;

    private void Start()
    {
        spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }
    }

    private void Update()
    {
        bool shouldCheckWin = !hasEnded && Time.time >= nextWinCheckTime;
        if (!shouldCheckWin)
        {
            return;
        }

        nextWinCheckTime = Time.time + WinCheckInterval;
        if (AllEnemiesDefeated())
        {
            EndBattle($"CHIẾN THẮNG!\n+{winSkillPointReward} điểm kỹ năng", winSkillPointReward);
        }
    }

    /// <summary>Thắng khi có ít nhất một cổng, mọi cổng đã sinh đủ hạn ngạch và sạch bóng quái.</summary>
    private bool AllEnemiesDefeated()
    {
        if (spawners == null || spawners.Length == 0)
        {
            return false;
        }

        foreach (EnemySpawner spawner in spawners)
        {
            bool spawnerStillActive = !spawner.HasFinishedSpawning || spawner.AliveEnemyCount > 0;
            if (spawnerStillActive)
            {
                return false;
            }
        }

        return true;
    }

    private void OnPlayerDied(UnitHealth health)
    {
        EndBattle("THẤT BẠI...", 0);
    }

    private void EndBattle(string resultMessage, int skillPointReward)
    {
        if (hasEnded)
        {
            return;
        }

        hasEnded = true;
        PlayerProgression.AddSkillPoints(skillPointReward);
        ShowResult(resultMessage);
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
