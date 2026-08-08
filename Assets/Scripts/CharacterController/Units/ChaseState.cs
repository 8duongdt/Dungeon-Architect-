using UnityEngine;

[DisallowMultipleComponent]
public class ChaseState : MonoBehaviour
{
    private UnitAI ai;
    private UnitMovement movement;
    private AttackAreaBase attackArea;
    private UnitPathFollower pathFollower;

    public void Initialize(
        UnitAI unitAI, UnitMovement unitMovement, AttackAreaBase unitAttackArea, UnitPathFollower unitPathFollower)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
        pathFollower = unitPathFollower;
    }

    public void Tick()
    {
        if (!ai.HasValidTarget())
        {
            ai.ClearTarget();
            return;
        }

        Transform target = ai.TargetEnemy;
        if (!attackArea.CanSee(target) || !attackArea.IsInDetectionRange(target))
        {
            ai.ClearTarget();
            return;
        }

        // Áp sát tới AttackApproachRange (< AttackRange) rồi mới chuyển Attack: tạo dải đệm
        // hysteresis vì AttackState chỉ nhả về Chase khi địch ra khỏi AttackRange đầy đủ.
        if (attackArea.IsInAttackApproachRange(target))
        {
            movement.Stop();
            ai.SetState(UnitAI.UnitState.Attack);
            return;
        }

        UnitPathFollower.SteeringResult steering = pathFollower.GetSteeringPoint(target.position);
        float stopDistance = steering.IsFinalDestination ? attackArea.AttackApproachRange : 0f;
        movement.MoveTowards(steering.Point, stopDistance);
    }
}
