using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh tiến trình "thức tỉnh" màu tím ở góc trên. Đọc tiến độ từ <see cref="PhaseManager"/> và
/// cập nhật ảnh fill + nhãn phần trăm. Ảnh fill nên đặt Image Type = Filled (Horizontal).
/// </summary>
public class AwakeningBarView : MonoBehaviour
{
    [SerializeField] private PhaseManager phaseManager;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [Tooltip("Hậu tố nhãn phần trăm.")]
    [SerializeField] private string percentSuffix = "% COMPLETE";

    private void Awake()
    {
        if (phaseManager == null)
        {
            phaseManager = PhaseManager.Instance;
        }
    }

    private void OnEnable()
    {
        if (phaseManager == null)
        {
            phaseManager = PhaseManager.Instance;
        }
        if (phaseManager == null)
        {
            return;
        }

        phaseManager.ProgressChanged += OnProgressChanged;
        OnProgressChanged(phaseManager.Progress01);
    }

    private void OnDisable()
    {
        if (phaseManager == null)
        {
            return;
        }

        phaseManager.ProgressChanged -= OnProgressChanged;
    }

    private void OnProgressChanged(float progress01)
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
