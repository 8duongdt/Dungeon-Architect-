using UnityEngine;

/// <summary>
/// Đòn đánh thường TẦM XA của người chơi: chuột trái phóng một viên đạn bay thẳng theo
/// hướng nhìn (chuột lái qua <see cref="PlayerControll.FacingDirection"/>), trúng kẻ địch
/// đầu tiên mới gây sát thương - Demon Lord không còn vung tay suông. Sát thương + nhịp bắn
/// lấy từ <see cref="UnitStats"/> (thiếu asset thì dùng số dự phòng serialize tại đây).
/// <see cref="PlayerControll.StartAttack"/> gọi TryFire; trả false khi đang hồi để bỏ luôn animation.
/// </summary>
[DisallowMultipleComponent]
public class PlayerRangedAttack : MonoBehaviour
{
    [Tooltip("Prefab đạn đánh thường (phải có SkillProjectile).")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Tốc độ bay của đạn (unit/giây).")]
    [SerializeField] private float projectileSpeed = 14f;

    [Tooltip("Tầm bay tối đa của đạn (unit).")]
    [SerializeField] private float maxRange = 8f;

    [Tooltip("Sát thương dự phòng khi nhân vật không có UnitStats/asset chỉ số.")]
    [SerializeField] private float fallbackDamage = 10f;

    [Tooltip("Thời gian hồi dự phòng giữa hai phát bắn khi thiếu UnitStats (giây).")]
    [SerializeField] private float fallbackCooldown = 0.5f;

    [Tooltip("Điểm phóng so với gốc nhân vật - nâng lên ngang thân thay vì bắn từ chân.")]
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0f, 0.5f);

    private PlayerControll playerControll;
    private UnitFaction faction;
    private UnitStats stats;
    private float nextFireTime;

    private void Awake()
    {
        playerControll = GetComponent<PlayerControll>();
        faction = GetComponent<UnitFaction>();
        stats = GetComponent<UnitStats>();
    }

    /// <summary>Bắn một viên theo hướng nhìn; trả false khi đang hồi hoặc thiếu prefab.</summary>
    public bool TryFire()
    {
        bool isReady = projectilePrefab != null && Time.time >= nextFireTime;
        if (!isReady)
        {
            return false;
        }

        Vector3 spawnPosition = transform.position + (Vector3)muzzleOffset;
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        var projectile = projectileObject.GetComponent<SkillProjectile>();
        if (projectile == null)
        {
            Destroy(projectileObject);
            return false;
        }

        projectile.Initialize(faction, ResolveDamage(), ResolveFireDirection(),
            projectileSpeed, maxRange, impactVfx: null);
        nextFireTime = Time.time + ResolveCooldown();
        return true;
    }

    private Vector2 ResolveFireDirection()
    {
        return playerControll != null ? playerControll.FacingDirection : Vector2.right;
    }

    private float ResolveDamage()
    {
        bool hasStats = stats != null && stats.Stats != null;
        return hasStats ? stats.Stats.AttackDamage : fallbackDamage;
    }

    private float ResolveCooldown()
    {
        bool hasStats = stats != null && stats.Stats != null;
        return hasStats ? stats.Stats.AttackCooldown : fallbackCooldown;
    }

    private void OnValidate()
    {
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        maxRange = Mathf.Max(0.1f, maxRange);
        fallbackDamage = Mathf.Max(0f, fallbackDamage);
        fallbackCooldown = Mathf.Max(0.05f, fallbackCooldown);
    }
}
