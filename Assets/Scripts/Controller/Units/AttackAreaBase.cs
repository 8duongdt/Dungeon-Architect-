using UnityEngine;


public abstract class AttackAreaBase : MonoBehaviour
{
    private const float MinAttackApproachFactor = 0.5f;
    private const float MaxAttackApproachFactor = 1f;

    [SerializeField] protected float detectionRadius = 5f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float loseTargetRadiusMultiplier = 1.2f;
    [Tooltip("Tỉ lệ của tầm đánh mà unit áp sát tới trước khi chuyển sang Attack. Nhỏ hơn 1 tạo vùng đệm " +
             "chống nhấp nháy Attack<->Chase và giữ unit dừng hẳn ở khoảng cách đánh thay vì thúc vào mục tiêu.")]
    [SerializeField] [Range(MinAttackApproachFactor, MaxAttackApproachFactor)] protected float attackApproachFactor = 0.9f;
    [SerializeField] protected LayerMask enemyLayer = ~0;
    [SerializeField] protected UnitLineOfSight lineOfSight;

    public float DetectionRadius => GetDetectionRadius();
    public float AttackRange => attackRange;
    public float AttackApproachRange => attackRange * attackApproachFactor;
    public float LoseTargetRadiusMultiplier => loseTargetRadiusMultiplier;

    public Transform FindVisibleTarget(UnitAI owner)
    {
        ResolveReferences();

        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(transform.position, GetDetectionRadius(), GetSearchLayerMask());
        Transform preferredAvatarTarget = null;
        Transform closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D potentialTarget in targetsInRadius)
        {
            if (!TryGetTarget(owner, potentialTarget, out Transform targetTransform))
            {
                continue;
            }

            if (!CanSee(targetTransform))
            {
                continue;
            }

            // Nhân vật chính do người chơi điều khiển luôn được ưu tiên hơn Unit RTS khác, bất kể
            // khoảng cách - quái "ưu tiên đánh người chơi" khi cùng lúc thấy nhiều mục tiêu.
            if (preferredAvatarTarget == null && targetTransform.GetComponentInParent<PlayerAvatarMarker>() != null)
            {
                preferredAvatarTarget = targetTransform;
                continue;
            }

            float distance = ((Vector2)targetTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return preferredAvatarTarget != null ? preferredAvatarTarget : closestTarget;
    }

    public bool IsInAttackRange(Transform target)
    {
        return IsInRange(target, attackRange);
    }

    public bool IsInAttackApproachRange(Transform target)
    {
        return IsInRange(target, AttackApproachRange);
    }

    public virtual bool TryGetDamageTarget(UnitAI owner, Transform target, out UnitHealth targetHealth)
    {
        targetHealth = null;
        if (target == null || !CanSee(target) || !IsInAttackRange(target) || !IsAttackableTarget(owner, target))
        {
            return false;
        }

        targetHealth = target.GetComponentInParent<UnitHealth>();
        return targetHealth != null && !targetHealth.IsDead;
    }

    public bool IsInDetectionRange(Transform target)
    {
        return IsInRange(target, GetDetectionRadius() * loseTargetRadiusMultiplier);
    }

    public virtual float GetDetectionRadius()
    {
        return detectionRadius;
    }

    public bool CanSee(Transform target)
    {
        ResolveReferences();
        return target != null && (lineOfSight == null || lineOfSight.HasLineOfSight(transform.position, target.position));
    }

    public bool IsAttackableTarget(UnitAI owner, Transform target)
    {
        if (target == null)
        {
            return false;
        }

        UnitHealth targetHealth = target.GetComponentInParent<UnitHealth>();
        if (targetHealth == null || targetHealth.IsDead)
        {
            return false;
        }

        if (owner != null && targetHealth == owner.Health)
        {
            return false;
        }

        UnitFaction ownerFaction = owner != null ? owner.Faction : GetComponent<UnitFaction>();
        UnitFaction targetFaction = targetHealth.GetComponentInParent<UnitFaction>();
        return ownerFaction != null && ownerFaction.CanAttack(targetFaction);
    }

    protected virtual void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        attackRange = Mathf.Max(0f, attackRange);
        loseTargetRadiusMultiplier = Mathf.Max(1f, loseTargetRadiusMultiplier);
        attackApproachFactor = Mathf.Clamp(attackApproachFactor, MinAttackApproachFactor, MaxAttackApproachFactor);
    }

    protected bool TryGetTarget(UnitAI owner, Collider2D collider, out Transform targetTransform)
    {
        targetTransform = null;
        if (collider == null)
        {
            return false;
        }

        UnitHealth targetHealth = collider.GetComponentInParent<UnitHealth>();
        if (targetHealth == null)
        {
            return false;
        }

        targetTransform = targetHealth.transform;
        return IsAttackableTarget(owner, targetTransform);
    }

    protected int GetSearchLayerMask()
    {
        return enemyLayer.value == 0 ? ~0 : enemyLayer.value;
    }

    protected bool IsInRange(Transform target, float range)
    {
        if (target == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, target.position) <= range;
    }

    protected void ResolveReferences()
    {
        if (lineOfSight == null)
        {
            lineOfSight = GetComponent<UnitLineOfSight>();
        }
    }
}
