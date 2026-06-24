using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string GameplaySceneName = "Phase 1";

    public void PlayGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
