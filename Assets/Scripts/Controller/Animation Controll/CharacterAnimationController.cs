using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterAnimatorParameters))]
[RequireComponent(typeof(CharacterFacingAnimation))]
[RequireComponent(typeof(CharacterLocomotionAnimation))]
[RequireComponent(typeof(CharacterAttackAnimation))]
public class CharacterAnimationController : MonoBehaviour
{
    [SerializeField] private CharacterAnimatorParameters animatorParameters;
    [SerializeField] private CharacterFacingAnimation facingAnimation;
    [SerializeField] private CharacterLocomotionAnimation locomotionAnimation;
    [SerializeField] private CharacterAttackAnimation attackAnimation;

    public bool IsAttacking => attackAnimation != null && attackAnimation.IsAttacking;

    private void Awake()
    {
        ResolveComponents();

        animatorParameters.Initialize();
        facingAnimation.Initialize(animatorParameters);
        locomotionAnimation.Initialize(animatorParameters, facingAnimation);
        attackAnimation.Initialize(animatorParameters);

        facingAnimation.SetDefaultFacing();
        locomotionAnimation.PlayIdle();
    }

    public void PlayMove(Vector2 moveDirection)
    {
        locomotionAnimation.PlayMove(moveDirection);
    }

    public void PlayIdle()
    {
        locomotionAnimation.PlayIdle();
    }

    public void SetFacingDirection(Vector2 direction)
    {
        facingAnimation.SetFacingDirection(direction);
    }

    public bool PlayAttack()
    {
        return attackAnimation.PlayAttack();
    }

    public void TickAttack(float deltaTime)
    {
        attackAnimation.TickAttack(deltaTime);
    }

    public void StopAttack()
    {
        attackAnimation.StopAttack();
    }

    public bool PlayHurt()
    {
        attackAnimation?.StopAttack();
        return animatorParameters != null && animatorParameters.PlayHurt();
    }

    public bool PlayDeath()
    {
        attackAnimation?.StopAttack();
        locomotionAnimation?.PlayIdle();
        return animatorParameters != null && animatorParameters.PlayDeath();
    }

    public bool PlayIdleState()
    {
        locomotionAnimation?.PlayIdle();
        return animatorParameters != null && animatorParameters.PlayIdleState();
    }

    public float GetClipLengthContaining(string clipNamePart)
    {
        return animatorParameters != null ? animatorParameters.GetClipLengthContaining(clipNamePart) : 0f;
    }

    private void ResolveComponents()
    {
        animatorParameters = GetOrAdd(animatorParameters);
        facingAnimation = GetOrAdd(facingAnimation);
        locomotionAnimation = GetOrAdd(locomotionAnimation);
        attackAnimation = GetOrAdd(attackAnimation);
    }

    private T GetOrAdd<T>(T current) where T : Component
    {
        if (current != null)
        {
            return current;
        }

        T component = GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
