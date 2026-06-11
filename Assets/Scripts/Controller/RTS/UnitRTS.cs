using UnityEngine;

public class UnitRTS : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.1f;

    private GameObject selectedVisual;
    private CharacterAnimationController animationController;
    private RTSUnitAI unitAI;
    private Rigidbody2D rb;
    private Vector3 targetPosition;
    private Vector2 currentMoveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animationController = GetAnimationController();
        unitAI = GetComponent<RTSUnitAI>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
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

        if (unitAI != null && unitAI.HasActiveCombatTarget)
        {
            return;
        }

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
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
        if (unitAI != null && unitAI.HasActiveCombatTarget)
        {
            targetPosition = transform.position;
            currentMoveDirection = Vector2.zero;
            unitAI.CompleteMoveCommand();
            return;
        }

        Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 targetPosition2D = targetPosition;
        Vector2 toTarget = targetPosition2D - currentPosition;

        if (toTarget.sqrMagnitude <= stoppingDistance * stoppingDistance)
        {
            currentMoveDirection = Vector2.zero;
            unitAI?.CompleteMoveCommand();
            return;
        }

        currentMoveDirection = toTarget.normalized;
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition2D,
            moveSpeed * Time.fixedDeltaTime
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
