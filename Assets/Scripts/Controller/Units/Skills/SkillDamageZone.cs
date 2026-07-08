using System.Collections;
using UnityEngine;

/// <summary>
/// Vùng gây sát thương theo nhịp (DoT zone): mỗi tickInterval giây quét kẻ địch
/// trong bán kính, gây sát thương (+ choáng tùy chọn), tự hủy khi hết thời gian.
/// Dùng cho LightningBolt (giật + choáng 3s) và FireWall (tường lửa thiêu đốt).
/// </summary>
[DisallowMultipleComponent]
public class SkillDamageZone : MonoBehaviour
{
    private UnitFaction casterFaction;
    private float damagePerTick;
    private float tickInterval;
    private float radius;
    private float stunDurationPerTick;
    private float duration;

    public void Initialize(in SkillCastContext context, SkillDefinitionSO skill)
    {
        casterFaction = context.CasterFaction;
        damagePerTick = context.Damage * skill.ZoneTickDamageFactor;
        tickInterval = skill.ZoneTickInterval;
        radius = skill.ZoneRadius;
        stunDurationPerTick = skill.ZoneStunDuration;
        duration = skill.ZoneDuration;

        StartCoroutine(TickLoop());
        Destroy(gameObject, duration);
    }

    private IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(tickInterval);
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            yield return wait;
            DamageEnemiesInside();
        }
    }

    private void DamageEnemiesInside()
    {
        foreach (UnitHealth target in SkillTargeting.FindAttackableInRadius(transform.position, radius, casterFaction))
        {
            target.TakeDamage(damagePerTick);
            if (stunDurationPerTick > 0f)
            {
                UnitStatusEffects.GetOrAdd(target.gameObject).ApplyStun(stunDurationPerTick);
            }
        }
    }
}
