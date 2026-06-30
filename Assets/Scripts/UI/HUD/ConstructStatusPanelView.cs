using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cửa sổ Trạng thái Công trình: chuột phải vào một công trình trên bản đồ để mở. Bố cục từ trên xuống:
/// Tiêu đề (tên + cấp) -> Chỉ số (độ bền + sản lượng/huấn luyện) -> Thông tin khác (cấp/loại/mô tả)
/// -> Nâng cấp (lợi ích + chi phí + nút) -> Phá dỡ (nút + thông tin hoàn trả).
/// Đọc dữ liệu trực tiếp từ các thành phần trên công trình, không phụ thuộc loại cụ thể nhờ
/// <see cref="IConstructInfo"/>.
/// </summary>
public class ConstructStatusPanelView : MonoBehaviour
{
    [Header("Nguồn dữ liệu")]
    [SerializeField] private GridBuildingSystem buildingSystem;

    [Header("Khung cửa sổ")]
    [SerializeField] private GameObject windowRoot;
    [SerializeField] private Button closeButton;

    [Header("Tiêu đề")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Chỉ số")]
    [SerializeField] private Image durabilityFill;
    [SerializeField] private TMP_Text durabilityText;
    [SerializeField] private TMP_Text statsText;

    [Header("Thông tin khác")]
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Nâng cấp")]
    [SerializeField] private GameObject upgradeSection;
    [SerializeField] private TMP_Text upgradeBenefitText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private CanvasGroup upgradeButtonGroup;

    [Header("Phá dỡ")]
    [SerializeField] private Button demolishButton;
    [SerializeField] private TMP_Text refundText;

    [SerializeField] private float disabledAlpha = 0.45f;

    private static readonly Color HealthyColor = new Color(0.5f, 1f, 0.4f);
    private static readonly Color DamagedColor = new Color(1f, 0.35f, 0.3f);

    private PlacedObject currentObject;
    private BuildingDurability currentDurability;
    private BuildingUpgrade currentUpgrade;
    private IConstructInfo[] currentInfos;

    private void OnEnable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.ConstructInspectRequested += Show;
        }
        AddListener(closeButton, Hide);
        AddListener(upgradeButton, OnUpgradeClicked);
        AddListener(demolishButton, OnDemolishClicked);

        Hide();
    }

    private void OnDisable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.ConstructInspectRequested -= Show;
        }
        RemoveListener(closeButton, Hide);
        RemoveListener(upgradeButton, OnUpgradeClicked);
        RemoveListener(demolishButton, OnDemolishClicked);
    }

    private void Update()
    {
        // Số liệu động (độ bền, khả năng đủ tiền nâng cấp) có thể đổi liên tục khi đang mở.
        if (currentObject != null)
        {
            RefreshDynamic();
        }
    }

    public void Show(PlacedObject placedObject)
    {
        if (placedObject == null)
        {
            return;
        }

        currentObject = placedObject;
        currentDurability = placedObject.GetComponent<BuildingDurability>();
        currentUpgrade = placedObject.GetComponent<BuildingUpgrade>();
        currentInfos = placedObject.GetComponents<IConstructInfo>();

        SetWindowActive(true);
        RefreshStatic();
        RefreshDynamic();
    }

    public void Hide()
    {
        currentObject = null;
        SetWindowActive(false);
    }

    private void RefreshStatic()
    {
        PlacedObjectTypeSO type = currentObject.Type;

        SetText(nameText, type != null && !string.IsNullOrEmpty(type.nameString)
            ? type.nameString.ToUpperInvariant()
            : currentObject.name.ToUpperInvariant());
        SetText(typeText, $"Type: {ResolveTypeLabel()}");
        SetText(descriptionText, type != null ? type.description : string.Empty);
        SetText(statsText, BuildStatLines());
        SetText(refundText, BuildRefundLabel(type));
    }

    private void RefreshDynamic()
    {
        SetText(levelText, $"Level {(currentUpgrade != null ? currentUpgrade.CurrentLevel : 1)}");
        RefreshDurability();
        RefreshUpgradeSection();
    }

    private void RefreshDurability()
    {
        if (currentDurability == null)
        {
            SetText(durabilityText, "HP: --");
            if (durabilityFill != null)
            {
                durabilityFill.fillAmount = 1f;
            }
            return;
        }

        float fraction = currentDurability.Fraction;
        if (durabilityFill != null)
        {
            durabilityFill.fillAmount = fraction;
            durabilityFill.color = Color.Lerp(DamagedColor, HealthyColor, fraction);
        }
        int current = Mathf.RoundToInt(currentDurability.CurrentDurability);
        int max = Mathf.RoundToInt(currentDurability.MaxDurability);
        SetText(durabilityText, $"HP: {current} / {max}");
    }

    private void RefreshUpgradeSection()
    {
        bool canUpgrade = currentUpgrade != null && currentUpgrade.HasNextLevel;
        if (upgradeSection != null)
        {
            upgradeSection.SetActive(canUpgrade);
        }
        if (!canUpgrade)
        {
            return;
        }

        SetText(upgradeBenefitText, currentUpgrade.NextBenefitDescription);
        SetText(upgradeCostText, $"Cost: {currentUpgrade.NextGoldCost}G  {currentUpgrade.NextManaCost}M");

        bool affordable = currentUpgrade.CanAffordUpgrade();
        if (upgradeButton != null)
        {
            upgradeButton.interactable = affordable;
        }
        if (upgradeButtonGroup != null)
        {
            upgradeButtonGroup.alpha = affordable ? 1f : disabledAlpha;
        }
    }

    private string ResolveTypeLabel()
    {
        if (currentInfos != null)
        {
            foreach (IConstructInfo info in currentInfos)
            {
                if (!string.IsNullOrEmpty(info.TypeLabel))
                {
                    return info.TypeLabel;
                }
            }
        }
        return "Construct";
    }

    private string BuildStatLines()
    {
        if (currentInfos == null || currentInfos.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (IConstructInfo info in currentInfos)
        {
            foreach (string line in info.GetStatLines())
            {
                builder.AppendLine(line);
            }
        }
        return builder.ToString().TrimEnd();
    }

    private string BuildRefundLabel(PlacedObjectTypeSO type)
    {
        if (buildingSystem == null || type == null)
        {
            return string.Empty;
        }

        int gold = buildingSystem.GetGoldRefund(type);
        int mana = buildingSystem.GetManaRefund(type);
        int percent = Mathf.RoundToInt(buildingSystem.RefundFraction * 100f);
        return $"Demolishing refunds {percent}%: +{gold}G +{mana}M";
    }

    private void OnUpgradeClicked()
    {
        if (currentUpgrade != null && currentUpgrade.TryUpgrade())
        {
            RefreshStatic();
            RefreshDynamic();
        }
    }

    private void OnDemolishClicked()
    {
        if (currentObject != null)
        {
            buildingSystem.DemolishPlaced(currentObject);
        }
        Hide();
    }

    private void SetWindowActive(bool active)
    {
        if (windowRoot != null)
        {
            windowRoot.SetActive(active);
        }
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
}
