using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Trạng thái người chơi trên HUD. Thanh máu do UnitHealth đẩy thẳng vào Bar
/// (hiệu ứng tụt/đầy chậm như thanh máu trên đầu unit prefab) nên view không
/// đụng vào fill máu; chỉ poll mỗi frame cho nhãn số và lớp khiên vì khiên
/// tự hết hạn theo thời gian, không có event.
/// Khiên hiển thị theo tỉ lệ máu tối đa để hai thanh so sánh được với nhau.
/// </summary>
public class PlayerStatusHudView : MonoBehaviour
{
    [SerializeField] private UnitHealth playerHealth;
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text healthLabel;

    private void Update()
    {
        // Player có thể đã bị hủy sau khi chết (destroyOnDeath) -> đưa hiển thị về 0.
        bool hasLivePlayer = playerHealth != null && !playerHealth.IsDead && playerHealth.MaxHealth > 0f;
        RefreshShieldFill(hasLivePlayer);
        RefreshHealthLabel(hasLivePlayer);
    }

    private void RefreshShieldFill(bool hasLivePlayer)
    {
        if (shieldFill == null)
        {
            return;
        }

        // Không có phép khiên thì CurrentShield = 0 -> lớp khiên để trống.
        shieldFill.fillAmount = hasLivePlayer
            ? Mathf.Clamp01(playerHealth.CurrentShield / playerHealth.MaxHealth)
            : 0f;
    }

    private void RefreshHealthLabel(bool hasLivePlayer)
    {
        if (healthLabel == null)
        {
            return;
        }

        healthLabel.text = hasLivePlayer
            ? $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}"
            : "0";
    }
}
