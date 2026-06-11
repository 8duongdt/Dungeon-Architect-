using UnityEngine;

[DisallowMultipleComponent]
public class CharacterAnimatorParameters : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string horizontalParameter = "Horizontal";
    [SerializeField] private string verticalParameter = "Vertical";
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackParameter = "Attack";

    private int horizontalHash;
    private int verticalHash;
    private int speedHash;
    private int attackHash;

    private bool hasHorizontalParameter;
    private bool hasVerticalParameter;
    private bool hasSpeedParameter;
    private bool hasAttackParameter;
    private RuntimeAnimatorController cachedAnimatorController;

    public bool HasAttackParameter => hasAttackParameter;

    private void OnValidate()
    {
        CacheHashes();
    }

    public void Initialize()
    {
        ResolveAnimator();
        CacheParameters();
    }

    public bool IsReady()
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

    public void SetFacing(Vector2 facingDirection)
    {
        if (!IsReady())
        {
            return;
        }

        if (hasHorizontalParameter)
        {
            animator.SetFloat(horizontalHash, facingDirection.x);
        }

        if (hasVerticalParameter)
        {
            animator.SetFloat(verticalHash, facingDirection.y);
        }
    }

    public void SetSpeed(float speed)
    {
        if (IsReady() && hasSpeedParameter)
        {
            animator.SetFloat(speedHash, speed);
        }
    }

    public void SetAttack(bool isAttacking)
    {
        if (IsReady() && hasAttackParameter)
        {
            animator.SetBool(attackHash, isAttacking);
        }
    }

    public float GetClipLengthContaining(string clipNamePart)
    {
        if (!IsReady() || animator.runtimeAnimatorController == null)
        {
            return 0f;
        }

        string loweredNamePart = clipNamePart.ToLowerInvariant();
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name.ToLowerInvariant().Contains(loweredNamePart))
            {
                return clip.length;
            }
        }

        return 0f;
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
}
