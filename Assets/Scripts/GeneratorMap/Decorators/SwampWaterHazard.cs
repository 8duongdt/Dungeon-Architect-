using UnityEngine;

/// <summary>
/// Vùng nước đầm lầy: unit lội vào bị làm chậm, ra khỏi thì hồi tốc độ.
/// Cần một Collider2D đặt isTrigger (Reset tự bật khi gắn component trong Editor).
///
/// Phát hiện unit qua <see cref="UnitEffectModifier"/> trên object đi vào (thay vì khóa cứng
/// theo tag "Player"), nên áp dụng cho MỌI unit RTS có hệ thống hiệu ứng. Mỗi collider vào/ra
/// kích hoạt riêng nên nhiều unit cùng đứng trong nước vẫn được làm chậm/hồi độc lập.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SwampWaterHazard : MonoBehaviour
{
    [Tooltip("Hệ số tốc độ khi unit lội trong nước (0.5 = còn một nửa tốc độ gốc).")]
    [Range(0f, 1f)]
    [SerializeField] private float slowMultiplier = 0.5f;

    // Khi gắn component trong Editor, bảo đảm collider là trigger để OnTrigger* hoạt động.
    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitEffectModifier effectModifier = other.GetComponentInParent<UnitEffectModifier>();
        if (effectModifier != null)
        {
            effectModifier.ApplySpeedPenalty(slowMultiplier);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UnitEffectModifier effectModifier = other.GetComponentInParent<UnitEffectModifier>();
        if (effectModifier != null)
        {
            effectModifier.ClearSpeedPenalty();
        }
    }
}
