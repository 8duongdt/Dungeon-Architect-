using UnityEngine;

/// <summary>
/// Tự động thi triển kỹ năng phép khi unit đang chiến đấu: cứ mỗi cooldown giây,
/// sinh VFX ngay trên mục tiêu đang bị tấn công và gây sát thương
/// (gốc + magicPower x hệ số) - giáp mục tiêu (giáp trừ trong UnitHealth.TakeDamage).
///
/// Component đứng cạnh UnitAI, KHÔNG sửa luồng state: chỉ đọc trạng thái công khai
/// (currentState, TargetEnemy, HasActiveCombatTarget) nên không ảnh hưởng lệnh di chuyển
/// thủ công (HasActiveCombatTarget đã loại trường hợp ignoringCombatForMoveCommand).
/// </summary>
[DisallowMultipleComponent]
public class UnitSkillCaster : MonoBehaviour, IDamageOutputModifiable
{
    [Tooltip("Kỹ năng của unit này (asset trong Assets/Skills).")]
    [SerializeField] private SkillDefinitionSO skill;

    [Tooltip("Sức mạnh phép - chỉ số nhân với statScaling của kỹ năng.")]
    [SerializeField] private float magicPower = 10f;

    private UnitAI ai;
    private AttackAreaBase attackArea;
    private float nextCastTime;
    private float damageOutputMultiplier = 1f;

    public float MagicPower => magicPower;

    // Hệ số nhân sát thương (1 = gốc) - đặt bởi UnitEffectModifier, dùng chung với đòn thường.
    public float DamageOutputMultiplier
    {
        get => damageOutputMultiplier;
        set => damageOutputMultiplier = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        ai = GetComponent<UnitAI>();
        attackArea = GetComponent<AttackAreaBase>();
    }

    private void Update()
    {
        bool isReadyToCast = CanCastNow() && Time.time >= nextCastTime;
        if (isReadyToCast)
        {
            TryCast();
        }
    }

    private bool CanCastNow()
    {
        return skill != null
            && ai != null
            && attackArea != null
            && !ai.IsDead
            && ai.currentState == UnitAI.UnitState.Attack
            && ai.HasActiveCombatTarget;
    }

    private void TryCast()
    {
        // Cổng kiểm tra dùng chung với đòn thường: phe địch, tầm nhìn, tầm đánh.
        // Thất bại thì KHÔNG đốt cooldown - thử lại frame sau.
        if (!attackArea.TryGetDamageTarget(ai, ai.TargetEnemy, out UnitHealth targetHealth))
        {
            return;
        }

        float premultipliedDamage = skill.ComputeDamage(magicPower) * damageOutputMultiplier;
        var context = new SkillCastContext(transform, ai.Faction, premultipliedDamage, targetHealth, this);
        SkillExecutor.Execute(skill, context);
        nextCastTime = Time.time + skill.Cooldown;
    }

    private void OnValidate()
    {
        magicPower = Mathf.Max(0f, magicPower);
    }
}
