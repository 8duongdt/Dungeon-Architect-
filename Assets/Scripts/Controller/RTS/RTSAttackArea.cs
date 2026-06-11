using UnityEngine;

[DisallowMultipleComponent]
public class RTSAttackArea : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float loseTargetRadiusMultiplier = 1.2f;
    [SerializeField] private LayerMask enemyLayer = ~0;
    [SerializeField] private RTSUnitLineOfSight lineOfSight;

    public float DetectionRadius => detectionRadius;
    public float AttackRange => attackRange;
    public float LoseTargetRadiusMultiplier => loseTargetRadiusMultiplier;

    private void Awake()
    {
        ResolveReferences();
    }

    public Transform FindVisibleTarget(RTSUnitAI owner)
    {
        ResolveReferences();

        Collider2D[] targetsInRadius = Physics2D.OverlapCircleAll(transform.position, detectionRadius, GetSearchLayerMask());
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

            float distance = ((Vector2)targetTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return closestTarget;
    }

    public bool IsInAttackRange(Transform target)
    {
        return IsInRange(target, attackRange);
    }

    public bool IsInDetectionRange(Transform target)
    {
        return IsInRange(target, detectionRadius * loseTargetRadiusMultiplier);
    }

    public bool CanSee(Transform target)
    {
        ResolveReferences();
        return target != null && (lineOfSight == null || lineOfSight.HasLineOfSight(transform.position, target.position));
    }

    public bool IsAttackableTarget(RTSUnitAI owner, Transform target)
    {
        if (target == null)
        {
            return false;
        }

        RTSUnitHealth targetHealth = target.GetComponentInParent<RTSUnitHealth>();
        if (targetHealth == null || targetHealth.IsDead)
        {
            return false;
        }

        if (owner != null && targetHealth == owner.Health)
        {
            return false;
        }

        RTSUnitFaction ownerFaction = owner != null ? owner.Faction : GetComponent<RTSUnitFaction>();
        RTSUnitFaction targetFaction = targetHealth.GetComponentInParent<RTSUnitFaction>();
        return ownerFaction != null && ownerFaction.CanAttack(targetFaction);
    }

    private bool TryGetTarget(RTSUnitAI owner, Collider2D collider, out Transform targetTransform)
    {
        targetTransform = null;
        if (collider == null)
        {
            return false;
        }

        RTSUnitHealth targetHealth = collider.GetComponentInParent<RTSUnitHealth>();
        if (targetHealth == null)
        {
            return false;
        }

        targetTransform = targetHealth.transform;
        return IsAttackableTarget(owner, targetTransform);
    }

    private int GetSearchLayerMask()
    {
        return enemyLayer.value == 0 ? ~0 : enemyLayer.value;
    }

    private bool IsInRange(Transform target, float range)
    {
        if (target == null)
        {
            return false;
        }

        return Vector2.Distance(transform.position, target.position) <= range;
    }

    private void ResolveReferences()
    {
        if (lineOfSight == null)
        {
            lineOfSight = GetComponent<RTSUnitLineOfSight>();
        }
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        attackRange = Mathf.Max(0f, attackRange);
        loseTargetRadiusMultiplier = Mathf.Max(1f, loseTargetRadiusMultiplier);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
