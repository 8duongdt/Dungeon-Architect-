using UnityEngine;

/// <summary>
/// Trạng thái khống chế trên một unit: choáng (stun - đứng im, không đánh),
/// trói chân (root - đứng im nhưng vẫn đánh được), làm chậm có thời hạn.
/// Component được thêm lúc runtime khi unit lần đầu dính hiệu ứng (GetOrAdd),
/// các nơi điều khiển di chuyển/AI tự gate qua IsStunned/IsImmobilized.
/// </summary>
[DisallowMultipleComponent]
public class UnitStatusEffects : MonoBehaviour
{
    private float stunUntil;
    private float rootUntil;
    private float slowUntil;
    private bool slowActive;
    private UnitEffectModifier effectModifier;

    /// <summary>Choáng: khóa toàn bộ AI (không di chuyển, không đánh).</summary>
    public bool IsStunned => Time.time < stunUntil;

    /// <summary>Bị giữ chân (choáng hoặc trói): không được di chuyển.</summary>
    public bool IsImmobilized => IsStunned || Time.time < rootUntil;

    public static UnitStatusEffects GetOrAdd(GameObject unit)
    {
        var status = unit.GetComponent<UnitStatusEffects>();
        return status != null ? status : unit.AddComponent<UnitStatusEffects>();
    }

    public void ApplyStun(float duration)
    {
        stunUntil = Mathf.Max(stunUntil, Time.time + duration);
    }

    public void ApplyRoot(float duration)
    {
        rootUntil = Mathf.Max(rootUntil, Time.time + duration);
    }

    /// <summary>
    /// Làm chậm có thời hạn qua UnitEffectModifier (hệ số tuyệt đối như các hazard hiện có).
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        if (effectModifier == null)
        {
            effectModifier = GetComponent<UnitEffectModifier>();
        }
        if (effectModifier == null)
        {
            return;
        }

        effectModifier.ApplySpeedPenalty(multiplier);
        slowUntil = Mathf.Max(slowUntil, Time.time + duration);
        slowActive = true;
    }

    private void Update()
    {
        bool slowExpired = slowActive && Time.time >= slowUntil;
        if (slowExpired)
        {
            slowActive = false;
            effectModifier?.ClearSpeedPenalty();
        }
    }
}
