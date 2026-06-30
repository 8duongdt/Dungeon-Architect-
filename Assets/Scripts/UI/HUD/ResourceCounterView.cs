using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô hiển thị tài nguyên ở góc trên màn hình: icon + số đếm. Vàng và Mana đều không giới hạn nên
/// chỉ hiện một con số. Chỉ là "view" - nhận giá trị từ <see cref="ResourceHudBinder"/>.
/// </summary>
public class ResourceCounterView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;

    /// <summary>Cập nhật số đếm tài nguyên.</summary>
    public void SetAmount(int amount)
    {
        if (valueText != null)
        {
            valueText.text = amount.ToString();
        }
    }
}
