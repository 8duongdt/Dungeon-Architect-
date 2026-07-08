using UnityEngine;

/// <summary>
/// Định nghĩa một kỹ năng phép: hiệu ứng VFX + thông số sát thương.
/// Sát thương trả về ở đây là TRƯỚC giáp - giáp được trừ tập trung trong
/// <see cref="UnitHealth.TakeDamage"/> để mọi nguồn sát thương đều được giảm trừ như nhau.
/// </summary>
[CreateAssetMenu(fileName = "Skill", menuName = "Skills/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject
{
    [Tooltip("Tên hiển thị của kỹ năng.")]
    [SerializeField] private string skillName;

    [Tooltip("Icon hiển thị trên thanh kỹ năng của HUD.")]
    [SerializeField] private Sprite icon;

    [Tooltip("Prefab hiệu ứng phép (SkillVfxOneShot) sinh ra ngay trên mục tiêu.")]
    [SerializeField] private GameObject vfxPrefab;

    [Tooltip("Sát thương gốc của kỹ năng.")]
    [SerializeField] private float baseDamage = 15f;

    [Tooltip("Hệ số cộng thêm theo sức mạnh phép của người thi triển (magicPower).")]
    [SerializeField] private float statScaling = 1f;

    [Tooltip("Thời gian hồi kỹ năng (giây).")]
    [SerializeField] private float cooldown = 5f;

    [Tooltip("Độ trễ từ lúc hiệu ứng xuất hiện tới lúc gây sát thương (0 = tức thì).")]
    [SerializeField] private float impactDelay = 0f;

    [Tooltip("Mana tiêu tốn khi NGƯỜI CHƠI thi triển (unit AI cast miễn phí, bỏ qua giá trị này).")]
    [SerializeField] private int manaCost = 0;

    [Header("Cơ chế")]
    [Tooltip("Cách kỹ năng thi triển: đánh tức thời / đạn bay / nảy chuỗi.")]
    [SerializeField] private SkillMechanic mechanic = SkillMechanic.InstantStrike;

    [Header("Projectile (chỉ dùng khi mechanic = Projectile)")]
    [Tooltip("Prefab đạn (SkillProjectile) bay từ người thi triển tới mục tiêu.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Tốc độ bay của đạn (world unit/giây).")]
    [SerializeField] private float projectileSpeed = 6f;
    [Tooltip("Tầm bay tối đa trước khi đạn tự hủy.")]
    [SerializeField] private float projectileMaxRange = 12f;

    [Header("Chain (chỉ dùng khi mechanic = ChainStrike)")]
    [Tooltip("Số kẻ địch tối đa bị nảy sang sau mục tiêu chính.")]
    [SerializeField] private int maxChainTargets = 2;
    [Tooltip("Bán kính tìm kẻ địch để nảy, tính từ mục tiêu chính.")]
    [SerializeField] private float chainRadius = 3f;
    [Tooltip("Hệ số sát thương của mỗi cú nảy (0.5 = một nửa đòn chính).")]
    [SerializeField] private float chainDamageFactor = 0.5f;

    [Header("Area (chỉ dùng khi mechanic = AreaStrike)")]
    [Tooltip("Bán kính vụ nổ quanh vị trí mục tiêu.")]
    [SerializeField] private float areaRadius = 2.5f;
    [Tooltip("Quãng đường đẩy lùi kẻ địch khỏi tâm nổ (0 = không đẩy).")]
    [SerializeField] private float knockbackDistance = 1.5f;

    [Header("Zone (chỉ dùng khi mechanic = DamageZone)")]
    [Tooltip("Prefab vùng hiệu ứng (SkillDamageZone) đặt tại vị trí mục tiêu.")]
    [SerializeField] private GameObject zonePrefab;
    [Tooltip("Thời gian vùng tồn tại (giây).")]
    [SerializeField] private float zoneDuration = 3f;
    [Tooltip("Nhịp gây sát thương trong vùng (giây/lần).")]
    [SerializeField] private float zoneTickInterval = 0.5f;
    [Tooltip("Bán kính vùng.")]
    [SerializeField] private float zoneRadius = 1.8f;
    [Tooltip("Hệ số sát thương mỗi nhịp so với sát thương skill (0.3 = 30%).")]
    [SerializeField] private float zoneTickDamageFactor = 0.3f;
    [Tooltip("Thời gian choáng mỗi nhịp cho kẻ địch trong vùng (0 = không choáng).")]
    [SerializeField] private float zoneStunDuration = 0f;

    [Header("Trap (chỉ dùng khi mechanic = Trap)")]
    [Tooltip("Prefab bẫy (SkillTrap) đặt tại vị trí mục tiêu.")]
    [SerializeField] private GameObject trapPrefab;
    [Tooltip("Thời gian chờ trước khi bẫy có hiệu lực (giây).")]
    [SerializeField] private float trapArmDelay = 0.5f;
    [Tooltip("Thời gian trói chân kẻ dẫm bẫy (giây).")]
    [SerializeField] private float trapRootDuration = 1.2f;

    [Header("Pull (chỉ dùng khi mechanic = PullVortex)")]
    [Tooltip("Prefab xoáy hút (SkillBlackHole) đặt tại vị trí mục tiêu.")]
    [SerializeField] private GameObject vortexPrefab;
    [Tooltip("Bán kính hút.")]
    [SerializeField] private float pullRadius = 4f;
    [Tooltip("Thời gian xoáy tồn tại (giây).")]
    [SerializeField] private float pullDuration = 2.5f;
    [Tooltip("Tốc độ kéo kẻ địch về tâm (world unit/giây).")]
    [SerializeField] private float pullSpeed = 4f;

    [Header("Shield (chỉ dùng khi mechanic = SelfShield)")]
    [Tooltip("Lượng sát thương khiên hấp thụ.")]
    [SerializeField] private float shieldAmount = 50f;
    [Tooltip("Thời gian khiên tồn tại (giây).")]
    [SerializeField] private float shieldDuration = 6f;

    [Header("Execute (chỉ dùng khi mechanic = ExecuteStrike)")]
    [Tooltip("Ngưỡng máu (theo tỉ lệ) để kết liễu ngay: 0.1 = dưới 10% máu tối đa.")]
    [SerializeField] private float executeHealthThreshold = 0.1f;
    [Tooltip("Vàng thưởng khi kết liễu thành công.")]
    [SerializeField] private int executeGoldReward = 25;

    public string SkillName => skillName;
    public Sprite Icon => icon;
    public GameObject VfxPrefab => vfxPrefab;
    public float Cooldown => cooldown;
    public float ImpactDelay => impactDelay;
    public int ManaCost => manaCost;
    public SkillMechanic Mechanic => mechanic;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileMaxRange => projectileMaxRange;
    public int MaxChainTargets => maxChainTargets;
    public float ChainRadius => chainRadius;
    public float ChainDamageFactor => chainDamageFactor;
    public float AreaRadius => areaRadius;
    public float KnockbackDistance => knockbackDistance;
    public GameObject ZonePrefab => zonePrefab;
    public float ZoneDuration => zoneDuration;
    public float ZoneTickInterval => zoneTickInterval;
    public float ZoneRadius => zoneRadius;
    public float ZoneTickDamageFactor => zoneTickDamageFactor;
    public float ZoneStunDuration => zoneStunDuration;
    public GameObject TrapPrefab => trapPrefab;
    public float TrapArmDelay => trapArmDelay;
    public float TrapRootDuration => trapRootDuration;
    public GameObject VortexPrefab => vortexPrefab;
    public float PullRadius => pullRadius;
    public float PullDuration => pullDuration;
    public float PullSpeed => pullSpeed;
    public float ShieldAmount => shieldAmount;
    public float ShieldDuration => shieldDuration;
    public float ExecuteHealthThreshold => executeHealthThreshold;
    public int ExecuteGoldReward => executeGoldReward;

    /// <summary>Sát thương trước giáp = gốc + sức mạnh phép x hệ số.</summary>
    public float ComputeDamage(float casterStat)
    {
        return baseDamage + casterStat * statScaling;
    }

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0f, baseDamage);
        statScaling = Mathf.Max(0f, statScaling);
        cooldown = Mathf.Max(0f, cooldown);
        impactDelay = Mathf.Max(0f, impactDelay);
        manaCost = Mathf.Max(0, manaCost);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileMaxRange = Mathf.Max(0f, projectileMaxRange);
        maxChainTargets = Mathf.Max(0, maxChainTargets);
        chainRadius = Mathf.Max(0f, chainRadius);
        chainDamageFactor = Mathf.Max(0f, chainDamageFactor);
        areaRadius = Mathf.Max(0f, areaRadius);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
        zoneDuration = Mathf.Max(0f, zoneDuration);
        zoneTickInterval = Mathf.Max(0.05f, zoneTickInterval);
        zoneRadius = Mathf.Max(0f, zoneRadius);
        zoneTickDamageFactor = Mathf.Max(0f, zoneTickDamageFactor);
        zoneStunDuration = Mathf.Max(0f, zoneStunDuration);
        trapArmDelay = Mathf.Max(0f, trapArmDelay);
        trapRootDuration = Mathf.Max(0f, trapRootDuration);
        pullRadius = Mathf.Max(0f, pullRadius);
        pullDuration = Mathf.Max(0f, pullDuration);
        pullSpeed = Mathf.Max(0f, pullSpeed);
        shieldAmount = Mathf.Max(0f, shieldAmount);
        shieldDuration = Mathf.Max(0f, shieldDuration);
        executeHealthThreshold = Mathf.Clamp01(executeHealthThreshold);
        executeGoldReward = Mathf.Max(0, executeGoldReward);
    }
}
