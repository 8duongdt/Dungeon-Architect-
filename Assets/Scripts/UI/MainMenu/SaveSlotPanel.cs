using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bảng chọn slot save (mặc định ẩn), mở từ MainMenu khi bấm "Play". Quét cả
/// <see cref="SaveSystem.SlotCount"/> slot và hiển thị tóm tắt lên từng ô:
///   - Slot đã có save  -> bấm để Tiếp tục (nạp slot đó rồi vào Lobby).
///   - Slot trống       -> bấm để New Game (tạo save mặc định rồi chiếu cốt truyện, vào Phase 1).
/// </summary>
[DisallowMultipleComponent]
public class SaveSlotPanel : MonoBehaviour
{
    private const string StoryIntroSceneName = "StoryIntro";
    private const string LobbySceneName = "Lobby";

    [Tooltip("Gốc bảng slot - bật/tắt để hiện/ẩn.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("6 ô slot, thứ tự tương ứng slot 1..6.")]
    [SerializeField] private SaveSlotView[] slotViews;

    [Tooltip("Nút đóng bảng, quay lại menu chính.")]
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Hide);
        }

        Hide();
    }

    public void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        RefreshSlots();
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void RefreshSlots()
    {
        if (slotViews == null)
        {
            return;
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            SaveSlotView view = slotViews[i];
            if (view == null)
            {
                continue;
            }

            int slot = i + 1;
            view.Bind(slot, OnSlotClicked, OnSlotDeleteClicked, SaveSystem.Exists(slot));
            view.SetSummary(BuildSummary(slot));
        }
    }

    private string BuildSummary(int slot)
    {
        if (!SaveSystem.Exists(slot))
        {
            return $"Slot {slot}\n＋ Empty";
        }

        SaveData data = SaveSystem.Load(slot);
        string phaseLabel = data.currentPhase >= 1 ? $"Phase {data.currentPhase}" : "Not started";
        string timeLine = string.IsNullOrEmpty(data.lastSaved) ? string.Empty : $"\n{data.lastSaved}";
        return $"Slot {slot}\n{phaseLabel}\nSP{data.skillPoints} · BP{data.buildingPoints}{timeLine}";
    }

    private void OnSlotClicked(int slot)
    {
        if (SaveSystem.Exists(slot))
        {
            LoadExistingSlot(slot);
        }
        else
        {
            StartNewSlot(slot);
        }
    }

    // Bấm nút X trên một ô: xóa hẳn file save của slot đó rồi vẽ lại bảng (ô về "Empty").
    private void OnSlotDeleteClicked(int slot)
    {
        PlayerProgression.DeleteSlot(slot);
        RefreshSlots();
    }

    private void LoadExistingSlot(int slot)
    {
        PlayerProgression.SelectSlot(slot);
        SceneManager.LoadScene(LobbySceneName);
    }

    private void StartNewSlot(int slot)
    {
        PlayerProgression.CreateNewSlot(slot);
        // Ghi checkpoint Phase 1 ngay để nếu người chơi thoát giữa cốt truyện, slot vẫn tiếp tục được.
        PlayerProgression.CurrentPhase = 1;
        SceneManager.LoadScene(StoryIntroSceneName);
    }
}
