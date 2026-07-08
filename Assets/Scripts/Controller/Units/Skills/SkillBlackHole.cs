using UnityEngine;

/// <summary>
/// Xoáy hút (ultimate BlackHole): trong pullDuration giây, mọi kẻ địch trong bán kính
/// bị kéo dần về tâm và trói chân liên tục (không tự di chuyển được). Tự hủy khi hết giờ.
/// Sát thương (nếu có) do các nguồn khác xử lý - xoáy chỉ khống chế vị trí.
/// </summary>
[DisallowMultipleComponent]
public class SkillBlackHole : MonoBehaviour
{
    // Trói theo nhịp ngắn liên tục để hiệu ứng tự hết ngay khi xoáy biến mất.
    private const float RootPulseDuration = 0.3f;
    private const float StopPullingDistance = 0.15f;

    private UnitFaction casterFaction;
    private float pullRadius;
    private float pullSpeed;
    private float endTime;

    public void Initialize(in SkillCastContext context, SkillDefinitionSO skill)
    {
        casterFaction = context.CasterFaction;
        pullRadius = skill.PullRadius;
        pullSpeed = skill.PullSpeed;
        endTime = Time.time + skill.PullDuration;

        Destroy(gameObject, skill.PullDuration);
    }

    private void FixedUpdate()
    {
        if (Time.time >= endTime)
        {
            return;
        }

        foreach (UnitHealth victim in SkillTargeting.FindAttackableInRadius(transform.position, pullRadius, casterFaction))
        {
            PullTowardsCenter(victim);
            UnitStatusEffects.GetOrAdd(victim.gameObject).ApplyRoot(RootPulseDuration);
        }
    }

    private void PullTowardsCenter(UnitHealth victim)
    {
        Vector2 victimPosition = victim.transform.position;
        Vector2 toCenter = (Vector2)transform.position - victimPosition;
        if (toCenter.magnitude <= StopPullingDistance)
        {
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(
            victimPosition, transform.position, pullSpeed * Time.fixedDeltaTime);

        var victimBody = victim.GetComponent<Rigidbody2D>();
        if (victimBody != null)
        {
            victimBody.MovePosition(nextPosition);
        }
        else
        {
            victim.transform.position = new Vector3(nextPosition.x, nextPosition.y, victim.transform.position.z);
        }
    }
}
