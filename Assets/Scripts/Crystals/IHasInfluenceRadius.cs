using UnityEngine;

/// <summary>
/// Hợp đồng cho mọi object có một vùng ảnh hưởng hình tròn cần hiển thị khi chuột phải vào
/// (tinh thể tài nguyên, pha lê đen của người chơi). <see cref="RadiusIndicatorDisplay"/> chỉ cần
/// biết giao diện này, không cần biết object cụ thể là gì.
/// </summary>
public interface IHasInfluenceRadius
{
    float Radius { get; }
    Color RadiusColor { get; }
    Transform Origin { get; }
}
