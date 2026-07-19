using System;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu save CƠ BẢN của người chơi, được ghi ra file JSON (xem <see cref="SaveSystem"/>).
/// Chỉ chứa tiến trình bền vững: checkpoint phase, điểm skill/công trình, bậc đã nâng,
/// và ô trang bị. Không giữ trạng thái runtime (máu/vàng/mana trong màn).
///
/// Dùng field public để <see cref="UnityEngine.JsonUtility"/> serialize được. JsonUtility không
/// hỗ trợ Dictionary nên bậc skill/công trình lưu dưới dạng danh sách cặp tên-bậc
/// (<see cref="LevelEntry"/>).
/// </summary>
[Serializable]
public class SaveData
{
    public int currentPhase;
    public int skillPoints;
    public int buildingPoints;
    public bool starterPointGranted;
    public List<LevelEntry> skillLevels = new List<LevelEntry>();
    public List<LevelEntry> buildingLevels = new List<LevelEntry>();
    public string[] equippedSlots = new string[0];

    // Thời điểm thực (giờ hệ thống) của lần ghi gần nhất - dùng hiển thị trên bảng chọn slot.
    // Rỗng = slot chưa từng được ghi.
    public string lastSaved = string.Empty;
}

/// <summary>Một cặp "tên node -> bậc" dùng cho cả cây skill lẫn cây công trình.</summary>
[Serializable]
public class LevelEntry
{
    public string name;
    public int level;

    public LevelEntry() { }

    public LevelEntry(string name, int level)
    {
        this.name = name;
        this.level = level;
    }
}
