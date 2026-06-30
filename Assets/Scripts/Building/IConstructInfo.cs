using System.Collections.Generic;

/// <summary>
/// Mọi hành vi công trình (khai thác tài nguyên, huấn luyện lính...) hiện thực giao diện này để
/// Cửa sổ Trạng thái (<see cref="ConstructStatusPanelView"/>) hiển thị mà không cần biết loại cụ thể.
/// </summary>
public interface IConstructInfo
{
    /// <summary>Nhãn loại công trình hiển thị ở mục "Other Info" (vd "Resource Converter", "Barracks").</summary>
    string TypeLabel { get; }

    /// <summary>Các dòng chỉ số khai thác/sản xuất hiển thị ở phần Stats (vd "Gold Generation: 3.5 / min").</summary>
    IEnumerable<string> GetStatLines();
}
