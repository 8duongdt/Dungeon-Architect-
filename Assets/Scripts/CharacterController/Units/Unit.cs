using UnityEngine;

public class Unit : MonoBehaviour, ISpeedModifiable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [Tooltip("Lực hãm vận tốc dư do va chạm. Càng cao thì unit càng không bị đẩy trôi đi (vẫn va chạm chống đè lên nhau).")]
    [SerializeField] private float collisionRecoveryDamping = 12f;

    // Hệ số nhân tốc độ do hệ thống hiệu ứng đặt (1 = bình thường). Áp cho lệnh di chuyển tay.
    private float speedMultiplier = 1f;
    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = Mathf.Max(0f, value);
    }

    /// <summary>Áp tốc độ GỐC từ bộ chỉ số trung tâm (<see cref="UnitStatsSO"/>).</summary>
    public void ApplySpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    private GameObject selectedVisual;
    private CharacterAnimationController animationController;
    private UnitAI unitAI;
    private UnitHealth health;
    private UnitStatusEffects statusEffects;
    private UnitPathFollower pathFollower;
    private Rigidbody2D rb;
    private Vector3 targetPosition;
    private Vector2 currentMoveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animationController = GetAnimationController();
        unitAI = GetComponent<UnitAI>();
        health = GetComponent<UnitHealth>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            // Giữ va chạm (chống đè lên nhau) nhưng dập tắt quán tính sau mỗi cú va
            // để unit không bị đẩy trôi đi.
            rb.linearDamping = Mathf.Max(rb.linearDamping, collisionRecoveryDamping);
        }

        Transform selectedTransform = transform.Find("Selected");
        if (selectedTransform != null)
        {
            selectedVisual = selectedTransform.gameObject;
        }

        targetPosition = transform.position;
        SetSelectedVisible(false);
    }

    private void Update()
    {
        animationController?.TickAttack(Time.deltaTime);

        if (health != null && health.IsDead)
        {
            currentMoveDirection = Vector2.zero;
            return;
        }

        if (unitAI != null && unitAI.HasActiveCombatTarget)
        {
            return;
        }

        // Chỉ điều khiển animation khi đang thực hiện lệnh di chuyển tay - lúc rảnh
        // IdleState/UnitMovement chịu trách nhiệm, tránh hai nơi cùng ghi đè animator.
        if (unitAI != null && !unitAI.IsManualMoveCommandActive)
        {
            return;
        }

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        MoveTowardsTarget();
    }

    public void SetSelectedVisible(bool visible)
    {
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(visible);
        }
    }

    public void MoveTo(Vector3 targetPos)
    {
        targetPosition = targetPos;
        targetPosition.z = transform.position.z;
        unitAI?.HandleMoveCommand();
    }

    private void MoveTowardsTarget()
    {
        // Bị giữ chân (choáng/trói bởi skill): đứng im tại chỗ, lệnh di chuyển vẫn giữ
        // (stuck-timeout của UnitAI sẽ tự nhả lệnh nếu bị trói quá lâu).
        if (statusEffects == null)
        {
            statusEffects = GetComponent<UnitStatusEffects>();
        }
        if (statusEffects != null && statusEffects.IsImmobilized)
        {
            currentMoveDirection = Vector2.zero;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        if (unitAI != null && unitAI.HasActiveCombatTarget)
        {
            // Đang combat: nhường quyền điều khiển vận tốc cho AI (ChaseState/AttackState).
            // KHÔNG xóa linearVelocity ở đây, nếu không unit sẽ không thể áp sát mục tiêu.
            targetPosition = transform.position;
            currentMoveDirection = Vector2.zero;
            unitAI.CompleteMoveCommand();
            return;
        }

        // Không có lệnh di chuyển tay: nhường rigidbody cho hành vi rảnh của AI
        // (tuần tra, hành quân chiếm tinh thể) - Unit chỉ lái khi người chơi ra lệnh.
        if (unitAI != null && !unitAI.IsManualMoveCommandActive)
        {
            return;
        }

        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 finalDestination = targetPosition;
        Vector2 toFinalDestination = finalDestination - currentPosition;

        if (toFinalDestination.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            currentMoveDirection = Vector2.zero;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            unitAI?.CompleteMoveCommand();
            return;
        }

        // Component do UnitAI thêm lúc runtime nên phải dò lại khi chưa có (thứ tự Awake không đảm bảo).
        if (pathFollower == null)
        {
            pathFollower = GetComponent<UnitPathFollower>();
        }

        // Đi theo waypoint A* thay vì lao thẳng xuyên tường - điểm "đã tới nơi" ở trên vẫn tính
        // theo đích thật, không phải waypoint, để không kết thúc lệnh giữa chừng.
        Vector2 steeringPoint = pathFollower != null
            ? (Vector2)pathFollower.GetSteeringPoint(targetPosition).Point
            : finalDestination;
        Vector2 toSteeringPoint = steeringPoint - currentPosition;

        currentMoveDirection = toSteeringPoint.sqrMagnitude > 0.0001f ? toSteeringPoint.normalized : Vector2.zero;
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            steeringPoint,
            moveSpeed * speedMultiplier * Time.fixedDeltaTime
        );

        if (rb != null)
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        }
    }

    private void UpdateAnimation()
    {
        if (currentMoveDirection.sqrMagnitude > 0.0001f)
        {
            animationController?.PlayMove(currentMoveDirection);
            return;
        }

        animationController?.PlayIdle();
    }

    private CharacterAnimationController GetAnimationController()
    {
        CharacterAnimationController controller = GetComponent<CharacterAnimationController>();
        if (controller == null && GetComponentInChildren<Animator>() != null)
        {
            controller = gameObject.AddComponent<CharacterAnimationController>();
        }

        return controller;
    }
}
