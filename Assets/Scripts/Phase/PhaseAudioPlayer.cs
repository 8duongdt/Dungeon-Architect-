using UnityEngine;

/// <summary>
/// Phát các "cú nhấn" âm thanh lớn của một phase: dungeon sinh xong, chuyển Phase 2, thắng/thua
/// trận Phase 2. Mỗi tham chiếu đều tùy chọn (null-check) để CÙNG một component dùng được ở
/// nhiều scene khác nhau - scene nào không có nguồn sự kiện tương ứng thì bỏ trống field đó.
/// </summary>
[DisallowMultipleComponent]
public class PhaseAudioPlayer : MonoBehaviour
{
    [Tooltip("Gán trong scene Phase 1 - phát tiếng khi dungeon sinh xong.")]
    [SerializeField] private DungeonManager dungeonManager;

    [Tooltip("Gán trong scene Phase 1 - phát tiếng khi thức tỉnh đạt 100% (chuyển Phase 2).")]
    [SerializeField] private PhaseManager phaseManager;

    [Tooltip("Gán trong scene Phase 2 - phát tiếng khi trận kết thúc (thắng/thua).")]
    [SerializeField] private Phase2Director phase2Director;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip dungeonReadyClip;
    [SerializeField] private AudioClip phaseTransitionClip;
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;

    private void OnEnable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated += HandleDungeonGenerated;
        }

        if (phaseManager != null)
        {
            phaseManager.PhaseTwoTriggered += HandlePhaseTwoTriggered;
        }

        if (phase2Director != null)
        {
            phase2Director.BattleEnded += HandleBattleEnded;
        }
    }

    private void OnDisable()
    {
        if (dungeonManager != null)
        {
            dungeonManager.DungeonGenerated -= HandleDungeonGenerated;
        }

        if (phaseManager != null)
        {
            phaseManager.PhaseTwoTriggered -= HandlePhaseTwoTriggered;
        }

        if (phase2Director != null)
        {
            phase2Director.BattleEnded -= HandleBattleEnded;
        }
    }

    private void HandleDungeonGenerated()
    {
        AudioManager.Instance.PlayOneShot(dungeonReadyClip);
    }

    private void HandlePhaseTwoTriggered()
    {
        AudioManager.Instance.PlayOneShot(phaseTransitionClip);
    }

    private void HandleBattleEnded(bool isVictory)
    {
        AudioManager.Instance.PlayOneShot(isVictory ? victoryClip : defeatClip);
    }
}
