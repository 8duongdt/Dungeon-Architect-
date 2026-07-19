using UnityEngine;

[DisallowMultipleComponent]
public class CharacterFacingAnimation : MonoBehaviour
{
    [SerializeField] private Vector2 defaultFacingDirection = Vector2.down;

    private CharacterAnimatorParameters animatorParameters;
    private Vector2 lastFacingDirection;

    public Vector2 LastFacingDirection => lastFacingDirection;

    public void Initialize(CharacterAnimatorParameters parameters)
    {
        animatorParameters = parameters;
        lastFacingDirection = defaultFacingDirection.sqrMagnitude > 0.0001f
            ? defaultFacingDirection.normalized
            : Vector2.down;
    }

    public void SetDefaultFacing()
    {
        SetFacingDirection(lastFacingDirection);
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (animatorParameters == null || direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        lastFacingDirection = direction.normalized;
        animatorParameters.SetFacing(lastFacingDirection);
    }
}
