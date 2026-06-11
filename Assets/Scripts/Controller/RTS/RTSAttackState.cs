using UnityEngine;

[DisallowMultipleComponent]
public class RTSAttackState : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private CharacterAnimationController animationController;

    private RTSUnitAI ai;
    private RTSUnitMovement movement;
    private RTSAttackArea attackArea;
    private float nextAttackTime;

    public void Initialize(RTSUnitAI unitAI, RTSUnitMovement unitMovement, RTSAttackArea unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
        animationController = GetAnimationController();
    }

    public void Tick()
    {
        if (!ai.HasValidTarget())
        {
            ai.ClearTarget();
            return;
        }

        Transform target = ai.TargetEnemy;
        if (!attackArea.CanSee(target))
        {
            ai.ClearTarget();
            return;
        }

        if (!attackArea.IsInAttackRange(target))
        {
            ai.SetState(RTSUnitAI.UnitState.Chase);
            return;
        }

        movement.Stop();
        if (Time.time >= nextAttackTime)
        {
            TriggerAttack(target);
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    private void TriggerAttack(Transform target)
    {
        RTSUnitHealth targetHealth = target.GetComponentInParent<RTSUnitHealth>();
        if (targetHealth == null)
        {
            ai.ClearTarget();
            return;
        }

        Vector2 attackDirection = targetHealth.transform.position - transform.position;
        animationController?.SetFacingDirection(attackDirection);
        animationController?.PlayAttack();

        targetHealth.TakeDamage(attackDamage);
        Debug.Log($"{gameObject.name} attacks {targetHealth.name} for {attackDamage} damage");

        if (targetHealth.IsDead)
        {
            ai.ClearTarget();
        }
    }

    private void OnValidate()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attackDamage = Mathf.Max(0f, attackDamage);
    }

    private CharacterAnimationController GetAnimationController()
    {
        if (animationController != null)
        {
            return animationController;
        }

        CharacterAnimationController controller = GetComponent<CharacterAnimationController>();
        if (controller == null && GetComponentInChildren<Animator>() != null)
        {
            controller = gameObject.AddComponent<CharacterAnimationController>();
        }

        return controller;
    }
}
