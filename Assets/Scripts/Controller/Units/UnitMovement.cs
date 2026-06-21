using UnityEngine;

[DisallowMultipleComponent]
public class UnitMovement : MonoBehaviour, ISpeedModifiable
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [Tooltip("Lực hãm vận tốc dư do va chạm. Càng cao thì unit càng không bị đẩy trôi đi (vẫn va chạm chống đè lên nhau).")]
    [SerializeField] private float collisionRecoveryDamping = 12f;
    [SerializeField] private CharacterAnimationController animationController;

    private Rigidbody2D rb;

    // Hệ số nhân tốc độ do hệ thống hiệu ứng đặt (1 = bình thường). Áp cho lúc AI đuổi đánh.
    private float speedMultiplier = 1f;
    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = Mathf.Max(0f, value);
    }

    // Tốc độ gốc theo cấu hình (không gồm hệ số hiệu ứng).
    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animationController = GetAnimationController();

        if (rb != null)
        {
            // Giữ va chạm (chống đè lên nhau) nhưng dập tắt quán tính sau mỗi cú va
            // để unit không bị đẩy trôi đi.
            rb.linearDamping = Mathf.Max(rb.linearDamping, collisionRecoveryDamping);
        }
    }

    public void MoveTowards(Vector3 targetPosition)
    {
        MoveTowards(targetPosition, stoppingDistance);
    }

    public void MoveTowards(Vector3 targetPosition, float targetStopDistance)
    {
        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 toTarget = (Vector2)targetPosition - currentPosition;
        float effectiveStoppingDistance = Mathf.Max(0f, targetStopDistance);
        if (toTarget.sqrMagnitude <= effectiveStoppingDistance * effectiveStoppingDistance)
        {
            Stop();
            return;
        }

        Vector2 direction = toTarget.normalized;
        animationController?.PlayMove(direction);

        if (rb != null)
        {
            // Bù phần vận tốc bị linearDamping hãm bớt để tốc độ di chuyển chủ động
            // không bị chậm đi, trong khi quán tính do va chạm vẫn tắt nhanh.
            float dampingCompensation = 1f + rb.linearDamping * Time.fixedDeltaTime;
            rb.linearVelocity = direction * moveSpeed * speedMultiplier * dampingCompensation;
        }
        else
        {
            transform.position += (Vector3)(direction * moveSpeed * speedMultiplier * Time.deltaTime);
        }
    }

    public void Stop(bool playIdleAnimation = true)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (playIdleAnimation)
        {
            animationController?.PlayIdle();
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
        collisionRecoveryDamping = Mathf.Max(0f, collisionRecoveryDamping);
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
