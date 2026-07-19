using UnityEngine;

[DisallowMultipleComponent]
public class CharacterLocomotionAnimation : MonoBehaviour
{
    private CharacterAnimatorParameters animatorParameters;
    private CharacterFacingAnimation facingAnimation;

    public void Initialize(CharacterAnimatorParameters parameters, CharacterFacingAnimation facing)
    {
        animatorParameters = parameters;
        facingAnimation = facing;
    }

    public void PlayMove(Vector2 moveDirection)
    {
        if (animatorParameters == null)
        {
            return;
        }

        animatorParameters.SetSpeed(moveDirection.magnitude);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            facingAnimation?.SetFacingDirection(moveDirection);
        }
    }

    public void PlayIdle()
    {
        animatorParameters?.SetSpeed(0f);
    }
}
