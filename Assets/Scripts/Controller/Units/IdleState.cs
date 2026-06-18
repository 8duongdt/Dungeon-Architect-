using UnityEngine;

[DisallowMultipleComponent]
public class IdleState : MonoBehaviour
{
    [SerializeField] private bool patrolWhenIdle;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolPointTolerance = 0.15f;

    private UnitAI ai;
    private UnitMovement movement;
    private AttackAreaBase attackArea;
    private int patrolIndex;

    public void Initialize(UnitAI unitAI, UnitMovement unitMovement, AttackAreaBase unitAttackArea)
    {
        ai = unitAI;
        movement = unitMovement;
        attackArea = unitAttackArea;
    }

    public void Tick()
    {
        // Lệnh di chuyển của người chơi ưu tiên cao nhất: không tự động khóa mục tiêu
        // trong lúc unit đang thực hiện lệnh di chuyển thủ công.
        if (ai.IsManualMoveCommandActive)
        {
            return;
        }

        Transform visibleTarget = attackArea.FindVisibleTarget(ai);
        if (visibleTarget != null)
        {
            ai.SetTarget(visibleTarget, UnitAI.UnitState.Chase);
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
