using UnityEngine;

/// <summary>
/// Bẫy đặt tại chỗ: chờ armDelay rồi mờ đi nằm chờ; kẻ địch ĐẦU TIÊN bước vào
/// bán kính sẽ bị gây sát thương + trói chân, bẫy kích hoạt hiệu ứng rồi tự hủy.
/// </summary>
[DisallowMultipleComponent]
public class SkillTrap : MonoBehaviour
{
    private const float ArmedSpriteAlpha = 0.45f;
    private const float TriggerCheckInterval = 0.1f;
    private const float TriggeredVfxLifetime = 1f;
    private const float MaxLifetimeSeconds = 30f;

    private UnitFaction casterFaction;
    private float damage;
    private float triggerRadius;
    private float rootDuration;
    private float armedAtTime;
    private float nextCheckTime;
    private bool isTriggered;
    private SpriteRenderer[] spriteRenderers;

    public void Initialize(in SkillCastContext context, SkillDefinitionSO skill)
    {
        casterFaction = context.CasterFaction;
        damage = context.Damage;
        triggerRadius = skill.ZoneRadius;
        rootDuration = skill.TrapRootDuration;
        armedAtTime = Time.time + skill.TrapArmDelay;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        Destroy(gameObject, MaxLifetimeSeconds);
    }

    private void Update()
    {
        bool isWaitingOrDone = isTriggered || Time.time < armedAtTime || Time.time < nextCheckTime;
        if (isWaitingOrDone)
        {
            return;
        }

        FadeToArmedLook();
        nextCheckTime = Time.time + TriggerCheckInterval;
        TryTrigger();
    }

    private void TryTrigger()
    {
        foreach (UnitHealth victim in SkillTargeting.FindAttackableInRadius(transform.position, triggerRadius, casterFaction))
        {
            Trigger(victim);
            return;
        }
    }

    private void Trigger(UnitHealth victim)
    {
        isTriggered = true;
        victim.TakeDamage(damage);
        if (rootDuration > 0f)
        {
            UnitStatusEffects.GetOrAdd(victim.gameObject).ApplyRoot(rootDuration);
        }

        RestoreSpriteAlpha();
        Destroy(gameObject, TriggeredVfxLifetime);
    }

    // Bẫy mờ đi sau khi có hiệu lực để "tàng hình" một phần theo thiết kế.
    private void FadeToArmedLook()
    {
        SetSpritesAlpha(ArmedSpriteAlpha);
    }

    private void RestoreSpriteAlpha()
    {
        SetSpritesAlpha(1f);
    }

    private void SetSpritesAlpha(float alpha)
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
