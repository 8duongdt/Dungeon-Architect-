using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sinh các nút UI để chọn công trình: nhân bản một button mẫu cho mỗi loại trong danh sách, gán icon
/// và nối onClick vào <see cref="GridBuildingSystem.SetActiveBuildingType"/>.
/// </summary>
public class BuildingMenuUI : MonoBehaviour
{
    [SerializeField]
    private GridBuildingSystem buildingSystem;

    [Tooltip("Nút mẫu (đặt sẵn trong Canvas, có thể để ẩn) sẽ được nhân bản cho mỗi công trình.")]
    [SerializeField]
    private Button buttonTemplate;

    [SerializeField]
    private List<PlacedObjectTypeSO> types = new List<PlacedObjectTypeSO>();

    private void Start()
    {
        if (buildingSystem == null || buttonTemplate == null)
        {
            return;
        }

        buttonTemplate.gameObject.SetActive(false);
        foreach (PlacedObjectTypeSO type in types)
        {
            CreateButton(type);
        }
    }

    private void CreateButton(PlacedObjectTypeSO type)
    {
        Button button = Instantiate(buttonTemplate, buttonTemplate.transform.parent);
        button.gameObject.SetActive(true);
        button.name = $"Button_{type.nameString}";

        SetButtonIcon(button, type.icon);
        button.onClick.AddListener(() => buildingSystem.SetActiveBuildingType(type));
    }

    // Gán icon vào Image con đầu tiên không phải nền của chính button (nếu có).
    private void SetButtonIcon(Button button, Sprite icon)
    {
        if (icon == null)
        {
            return;
        }

        Image targetImage = button.GetComponent<Image>();
        if (targetImage != null)
        {
            targetImage.sprite = icon;
        }
    }
}
