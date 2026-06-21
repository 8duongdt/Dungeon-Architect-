/// <summary>
/// Hợp đồng cho mọi component điều khiển di chuyển có thể bị hệ thống hiệu ứng
/// scale tốc độ (làm chậm / tăng tốc). Component chỉ phơi ra một hệ số nhân và
/// tự nhân vào tốc độ gốc khi di chuyển - KHÔNG biết gì về logic hiệu ứng cụ thể.
/// Nhờ vậy hệ thống hiệu ứng (UnitEffectModifier) tách rời hoàn toàn khỏi code di chuyển.
/// </summary>
public interface ISpeedModifiable
{
    // Hệ số nhân tốc độ di chuyển hiện tại (1 = tốc độ gốc, 0.5 = còn một nửa).
    float SpeedMultiplier { get; set; }
}
