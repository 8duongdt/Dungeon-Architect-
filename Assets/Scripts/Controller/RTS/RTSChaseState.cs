using UnityEngine;

[DisallowMultipleComponent]
public class RTSChaseState : MonoBehaviour
{
    private RTSUnitAI ai;
    private RTSUnitMovement movement;
    private RTSAttackArea attackArea;

    public void Initialize(RTSUnitAI unitAI, RTSUnitMovement unitMovement, RTSAttackArea unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
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

        if (attackArea.IsInAttackRange(target))
        {
            movement.Stop();
            ai.SetState(RTSUnitAI.UnitState.Attack);
            return;
        }

        movement.MoveTowards(target.position);
    }
}
