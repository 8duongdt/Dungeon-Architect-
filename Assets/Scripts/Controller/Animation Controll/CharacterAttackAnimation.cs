using UnityEngine;

[DisallowMultipleComponent]
public class CharacterAttackAnimation : MonoBehaviour
{
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private bool autoDetectAttackDuration = true;

    private CharacterAnimatorParameters animatorParameters;
    private bool isAttacking;
    private float attackTimer;

    public bool IsAttacking => isAttacking;

    public void Initialize(CharacterAnimatorParameters parameters)
    {
        animatorParameters = parameters;
        if (autoDetectAttackDuration)
        {
            SyncAttackDurationFromClip();
        }
    }

    public bool PlayAttack()
    {
        if (animatorParameters == null || !animatorParameters.IsReady() || !animatorParameters.HasAttackParameter || isAttacking)
        {
            return false;
        }

        isAttacking = true;
        attackTimer = attackDuration;
        animatorParameters.SetAttack(true);
        return true;
    }

    public void TickAttack(float deltaTime)
    {
        if (!isAttacking)
        {
            return;
        }

        attackTimer -= deltaTime;
        if (attackTimer <= 0f)
        {
            StopAttack();
        }
    }

    public void StopAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        isAttacking = false;
        animatorParameters?.SetAttack(false);
    }

    private void SyncAttackDurationFromClip()
    {
        float detectedAttackLength = animatorParameters != null
            ? animatorParameters.GetClipLengthContaining("attack")
            : 0f;

        if (detectedAttackLength > 0f)
        {
            attackDuration = detectedAttackLength;
        }
    }

    private void OnValidate()
    {
        attackDuration = Mathf.Max(0f, attackDuration);
    }
}
