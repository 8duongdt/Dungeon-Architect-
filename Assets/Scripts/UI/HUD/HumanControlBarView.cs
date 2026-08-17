using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh cảnh báo "Human đang khống chế" màu đỏ - đối trọng của <see cref="AwakeningBarView"/>.
/// Đọc tiến độ từ <see cref="HumanLockoutMeter"/> và cập nhật ảnh fill + nhãn phần trăm. Ảnh fill
/// nên đặt Image Type = Filled (Horizontal). GameObject này ẩn theo mặc định trong scene - tự bật
/// lên qua Inspector khi cần xem, không cần phím tắt.
/// </summary>
public class HumanControlBarView : MonoBehaviour
{
    [SerializeField] private HumanLockoutMeter lossMeter;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [Tooltip("Hậu tố nhãn phần trăm.")]
    [SerializeField] private string percentSuffix = "% CONTROLLED";

    private void Awake()
    {
        if (lossMeter == null)
        {
            lossMeter = HumanLockoutMeter.Instance;
        }
    }

    private void OnEnable()
    {
        if (lossMeter == null)
        {
            lossMeter = HumanLockoutMeter.Instance;
        }
        if (lossMeter == null)
        {
            return;
        }

        lossMeter.LossProgressChanged += OnLossProgressChanged;
        OnLossProgressChanged(lossMeter.LossProgress01);
    }

    private void OnDisable()
    {
        if (lossMeter == null)
        {
            return;
        }

        lossMeter.LossProgressChanged -= OnLossProgressChanged;
    }

    private void OnLossProgressChanged(float progress01)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(progress01);
        }

        if (percentText != null)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f);
            percentText.text = $"{percent}{percentSuffix}";
        }
    }
}
