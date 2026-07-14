using UnityEngine;

/// <summary>
/// Áp buff nhánh HỖ TRỢ CHIẾN ĐẤU của cây công trình lên nhân vật chính khi vào Phase 2:
/// khiên HP cấp sẵn cả trận, hệ số sát thương skill và hồi máu mỗi giây.
/// Gắn lên avatar trong scene Phase 2 (Phase2BattleSetup tự attach), theo mẫu EquippedSkillLoader.
/// Chạy ở Start (sau Awake của UnitHealth) để máu/khiên đã khởi tạo xong.
/// Chưa nâng node nào thì mọi giá trị trung tính - component vô hại.
/// </summary>
[DisallowMultipleComponent]
public class Phase2BuffApplier : MonoBehaviour
{
    // Khiên từ công trình tồn tại cả trận (AddShield yêu cầu thời hạn hữu hạn).
    private const float ShieldDurationSeconds = 9999f;

    [Tooltip("Máu của người chơi nhận khiên/hồi máu (bỏ trống = lấy trên cùng GameObject).")]
    [SerializeField] private UnitHealth playerHealth;

    [Tooltip("Caster của người chơi nhận buff sát thương (bỏ trống = lấy trên cùng GameObject).")]
    [SerializeField] private PlayerSkillCaster caster;

    private float regenPerSecond;

    private void Start()
    {
        ResolveComponents();
        ApplyShield();
        ApplyDamageBuff();
        regenPerSecond = BuildingProgressionEffects.GetPhase2RegenPerSecond();
    }

    private void Update()
    {
        bool canRegen = regenPerSecond > 0f && playerHealth != null && !playerHealth.IsDead;
        if (canRegen)
        {
            playerHealth.Heal(regenPerSecond * Time.deltaTime);
        }
    }

    private void ResolveComponents()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<UnitHealth>();
        }

        if (caster == null)
        {
            caster = GetComponent<PlayerSkillCaster>();
        }
    }

    private void ApplyShield()
    {
        float shieldAmount = BuildingProgressionEffects.GetPhase2ShieldAmount();
        if (shieldAmount > 0f && playerHealth != null)
        {
            playerHealth.AddShield(shieldAmount, ShieldDurationSeconds);
        }
    }

    private void ApplyDamageBuff()
    {
        if (caster != null)
        {
            caster.ExternalDamageMultiplier = BuildingProgressionEffects.GetPhase2DamageMultiplier();
        }
    }
}
