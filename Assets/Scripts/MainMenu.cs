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
    /// New Game: xóa sạch toàn bộ tiến trình đã lưu (điểm/bậc skill, công trình, ô trang bị) rồi
    /// bắt đầu lại từ đầu - luôn chiếu lại cốt truyện vì HasSeenIntro cũng bị reset.
    /// </summary>
    public void PlayGame()
    {
        PlayerProgression.ResetAll();
        PlayerProgression.CurrentPhase = 1;
        PlayerProgression.HasSeenIntro = true;
        SceneManager.LoadScene(StoryIntroSceneName);
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
