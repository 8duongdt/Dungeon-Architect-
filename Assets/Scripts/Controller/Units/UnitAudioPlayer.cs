using UnityEngine;

/// <summary>
/// Façade âm thanh của một unit (người chơi hoặc AI): vung vũ khí, trúng đòn, bị thương, chết,
/// bước chân - cùng cấu trúc với <see cref="CharacterAnimationController"/> (gameplay code chỉ
/// gọi PlaySwing/PlayHit, còn tự lắng nghe UnitHealth để phát tiếng bị thương/chết). Tách khỏi
/// AttackState/UnitHealth để hai lớp đó không phải ôm mảng AudioClip.
/// </summary>
[DisallowMultipleComponent]
public class UnitAudioPlayer : MonoBehaviour
{
    private const float MovingVelocitySqrThreshold = 0.01f;

    [Header("Chiến đấu")]
    [SerializeField] private AudioClip[] swingClips;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private AudioClip[] deathClips;

    [Header("Di chuyển")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.35f;

    private UnitHealth health;
    private Rigidbody2D rb;
    private float footstepTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<UnitHealth>();
        if (health != null)
        {
            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }
    }

    private void Update()
    {
        UpdateFootsteps();
    }

    public void PlaySwing()
    {
        AudioManager.Instance.PlayRandom(swingClips);
    }

    public void PlayHit()
    {
        AudioManager.Instance.PlayRandom(hitClips);
    }

    private void HandleDamaged(UnitHealth source, float amount)
    {
        AudioManager.Instance.PlayRandom(hurtClips);
    }

    private void HandleDied(UnitHealth source)
    {
        AudioManager.Instance.PlayRandom(deathClips);
    }

    private void UpdateFootsteps()
    {
        if (!IsMoving())
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
        {
            return;
        }

        AudioManager.Instance.PlayRandom(footstepClips);
        footstepTimer = footstepInterval;
    }

    private bool IsMoving()
    {
        return rb != null && rb.linearVelocity.sqrMagnitude > MovingVelocitySqrThreshold;
    }

    private void OnValidate()
    {
        footstepInterval = Mathf.Max(0.05f, footstepInterval);
    }
}
