using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng menu Tạm dừng: hiện/ẩn theo <see cref="PauseManager"/> và chuyển tiếp click nút
/// sang các hành động Tiếp tục / Về Sảnh chờ / Thoát game.
/// </summary>
public class PauseMenuView : MonoBehaviour
{
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (pauseManager == null)
        {
            pauseManager = PauseManager.Instance;
        }
    }

    private void OnEnable()
    {
        if (pauseManager == null)
        {
            pauseManager = PauseManager.Instance;
        }

        if (pauseManager == null)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            return;
        }

        OnPauseStateChanged(PauseManager.IsPaused);

        pauseManager.PauseStateChanged += OnPauseStateChanged;
        BindButtons();
    }

    private void OnDisable()
    {
        if (pauseManager == null)
        {
            return;
        }

        pauseManager.PauseStateChanged -= OnPauseStateChanged;
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(pauseManager.Resume);
        }
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.AddListener(pauseManager.ReturnToLobby);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(pauseManager.QuitGame);
        }
    }

    private void UnbindButtons()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(pauseManager.Resume);
        }
        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.RemoveListener(pauseManager.ReturnToLobby);
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(pauseManager.QuitGame);
        }
    }

    private void OnPauseStateChanged(bool paused)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(paused);
        }
    }
}
