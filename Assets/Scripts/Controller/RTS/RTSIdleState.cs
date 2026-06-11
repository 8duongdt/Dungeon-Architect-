using UnityEngine;

[DisallowMultipleComponent]
public class RTSIdleState : MonoBehaviour
{
    [SerializeField] private bool patrolWhenIdle;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPointTolerance = 0.15f;

    private RTSUnitAI ai;
    private RTSUnitMovement movement;
    private RTSAttackArea attackArea;
    private int patrolIndex;

    public void Initialize(RTSUnitAI unitAI, RTSUnitMovement unitMovement, RTSAttackArea unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
    }

    public void Tick()
    {
        Transform visibleTarget = attackArea.FindVisibleTarget(ai);
        if (visibleTarget != null)
        {
            ai.SetTarget(visibleTarget, RTSUnitAI.UnitState.Chase);
            return;
        }

        if (ai.IsManualMoveCommandActive)
        {
            return;
        }

        PatrolOrStop();
    }

    private void PatrolOrStop()
    {
        if (!patrolWhenIdle || patrolPoints == null || patrolPoints.Length == 0)
        {
            movement.Stop();
            return;
        }

        Transform patrolPoint = patrolPoints[patrolIndex];
        if (patrolPoint == null)
        {
            AdvancePatrolPoint();
            movement.Stop();
            return;
        }

        if (Vector2.Distance(transform.position, patrolPoint.position) <= patrolPointTolerance)
        {
            AdvancePatrolPoint();
            patrolPoint = patrolPoints[patrolIndex];
        }

        if (patrolPoint != null)
        {
            movement.MoveTowards(patrolPoint.position);
        }
    }

    private void AdvancePatrolPoint()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    private void OnValidate()
    {
        patrolPointTolerance = Mathf.Max(0f, patrolPointTolerance);
    }
}
