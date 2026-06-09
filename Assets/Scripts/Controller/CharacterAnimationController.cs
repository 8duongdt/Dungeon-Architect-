using UnityEngine;

[DisallowMultipleComponent]
public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private bool autoDetectAttackDuration = true;

    [Header("Animator Parameters")]
    [SerializeField] private string horizontalParameter = "Horizontal";
    [SerializeField] private string verticalParameter = "Vertical";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackParameter = "Attack";

    private readonly Vector2 defaultFacingDirection = Vector2.down;

    private int horizontalHash;
    private int verticalHash;
    private int speedHash;
    private int attackHash;

    private bool hasHorizontalParameter;
    private bool hasVerticalParameter;
    private bool hasSpeedParameter;
    private bool hasAttackParameter;

    private RuntimeAnimatorController cachedAnimatorController;
    private bool isAttacking;
    private float attackTimer;
    private Vector2 lastFacingDirection;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        lastFacingDirection = defaultFacingDirection;
        ResolveAnimator();
        CacheParameters();

        if (autoDetectAttackDuration)
        {
            SyncAttackDurationFromClip();
        }

        SetFacingDirection(lastFacingDirection);
        PlayIdle();
    }

    private void OnValidate()
    {
        CacheHashes();
    }

    public void PlayMove(Vector2 moveDirection)
    {
        if (!IsAnimatorReady())
        {
            return;
        }

        SetSpeed(moveDirection.magnitude);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            SetFacingDirection(moveDirection);
        }
    }

    public void PlayIdle()
    {
        if (!IsAnimatorReady())
        {
            return;
        }

        SetSpeed(0f);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (!IsAnimatorReady() || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        lastFacingDirection = direction.normalized;

        if (hasHorizontalParameter)
        {
            animator.SetFloat(horizontalHash, lastFacingDirection.x);
        }

        if (hasVerticalParameter)
        {
            animator.SetFloat(verticalHash, lastFacingDirection.y);
        }
    }

    public bool PlayAttack()
    {
        if (!IsAnimatorReady() || !hasAttackParameter || isAttacking)
        {
            return false;
        }

        isAttacking = true;
        attackTimer = attackDuration;
        animator.SetBool(attackHash, true);
        return true;
    }

    public void TickAttack(float deltaTime)
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= deltaTime;
        if (attackTimer > 0f)
        {
            return;
        }

        StopAttack();
    }

    public void StopAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        isAttacking = false;

        if (IsAnimatorReady() && hasAttackParameter)
        {
            animator.SetBool(attackHash, false);
        }
    }

    private void SetSpeed(float speed)
    {
        if (hasSpeedParameter)
        {
            animator.SetFloat(speedHash, speed);
        }
    }

    private bool IsAnimatorReady()
    {
        if (animator == null)
        {
            ResolveAnimator();
            CacheParameters();
        }

        if (animator != null && cachedAnimatorController != animator.runtimeAnimatorController)
        {
            CacheParameters();
        }

        return animator != null;
    }

    private void ResolveAnimator()
    {
        if (animator != null)
        {
            return;
        }

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void CacheParameters()
    {
        CacheHashes();

        cachedAnimatorController = animator != null ? animator.runtimeAnimatorController : null;
        hasHorizontalParameter = false;
        hasVerticalParameter = false;
        hasSpeedParameter = false;
        hasAttackParameter = false;

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == horizontalHash && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasHorizontalParameter = true;
            }
            else if (parameter.nameHash == verticalHash && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasVerticalParameter = true;
            }
            else if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
            }
            else if (parameter.nameHash == attackHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasAttackParameter = true;
            }
        }
    }

    private void CacheHashes()
    {
        horizontalHash = Animator.StringToHash(horizontalParameter ?? string.Empty);
        verticalHash = Animator.StringToHash(verticalParameter ?? string.Empty);
        speedHash = Animator.StringToHash(speedParameter ?? string.Empty);
        attackHash = Animator.StringToHash(attackParameter ?? string.Empty);
    }

    private void SyncAttackDurationFromClip()
    {
        float detectedAttackLength = GetAttackClipLength();
        if (detectedAttackLength > 0f)
        {
            attackDuration = detectedAttackLength;
        }
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
            if (clip != null && clip.name.ToLowerInvariant().Contains("attack"))
            {
                return clip.length;
            }
        }

        return 0f;
    }
}
