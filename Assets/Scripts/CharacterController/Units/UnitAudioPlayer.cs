using UnityEngine;

/// <summary>
/// Façade âm thanh của một unit (người chơi hoặc AI): vung vũ khí, trúng đòn, bị thương, chết -
/// cùng cấu trúc với <see cref="CharacterAnimationController"/> (gameplay code chỉ gọi PlaySwing/PlayHit,
/// còn tự lắng nghe UnitHealth để phát tiếng bị thương/chết). Tách khỏi AttackState/UnitHealth để hai
/// lớp đó không phải ôm mảng AudioClip. (Không phát tiếng bước chân.)
/// </summary>
[DisallowMultipleComponent]
public class UnitAudioPlayer : MonoBehaviour
{
    [Header("Chiến đấu")]
    [SerializeField] private AudioClip[] swingClips;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private AudioClip[] deathClips;

    private UnitHealth health;

    private void Awake()
    {
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
}
