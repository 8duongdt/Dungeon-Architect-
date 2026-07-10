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

    /// <summary>Mục tiêu chính (có thể null khi cast tự do xuống đất) - đã qua cổng phe/tầm của caster.</summary>
    public readonly UnitHealth PrimaryTarget;

    /// <summary>Điểm rơi của skill - luôn hợp lệ kể cả khi PrimaryTarget null (cast tại điểm chuột).</summary>
    public readonly Vector3 TargetPoint;

    /// <summary>Nơi chạy coroutine impactDelay; null hoặc đã hủy thì gây sát thương ngay.</summary>
    public readonly MonoBehaviour CoroutineHost;

    /// <summary>
    /// Hệ số nhân theo bậc nâng cấp của skill (1 = không nâng). Sát thương đã được nhân sẵn vào
    /// <see cref="Damage"/>; giá trị này để các cơ chế phi sát thương (vd Shield) nhân chỉ số riêng.
    /// </summary>
    public readonly float UpgradeMultiplier;

    public SkillCastContext(Transform casterTransform, UnitFaction casterFaction,
        float damage, UnitHealth primaryTarget, MonoBehaviour coroutineHost)
        : this(casterTransform, casterFaction, damage, primaryTarget,
            ResolveTargetPoint(casterTransform, primaryTarget), coroutineHost)
    {
    }

    public SkillCastContext(Transform casterTransform, UnitFaction casterFaction,
        float damage, UnitHealth primaryTarget, Vector3 targetPoint, MonoBehaviour coroutineHost,
        float upgradeMultiplier = 1f)
    {
        CasterTransform = casterTransform;
        CasterFaction = casterFaction;
        Damage = damage;
        PrimaryTarget = primaryTarget;
        TargetPoint = targetPoint;
        CoroutineHost = coroutineHost;
        UpgradeMultiplier = upgradeMultiplier;
    }

    private static Vector3 ResolveTargetPoint(Transform casterTransform, UnitHealth primaryTarget)
    {
        return primaryTarget != null ? primaryTarget.transform.position : casterTransform.position;
    }
}
