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
    [SerializeField] private string hurtTriggerParameter = "takeDamage";
    [SerializeField] private string deathTriggerParameter = "die";

    [Header("Animator States")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string hurtStateName = "hurt";
    [SerializeField] private string deathStateName = "death";
    [SerializeField] private float stateTransitionDuration = 0.05f;

    private int horizontalHash;
    private int verticalHash;
    private int speedHash;
    private int attackHash;
    private int hurtTriggerHash;
    private int deathTriggerHash;

    private bool hasHorizontalParameter;
    private bool hasVerticalParameter;
    private bool hasSpeedParameter;
    private bool hasAttackParameter;
    private bool hasHurtTriggerParameter;
    private bool hasDeathTriggerParameter;
    private RuntimeAnimatorController cachedAnimatorController;

    public bool HasAttackParameter => hasAttackParameter;

    private void OnValidate()
    {
        CacheHashes();
        stateTransitionDuration = Mathf.Max(0f, stateTransitionDuration);
    }

    public void Initialize()
    {
        ResolveAnimator();
        stateTransitionDuration = Mathf.Max(0f, stateTransitionDuration);
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

    public bool PlayHurt()
    {
        if (SetTriggerIfAvailable(hasHurtTriggerParameter, hurtTriggerHash))
        {
            return true;
        }

        return PlayStateIfExists(hurtStateName, "hurt", "Hurt", "hurt_s", "hurt_d", "hurt_w", "hurt_a");
    }

    public bool PlayDeath()
    {
        if (SetTriggerIfAvailable(hasDeathTriggerParameter, deathTriggerHash))
        {
            return true;
        }

        return PlayStateIfExists(deathStateName, "death", "Death", "death_s", "death_d", "death_w", "death_a");
    }

    public bool PlayIdleState()
    {
        return PlayStateIfExists(idleStateName, "idle", "Idle");
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

    // Trả về độ dài clip DÀI NHẤT khớp tên (vd nhiều hướng attack_w/attack_s...),
    // để thời lượng đòn đánh không bị cắt ngắn theo clip ngắn nhất.
    public float GetLongestClipLengthContaining(string clipNamePart)
    {
        if (!IsReady() || animator.runtimeAnimatorController == null)
        {
            return 0f;
        }

        string loweredNamePart = clipNamePart.ToLowerInvariant();
        float longestLength = 0f;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name.ToLowerInvariant().Contains(loweredNamePart) && clip.length > longestLength)
            {
                longestLength = clip.length;
            }
        }

        return longestLength;
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
        hasHurtTriggerParameter = false;
        hasDeathTriggerParameter = false;

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
            else if (parameter.nameHash == hurtTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasHurtTriggerParameter = true;
            }
            else if (parameter.nameHash == deathTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasDeathTriggerParameter = true;
            }
        }
    }

    private void CacheHashes()
    {
        horizontalHash = Animator.StringToHash(horizontalParameter ?? string.Empty);
        verticalHash = Animator.StringToHash(verticalParameter ?? string.Empty);
        speedHash = Animator.StringToHash(speedParameter ?? string.Empty);
        attackHash = Animator.StringToHash(attackParameter ?? string.Empty);
        hurtTriggerHash = Animator.StringToHash(hurtTriggerParameter ?? string.Empty);
        deathTriggerHash = Animator.StringToHash(deathTriggerParameter ?? string.Empty);
    }

    private bool SetTriggerIfAvailable(bool hasParameter, int parameterHash)
    {
        if (!hasParameter || !IsReady())
        {
            return false;
        }

        animator.SetTrigger(parameterHash);
        return true;
    }

    private bool PlayStateIfExists(params string[] stateNames)
    {
        if (!IsReady())
        {
            return false;
        }

        foreach (string stateName in stateNames)
        {
            if (TryPlayState(stateName))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryPlayState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int shortStateHash = Animator.StringToHash(stateName);
        if (TryPlayStateHash(shortStateHash))
        {
            return true;
        }

        int fullStateHash = Animator.StringToHash($"Base Layer.{stateName}");
        return TryPlayStateHash(fullStateHash);
    }

    private bool TryPlayStateHash(int stateHash)
    {
        if (!animator.HasState(0, stateHash))
        {
            return false;
        }

        animator.CrossFadeInFixedTime(stateHash, stateTransitionDuration, 0);
        return true;
    }
}
