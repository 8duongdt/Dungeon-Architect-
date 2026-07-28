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
public class UnitSkillCaster : MonoBehaviour
{
    [Tooltip("Kỹ năng của unit này (asset trong Assets/Skills).")]
    [SerializeField] private SkillDefinitionSO skill;

    [Tooltip("Sức mạnh phép - chỉ số nhân với statScaling của kỹ năng.")]
    [SerializeField] private float magicPower = 10f;

    [Tooltip("Cho phép cast cả khi đang ĐUỔI mục tiêu (skill lao tới) thay vì chỉ khi đứng đánh.")]
    [SerializeField] private bool castWhileChasing = false;

    [Tooltip("Tầm cast tối đa khi đang đuổi (chỉ dùng khi bật cast lúc đuổi).")]
    [SerializeField] private float chaseCastRange = 5f;

    private UnitAI ai;
    private AttackAreaBase attackArea;
    private CharacterAnimationController animationController;
    private float nextCastTime;

    public float MagicPower => magicPower;

    private void Awake()
    {
        ai = GetComponent<UnitAI>();
        attackArea = GetComponent<AttackAreaBase>();
        animationController = GetComponent<CharacterAnimationController>();
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
        bool isInCastableState = ai != null
            && (ai.currentState == UnitAI.UnitState.Attack
                || (castWhileChasing && ai.currentState == UnitAI.UnitState.Chase));

        return skill != null
            && attackArea != null
            && isInCastableState
            && !ai.IsDead
            && ai.HasActiveCombatTarget;
    }

    private void TryCast()
    {
        // Thất bại thì KHÔNG đốt cooldown - thử lại frame sau.
        if (!TryResolveTarget(out UnitHealth targetHealth))
        {
            return;
        }

        float damage = skill.ComputeDamage(magicPower);
        var context = new SkillCastContext(transform, ai.Faction, damage, targetHealth, this);
        SkillExecutor.Execute(skill, context);
        animationController?.PlaySkill();
        nextCastTime = Time.time + skill.Cooldown;
    }

    // Đứng đánh: cổng kiểm tra dùng chung với đòn thường (phe địch, tầm nhìn, tầm đánh).
    // Đang đuổi (skill lao tới): chỉ cần thấy mục tiêu trong tầm cast lúc đuổi.
    private bool TryResolveTarget(out UnitHealth targetHealth)
    {
        if (ai.currentState == UnitAI.UnitState.Attack)
        {
            return attackArea.TryGetDamageTarget(ai, ai.TargetEnemy, out targetHealth);
        }

        Transform target = ai.TargetEnemy;
        targetHealth = target != null ? target.GetComponentInParent<UnitHealth>() : null;
        bool isCastableWhileChasing = targetHealth != null
            && !targetHealth.IsDead
            && attackArea.CanSee(target)
            && Vector2.Distance(transform.position, target.position) <= chaseCastRange;
        return isCastableWhileChasing;
    }

    private void OnValidate()
    {
        magicPower = Mathf.Max(0f, magicPower);
        chaseCastRange = Mathf.Max(0f, chaseCastRange);
    }
}
