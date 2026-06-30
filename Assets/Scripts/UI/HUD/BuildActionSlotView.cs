using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Một ô lệnh xây dựng trên bảng điều khiển: nền ô đồ, icon công trình, phím tắt, tên và chi phí.
/// Bấm ô (hoặc phím tắt) sẽ chọn loại công trình để xây. Có thể làm mờ khi không đủ tài nguyên hoặc
/// chưa có asset (placeholder).
/// </summary>
public class BuildActionSlotView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Độ mờ khi ô bị vô hiệu (không đủ tài nguyên / chưa có asset).")]
    [SerializeField] private float disabledAlpha = 0.45f;

    public PlacedObjectTypeSO Type { get; private set; }

    public void Bind(PlacedObjectTypeSO type, UnityAction onClick)
    {
        Type = type;

        if (iconImage != null)
        {
            iconImage.sprite = type.icon;
            iconImage.enabled = type.icon != null;
        }
        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(type.nameString) ? name : type.nameString.ToUpperInvariant();
        }
        if (hotkeyText != null)
        {
            hotkeyText.text = type.hotkey;
        }
        if (costText != null)
        {
            costText.text = BuildCostLabel(type);
        }
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }

    /// <summary>Bật/tắt khả năng tương tác + làm mờ ô.</summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? 1f : disabledAlpha;
        }
    }

    private static string BuildCostLabel(PlacedObjectTypeSO type)
    {
        bool hasGold = type.goldCost > 0;
        bool hasMana = type.manaCost > 0;

        if (hasGold && hasMana)
        {
            return $"{type.goldCost}G  {type.manaCost}M";
        }
        if (hasMana)
        {
            return $"COST: {type.manaCost}";
        }
        return $"COST: {type.goldCost}";
    }
}
