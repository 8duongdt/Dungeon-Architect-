using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControll : MonoBehaviour
{
    public float moveSpeed = 5f;
    [SerializeField] private float attackDuration = 0.2f;


    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMove = new Vector2(0, -1); 
    private Animator animator;
    private bool isAttacking;
    private float attackTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Auto-sync with the attack clip length to avoid cutting animation early.
        float detectedAttackLength = GetAttackClipLength();
        if (detectedAttackLength > 0f)
        {
            attackDuration = detectedAttackLength;
        }

        // Nếu dùng Rigidbody2D để di chuyển, nên để Gravity Scale = 0 
        // và đóng băng trục Z (Freeze Rotation Z) trong Inspector.
    }

    private void Update()
    {
        var keyboard = Keyboard.current;

        moveInput = Vector2.zero;
        if (keyboard != null)
        {
            if (keyboard.dKey.isPressed) moveInput.x = 1;
            else if (keyboard.aKey.isPressed) moveInput.x = -1;

            if (keyboard.wKey.isPressed) moveInput.y = 1;
            else if (keyboard.sKey.isPressed) moveInput.y = -1;
        }

        //Cập nhật Animator
        animator.SetFloat("Speed", moveInput.magnitude);

        UpdateFacingDirection();

        // if (PauseController.IsGamePaused || isWaiting)
        // {
        //     animator.SetBool("isRunning", false);
        //     return;
        // }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartAttack();
        }

        UpdateAttackState();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 movement = moveInput.normalized;
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void UpdateFacingDirection()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            if (moveInput != Vector2.zero)
            {
                lastMove = moveInput.normalized;
            }

            animator.SetFloat("Horizontal", lastMove.x);
            animator.SetFloat("Vertical", lastMove.y);
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

        animator.SetFloat("Horizontal", direction.x);
        animator.SetFloat("Vertical", direction.y);
    }

    private void StartAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        attackTimer = attackDuration;
        animator.SetBool("Attack", true);
    }

    private void UpdateAttackState()
    {
        if (!isAttacking) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        isAttacking = false;
        animator.SetBool("Attack", false);
    }

    private float GetAttackClipLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return 0f;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name.ToLower().Contains("attack"))
            {
                return clip.length;
            }
        }

        return 0f;
    }
}
