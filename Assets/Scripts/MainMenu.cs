using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string StoryIntroSceneName = "StoryIntro";

    public void PlayGame()
    {
        SceneManager.LoadScene(StoryIntroSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
