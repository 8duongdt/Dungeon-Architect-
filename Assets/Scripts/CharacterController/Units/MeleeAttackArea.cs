using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MeleeAttackArea : AttackAreaBase
{
    private const int MaxHitBoxOverlaps = 32;

    [Header("Melee HitBox")]
    [SerializeField] private Collider2D hitBox;

    private readonly Collider2D[] hitBoxResults = new Collider2D[MaxHitBoxOverlaps];

    public Collider2D HitBox => hitBox;

    public override float GetDetectionRadius()
    {
        return Mathf.Max(base.GetDetectionRadius(), attackRange + 5f);
    }

    public override bool TryGetDamageTarget(UnitAI owner, Transform target, out UnitHealth targetHealth)
    {
        if (hitBox == null)
        {
            return base.TryGetDamageTarget(owner, target, out targetHealth);
        }

        targetHealth = target != null ? target.GetComponentInParent<UnitHealth>() : null;
        if (targetHealth == null || targetHealth.IsDead || !hitBox.enabled || !hitBox.gameObject.activeInHierarchy)
        {
            targetHealth = null;
            return false;
        }

        if (!CanSee(targetHealth.transform) || !IsAttackableTarget(owner, targetHealth.transform))
        {
            targetHealth = null;
            return false;
        }

        int hitCount = hitBox.Overlap(CreateHitBoxFilter(), hitBoxResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlap = hitBoxResults[i];
            if (overlap == null)
            {
                continue;
            }

            if (overlap.GetComponentInParent<UnitHealth>() == targetHealth)
            {
                return true;
            }
        }

        targetHealth = null;
        return false;
    }

    private void Reset()
    {
        detectionRadius = 7f;
        attackRange = 1.5f;
        loseTargetRadiusMultiplier = 1.1f;
        ResolveHitBox();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        ResolveHitBox();
    }

    private void Awake()
    {
        ResolveHitBox();
    }

    private ContactFilter2D CreateHitBoxFilter()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        filter.SetLayerMask(GetSearchLayerMask());
        return filter;
    }

    private void ResolveHitBox()
    {
        if (hitBox != null)
        {
            return;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D childCollider in colliders)
        {
            if (childCollider != null &&
                childCollider.gameObject != gameObject &&
                childCollider.gameObject.name.IndexOf("HitBox", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hitBox = childCollider;
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (hitBox == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            return;
        }

        Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(hitBox.bounds.center, hitBox.bounds.size);
    }
}
