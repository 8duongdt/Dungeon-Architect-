using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh Mana trên HUD. Mana không có trần (ResourceManager) nên thanh chỉ mang tính
/// hiển thị tương đối theo displayMaxMana; số chữ luôn là giá trị thật, không bị cắt.
/// Đăng ký sự kiện ManaChanged như ResourceHudBinder - không poll mỗi frame.
/// </summary>
public class ManaBarView : MonoBehaviour
{
    [Tooltip("Mana dùng để quy đổi độ đầy của thanh (chỉ để vẽ - Mana thật không có trần).")]
    [SerializeField] private float displayMaxMana = 100f;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private Image manaFill;
    [SerializeField] private TMP_Text manaLabel;

    private void OnEnable()
    {
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;
        }
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.ManaChanged += OnManaChanged;
        OnManaChanged(resourceManager.Mana);
    }

    private void OnDisable()
    {
        if (resourceManager != null)
        {
            resourceManager.ManaChanged -= OnManaChanged;
        }
    }

    private void OnManaChanged(int mana)
    {
        if (manaFill != null)
        {
            manaFill.fillAmount = displayMaxMana > 0f ? Mathf.Clamp01(mana / displayMaxMana) : 0f;
        }

        if (manaLabel != null)
        {
            manaLabel.text = mana.ToString();
        }
    }

    private void OnValidate()
    {
        displayMaxMana = Mathf.Max(1f, displayMaxMana);
    }
}
