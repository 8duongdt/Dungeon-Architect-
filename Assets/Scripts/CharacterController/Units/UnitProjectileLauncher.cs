using UnityEngine;

/// <summary>
/// Cắm trên unit TẦM XA để đòn đánh thường phóng đạn thay vì trừ máu tức thời -
/// <see cref="AttackState"/> tự phát hiện component này và giao phần gây sát thương cho
/// <see cref="UnitProjectile"/>. Unit cận chiến không gắn thì giữ nguyên đánh thẳng cũ.
/// </summary>
[DisallowMultipleComponent]
public class UnitProjectileLauncher : MonoBehaviour
{
    // Nhô đạn khỏi thân unit một chút theo hướng bắn cho đẹp mắt lúc xuất phát.
    private const float SpawnForwardOffset = 0.4f;

    [Tooltip("Prefab đạn (phải có UnitProjectile).")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Tốc độ bay của đạn (unit/giây).")]
    [SerializeField] private float projectileSpeed = 12f;

    [Tooltip("Nhuộm màu đạn theo unit (trắng = giữ nguyên màu sprite gốc).")]
    [SerializeField] private Color projectileTint = Color.white;

    [Tooltip("Điểm phóng so với gốc unit - nâng lên ngang thân thay vì bắn từ chân.")]
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0f, 0.5f);

    private UnitAudioPlayer audioPlayer;

    private void Awake()
    {
        audioPlayer = GetComponent<UnitAudioPlayer>();
    }

    /// <summary>Phóng đạn về phía mục tiêu; trả false khi thiếu prefab để caller đánh thẳng như cũ.</summary>
    public bool TryLaunch(UnitHealth target, float damage)
    {
        if (projectilePrefab == null || target == null)
        {
            return false;
        }

        Vector3 spawnPosition = ResolveSpawnPosition(target.transform.position);
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        var projectile = projectileObject.GetComponent<UnitProjectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return false;
        }

        ApplyTint(projectileObject);
        projectile.Initialize(target, damage, projectileSpeed, PlayImpactSound);
        return true;
    }

    private Vector3 ResolveSpawnPosition(Vector3 targetPosition)
    {
        Vector3 muzzleWorld = transform.position + (Vector3)muzzleOffset;
        Vector2 towardTarget = ((Vector2)(targetPosition - muzzleWorld)).normalized;
        return muzzleWorld + (Vector3)(towardTarget * SpawnForwardOffset);
    }

    private void ApplyTint(GameObject projectileObject)
    {
        var spriteRenderer = projectileObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = projectileTint;
        }
    }

    private void PlayImpactSound()
    {
        audioPlayer?.PlayHit();
    }

    private void OnValidate()
    {
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
    }
}
