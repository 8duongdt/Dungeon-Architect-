using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh Mana trên HUD. Trần Mana lấy thẳng từ <see cref="ResourceManager.MaxMana"/> nên
/// thanh và nhãn "hiện tại/tối đa" luôn khớp kho thật, không cần con số cấu hình song song.
/// Đăng ký sự kiện ManaChanged như ResourceHudBinder - không poll mỗi frame.
/// </summary>
public class ManaBarView : MonoBehaviour
{
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
        int maxMana = resourceManager.MaxMana;

        if (manaFill != null)
        {
            manaFill.fillAmount = maxMana > 0 ? Mathf.Clamp01((float)mana / maxMana) : 0f;
        }

        if (manaLabel != null)
        {
            manaLabel.text = $"{mana}/{maxMana}";
        }
    }
}
