/// <summary>
/// Hợp đồng cho component có thể bị hệ thống hiệu ứng scale máu tối đa - cùng vai trò với
/// <see cref="ISpeedModifiable"/> nhưng cho chỉ số máu. <see cref="UnitHealth"/> hiện thực, gom
/// bởi <see cref="UnitEffectModifier"/> giống hệt cách ISpeedModifiable được gom.
/// </summary>
public interface IMaxHealthModifiable
{
    // Hệ số nhân máu tối đa hiện tại (1 = máu gốc, 0.5 = còn một nửa máu tối đa).
    float MaxHealthMultiplier { get; set; }
}
