using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string StoryIntroSceneName = "StoryIntro";
    private const string LobbySceneName = "Lobby";

    [Tooltip("Nút 'Tiếp tục' - chỉ hiện khi có save (đã bắt đầu ít nhất một phase).")]
    [SerializeField] private GameObject continueButton;

    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.SetActive(PlayerProgression.HasSave);
        }
    }

    /// <summary>
    /// Chơi mới: reset checkpoint về Phase 1 (skill/tiến trình vẫn giữ). Lần đầu chiếu cốt truyện;
    /// các lần sau vào thẳng sảnh chờ.
    /// </summary>
    public void PlayGame()
    {
        PlayerProgression.CurrentPhase = 1;

        if (!PlayerProgression.HasSeenIntro)
        {
            PlayerProgression.HasSeenIntro = true;
            SceneManager.LoadScene(StoryIntroSceneName);
            return;
        }

        SceneManager.LoadScene(LobbySceneName);
    }

    /// <summary>Tiếp tục: giữ nguyên checkpoint, vào Lobby để chọn skill rồi Start vào phase đã lưu.</summary>
    public void ContinueGame()
    {
        SceneManager.LoadScene(LobbySceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
