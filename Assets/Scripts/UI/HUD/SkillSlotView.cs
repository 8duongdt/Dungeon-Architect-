using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô trên thanh kỹ năng: icon phép, nhãn phím tắt, nhãn mana và
/// lớp phủ hồi chiêu quét tròn (fillAmount 1 = vừa cast, 0 = sẵn sàng).
/// </summary>
public class SkillSlotView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private TMP_Text manaLabel;

    public void Bind(SkillDefinitionSO skill, string hotkeyLabel)
    {
        if (keyLabel != null)
        {
            keyLabel.text = hotkeyLabel;
        }

        bool hasSkill = skill != null;
        if (iconImage != null)
        {
            iconImage.enabled = hasSkill && skill.Icon != null;
            iconImage.sprite = hasSkill ? skill.Icon : null;
        }

        if (manaLabel != null)
        {
            bool showMana = hasSkill && skill.ManaCost > 0;
            manaLabel.text = showMana ? skill.ManaCost.ToString() : string.Empty;
        }
    }

    public void SetCooldownFraction(float fraction)
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = fraction;
        }
    }
}
