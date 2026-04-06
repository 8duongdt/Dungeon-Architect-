using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControll : MonoBehaviour
{
    public float moveSpeed = 5f;
    [SerializeField] private float attackDuration = 0.2f;


    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 lastMove = new Vector2(0, -1); // Mặc định nhìn xuống dưới khi bắt đầu
    private Animator animator;
    private bool isAttacking;
    private float attackTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

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

        //Di chuyển nhân vật
        Vector2 movement = moveInput.normalized;
        transform.position += (Vector3)movement * moveSpeed * Time.deltaTime;

        //Cập nhật Animator
        animator.SetFloat("Speed", moveInput.magnitude);

        if (moveInput != Vector2.zero)
        {
            //lưu hướng quay mặt cuối cùng
            lastMove = moveInput;

            // Gửi thông số hướng vào Animator
            animator.SetFloat("Horizontal", lastMove.x);
            animator.SetFloat("Vertical", lastMove.y);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartAttack();
        }

        UpdateAttackState();
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
