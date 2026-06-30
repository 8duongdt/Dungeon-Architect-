using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng thông tin đơn vị (góc trái bảng điều khiển): chân dung, tên, máu, sát thương của đơn vị đang
/// chọn. Khi không chọn gì thì hiển thị Commander mặc định. Nút mũi tên thu gọn/mở bảng.
/// </summary>
public class UnitInfoPanelView : MonoBehaviour
{
    [Header("Nguồn dữ liệu")]
    [SerializeField] private UnitController unitController;

    [Tooltip("Thông tin Commander hiển thị mặc định khi không chọn đơn vị nào.")]
    [SerializeField] private HudDisplayInfo commanderInfo;

    [Header("Widget hiển thị")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text attackText;

    [Header("Thu gọn")]
    [SerializeField] private GameObject collapsibleContent;
    [SerializeField] private Button collapseButton;

    private HudDisplayInfo currentInfo;
    private bool isCollapsed;

    private void OnEnable()
    {
        if (unitController != null)
        {
            unitController.SelectionChanged += OnSelectionChanged;
        }
        if (collapseButton != null)
        {
            collapseButton.onClick.AddListener(ToggleCollapsed);
        }

        ShowCommanderDefault();
    }

    private void OnDisable()
    {
        if (unitController != null)
        {
            unitController.SelectionChanged -= OnSelectionChanged;
        }
        if (collapseButton != null)
        {
            collapseButton.onClick.RemoveListener(ToggleCollapsed);
        }
    }

    private void Update()
    {
        // Máu/sát thương có thể đổi liên tục trong giao tranh -> cập nhật lại nhãn động.
        RefreshDynamicStats();
    }

    private void OnSelectionChanged(IReadOnlyList<Unit> selectedUnits)
    {
        HudDisplayInfo info = GetFirstDisplayInfo(selectedUnits);
        if (info != null)
        {
            Display(info);
        }
        else
        {
            ShowCommanderDefault();
        }
    }

    private static HudDisplayInfo GetFirstDisplayInfo(IReadOnlyList<Unit> selectedUnits)
    {
        if (selectedUnits == null)
        {
            return null;
        }

        foreach (Unit unit in selectedUnits)
        {
            if (unit == null)
            {
                continue;
            }

            HudDisplayInfo info = unit.GetComponent<HudDisplayInfo>();
            if (info != null)
            {
                return info;
            }
        }
        return null;
    }

    private void ShowCommanderDefault()
    {
        if (commanderInfo != null)
        {
            Display(commanderInfo);
        }
    }

    private void Display(HudDisplayInfo info)
    {
        currentInfo = info;

        if (portraitImage != null)
        {
            portraitImage.sprite = info.Portrait;
            portraitImage.enabled = info.Portrait != null;
        }
        if (nameText != null)
        {
            nameText.text = info.DisplayName;
        }

        RefreshDynamicStats();
    }

    private void RefreshDynamicStats()
    {
        if (currentInfo == null)
        {
            return;
        }

        if (healthText != null)
        {
            int current = Mathf.RoundToInt(currentInfo.CurrentHealth);
            int max = Mathf.RoundToInt(currentInfo.MaxHealth);
            healthText.text = $"HEALTH: {current}/{max}";
        }
        if (attackText != null)
        {
            attackText.text = $"ATTACK: {Mathf.RoundToInt(currentInfo.AttackValue)}";
        }
    }

    private void ToggleCollapsed()
    {
        isCollapsed = !isCollapsed;
        if (collapsibleContent != null)
        {
            collapsibleContent.SetActive(!isCollapsed);
        }
    }
}
