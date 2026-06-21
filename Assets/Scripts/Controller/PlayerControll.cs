using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControll : MonoBehaviour, ISpeedModifiable
{
    public float moveSpeed = 5f;
    [SerializeField] private CharacterAnimationController animationController;

    // Hệ số nhân tốc độ do hệ thống hiệu ứng đặt (1 = bình thường).
    private float speedMultiplier = 1f;
    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = Mathf.Max(0f, value);
    }

    private Rigidbody2D rb;
    private UnitHealth health;
    private Vector2 moveInput;
    private Vector2 lastMove = new Vector2(0, -1);

    public bool HasMovementInput => moveInput.sqrMagnitude > 0.0001f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<UnitHealth>();
        animationController = GetAnimationController();
        SetupRigidbody();

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void OnDamaged(UnitHealth source, float amount)
    {
        if (animationController != null)
        {
            animationController.PlayHurt();
        }
    }

    private void OnDied(UnitHealth source)
    {
        if (animationController != null)
        {
            animationController.PlayDeath();
        }
    }

    private void SetupRigidbody()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadMovementInput();
        animationController?.PlayMove(moveInput);
        UpdateFacingDirection();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartAttack();
        }

        animationController?.TickAttack(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        Vector2 movement = moveInput.normalized;
        if (movement == Vector2.zero)
        {
            return;
        }

        if (rb == null)
        {
            transform.position += (Vector3)(movement * moveSpeed * speedMultiplier * Time.fixedDeltaTime);
            return;
        }

        rb.MovePosition(rb.position + movement * moveSpeed * speedMultiplier * Time.fixedDeltaTime);
    }

    private void ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;

        moveInput = Vector2.zero;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.dKey.isPressed)
        {
            moveInput.x = 1;
        }
        else if (keyboard.aKey.isPressed)
        {
            moveInput.x = -1;
        }

        if (keyboard.wKey.isPressed)
        {
            moveInput.y = 1;
        }
        else if (keyboard.sKey.isPressed)
        {
            moveInput.y = -1;
        }
    }

    private void UpdateFacingDirection()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            if (moveInput != Vector2.zero)
            {
                lastMove = moveInput.normalized;
            }

            animationController?.SetFacingDirection(lastMove);
            return;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = -Camera.main.transform.position.z;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 direction = mouseWorldPosition - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = lastMove;
        }
        else
        {
            direction.Normalize();
            lastMove = direction;
        }

        animationController?.SetFacingDirection(direction);
    }

    private void StartAttack()
    {
        animationController?.PlayAttack();
    }

    private CharacterAnimationController GetAnimationController()
    {
        if (animationController != null)
        {
            return animationController;
        }

        CharacterAnimationController controller = GetComponent<CharacterAnimationController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterAnimationController>();
        }

        return controller;
    }
}
