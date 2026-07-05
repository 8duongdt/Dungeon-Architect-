using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Một ô triệu hồi lính trong Cửa sổ Trạng thái của trại huấn luyện: avatar lính, chi phí Vàng/Mana
/// bên dưới, lớp phủ tối quét tròn (radial) thể hiện thời gian hồi, và badge góc trên-phải đếm số
/// con đang trong hàng chờ. Trái chuột = trừ tiền trước + xếp hàng; phải chuột = hủy một con + hoàn
/// tiền. Dùng IPointerClickHandler thay vì Button vì Button chỉ bắt trái chuột.
/// </summary>
public class TrainingSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text queueBadgeText;
    [SerializeField] private GameObject queueBadgeRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Độ mờ khi ô bị vô hiệu (chưa mở khóa / không đủ tài nguyên / chạm giới hạn).")]
    [SerializeField] private float disabledAlpha = 0.45f;

    private UnitTrainingBuilding training;
    private int unitIndex;

    /// <summary>Nối ô với một loại lính của trại - gọi mỗi lần mở cửa sổ cho một trại khác.</summary>
    public void Bind(UnitTrainingBuilding trainingBuilding, int index)
    {
        training = trainingBuilding;
        unitIndex = index;

        UnitTrainingBuilding.TrainableUnit unit = training.TrainableUnits[index];
        if (avatarImage != null)
        {
            Sprite avatar = ResolveAvatar(unit);
            avatarImage.sprite = avatar;
            avatarImage.enabled = avatar != null;
        }
        if (costText != null)
        {
            costText.text = BuildCostLabel(unit);
        }
        Refresh();
    }

    /// <summary>Cập nhật trạng thái động (cooldown, badge, làm mờ) - panel gọi mỗi khung hình khi mở.</summary>
    public void Refresh()
    {
        if (training == null)
        {
            return;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = training.CooldownFraction;
        }

        int queued = training.QueuedCount(unitIndex);
        if (queueBadgeRoot != null)
        {
            queueBadgeRoot.SetActive(queued > 0);
        }
        if (queueBadgeText != null)
        {
            queueBadgeText.text = queued.ToString();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = training.CanEnqueue(unitIndex) ? 1f : disabledAlpha;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (training == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            training.TryEnqueue(unitIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            training.TryCancelOne(unitIndex);
        }
        Refresh();
    }

    // Avatar ưu tiên field portrait; chưa gán thì mượn luôn sprite trên prefab lính để ô không trống.
    private static Sprite ResolveAvatar(UnitTrainingBuilding.TrainableUnit unit)
    {
        if (unit.portrait != null)
        {
            return unit.portrait;
        }

        SpriteRenderer renderer = unit.prefab != null
            ? unit.prefab.GetComponentInChildren<SpriteRenderer>()
            : null;
        return renderer != null ? renderer.sprite : null;
    }

    private static string BuildCostLabel(UnitTrainingBuilding.TrainableUnit unit)
    {
        return unit.manaCost > 0 ? $"{unit.goldCost}G {unit.manaCost}M" : $"{unit.goldCost}G";
    }
}
