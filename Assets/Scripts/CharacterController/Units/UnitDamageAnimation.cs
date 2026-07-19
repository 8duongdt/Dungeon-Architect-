using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
public class UnitDamageAnimation : MonoBehaviour
{
    [SerializeField] private CharacterAnimationController animationController;
    [SerializeField] private bool returnToIdleAfterHurt = true;
    [SerializeField] private float hurtDuration = 0.25f;
    [SerializeField] private bool autoDetectHurtDuration = true;

    private UnitHealth health;
    private Coroutine hurtCoroutine;

    private void Awake()
    {
        ResolveComponents();
        SyncHurtDurationFromClip();
    }

    private void OnEnable()
    {
        ResolveComponents();
        SyncHurtDurationFromClip();

        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        StopHurtCoroutine();
    }

    private void HandleDamaged(UnitHealth damagedHealth, float damageAmount)
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        bool playedHurt = animationController != null && animationController.PlayHurt();
        if (playedHurt && returnToIdleAfterHurt && hurtDuration > 0f)
        {
            StopHurtCoroutine();
            hurtCoroutine = StartCoroutine(ReturnToIdleAfterHurt());
        }
    }

    private void HandleDied(UnitHealth deadHealth)
    {
        StopHurtCoroutine();
        animationController?.PlayDeath();
    }

    private void ResolveComponents()
    {
        if (health == null)
        {
            health = GetComponent<UnitHealth>();
        }

        animationController = GetAnimationController();
    }

    private void SyncHurtDurationFromClip()
    {
        if (!autoDetectHurtDuration || animationController == null)
        {
            return;
        }

        float detectedHurtDuration = animationController.GetClipLengthContaining("hurt");
        if (detectedHurtDuration > 0f)
        {
            hurtDuration = detectedHurtDuration;
        }
    }

    private IEnumerator ReturnToIdleAfterHurt()
    {
        yield return new WaitForSeconds(hurtDuration);
        hurtCoroutine = null;

        if (health == null || !health.IsDead)
        {
            animationController?.PlayIdleState();
        }
    }

    private void StopHurtCoroutine()
    {
        if (hurtCoroutine == null)
        {
            return;
        }

        StopCoroutine(hurtCoroutine);
        hurtCoroutine = null;
    }

    private void OnValidate()
    {
        hurtDuration = Mathf.Max(0f, hurtDuration);
    }

    private CharacterAnimationController GetAnimationController()
    {
        if (animationController != null)
        {
            return animationController;
        }

        CharacterAnimationController controller = GetComponent<CharacterAnimationController>();
        if (controller == null && GetComponentInChildren<Animator>() != null)
        {
            controller = gameObject.AddComponent<CharacterAnimationController>();
        }

        return controller;
    }
}
