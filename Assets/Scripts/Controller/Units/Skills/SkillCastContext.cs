using UnityEngine;

/// <summary>
/// Ngữ cảnh thi triển kỹ năng — KHÔNG phụ thuộc UnitAI, để mọi loại caster
/// (unit AI hiện tại, PlayerSkillCaster ở giai đoạn sau) đều dựng được và
/// tái dùng chung <see cref="SkillExecutor"/>.
/// </summary>
public readonly struct SkillCastContext
{
    /// <summary>Vị trí xuất phát của đạn/hiệu ứng.</summary>
    public readonly Transform CasterTransform;

    /// <summary>Phe của người thi triển - cổng CanAttack cho projectile/chain.</summary>
    public readonly UnitFaction CasterFaction;

    /// <summary>Sát thương TRƯỚC giáp, ĐÃ nhân mọi hệ số (executor không biết stat model).</summary>
    public readonly float Damage;

    /// <summary>Mục tiêu chính, đã qua cổng phe/tầm nhìn/tầm đánh của caster.</summary>
    public readonly UnitHealth PrimaryTarget;

    /// <summary>Nơi chạy coroutine impactDelay; null hoặc đã hủy thì gây sát thương ngay.</summary>
    public readonly MonoBehaviour CoroutineHost;

    public SkillCastContext(Transform casterTransform, UnitFaction casterFaction,
        float damage, UnitHealth primaryTarget, MonoBehaviour coroutineHost)
    {
        CasterTransform = casterTransform;
        CasterFaction = casterFaction;
        Damage = damage;
        PrimaryTarget = primaryTarget;
        CoroutineHost = coroutineHost;
    }
}
