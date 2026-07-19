using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Bảng chọn slot save - nút 'Play' mở bảng này.")]
    [SerializeField] private SaveSlotPanel saveSlotPanel;

    /// <summary>
    /// Play: mở bảng chọn slot. Trong bảng: ô đã có save = Tiếp tục, ô trống = New Game.
    /// Toàn bộ luồng New Game/Continue giờ nằm trong <see cref="SaveSlotPanel"/>.
    /// </summary>
    public void OpenSlotPanel()
    {
        if (saveSlotPanel != null)
        {
            saveSlotPanel.Show();
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
