using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu + khiên của người chơi trên HUD. Poll mỗi frame vì UnitHealth
/// không có event cho khiên (khiên tự hết hạn theo thời gian).
/// Khiên hiển thị theo tỉ lệ máu tối đa để hai thanh so sánh được với nhau.
/// </summary>
public class PlayerStatusHudView : MonoBehaviour
{
    [SerializeField] private UnitHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text healthLabel;

    private void Update()
    {
        RefreshBars();
    }

    private void RefreshBars()
    {
        // Player có thể đã bị hủy sau khi chết (destroyOnDeath) -> đưa thanh về 0.
        bool hasLivePlayer = playerHealth != null && !playerHealth.IsDead && playerHealth.MaxHealth > 0f;
        float healthFraction = hasLivePlayer ? playerHealth.CurrentHealth / playerHealth.MaxHealth : 0f;
        float shieldFraction = hasLivePlayer ? Mathf.Clamp01(playerHealth.CurrentShield / playerHealth.MaxHealth) : 0f;

        if (healthFill != null)
        {
            healthFill.fillAmount = healthFraction;
        }

        if (shieldFill != null)
        {
            shieldFill.fillAmount = shieldFraction;
        }

        if (healthLabel != null)
        {
            healthLabel.text = hasLivePlayer
                ? $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}"
                : "0";
        }
    }
}
