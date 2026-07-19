/// <summary>
/// Hợp đồng cho component có thể bị hệ thống hiệu ứng scale sát thương gây ra - cùng vai trò với
/// <see cref="ISpeedModifiable"/> nhưng cho sát thương đòn đánh. <see cref="AttackState"/> hiện
/// thực, gom bởi <see cref="UnitEffectModifier"/> giống hệt cách ISpeedModifiable được gom.
/// </summary>
public interface IDamageOutputModifiable
{
    // Hệ số nhân sát thương gây ra hiện tại (1 = sát thương gốc, 0.5 = còn một nửa).
    float DamageOutputMultiplier { get; set; }
}
