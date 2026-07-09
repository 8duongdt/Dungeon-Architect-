using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string StoryIntroSceneName = "StoryIntro";
    private const string LobbySceneName = "Lobby";

    /// <summary>Lần đầu chơi thì chiếu cốt truyện; các lần sau vào thẳng sảnh chờ.</summary>
    public void PlayGame()
    {
        if (!PlayerProgression.HasSeenIntro)
        {
            PlayerProgression.HasSeenIntro = true;
            SceneManager.LoadScene(StoryIntroSceneName);
            return;
        }

        SceneManager.LoadScene(LobbySceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
