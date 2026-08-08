using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý trạng thái Tạm dừng (Pause) của gameplay. Bấm ESC bật/tắt: đóng băng thời gian
/// (<see cref="Time.timeScale"/>) và bắn sự kiện cho UI hiển thị bảng menu.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    /// <summary>Đang tạm dừng hay không - static để các script gameplay khác chặn input mà không cần giữ tham chiếu Instance.</summary>
    public static bool IsPaused { get; private set; }

    [Tooltip("Tên scene Sảnh chờ - phải có trong Build Settings.")]
    [SerializeField] private string lobbySceneName = "Lobby";

    /// <summary>Bắn mỗi khi trạng thái Pause đổi (true = đang tạm dừng).</summary>
    public event Action<bool> PauseStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Mỗi scene mới bắt đầu ở trạng thái không Pause - phòng trường hợp static rò rỉ
        // giá trị cũ khi Editor tắt Reload Domain giữa các lần Play.
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Phòng trường hợp scene đổi khi đang Pause - không để timeScale = 0 rò rỉ sang scene sau.
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void ReturnToLobby()
    {
        SetPaused(false);

        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        PauseStateChanged?.Invoke(paused);
    }
}
