using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Đọc/ghi các file save JSON của người chơi tại <see cref="Application.persistentDataPath"/>.
/// Hỗ trợ nhiều slot (save_1.json .. save_<see cref="SlotCount"/>.json) để người chơi có nhiều
/// tuyến chơi song song. Là lớp truy cập file thuần: <see cref="PlayerProgression"/> mới là nơi
/// giữ logic tiến trình và gọi vào đây. Mọi lỗi I/O được nuốt an toàn để không làm sập game.
/// </summary>
public static class SaveSystem
{
    /// <summary>Số slot save tối đa hiển thị trên bảng chọn.</summary>
    public const int SlotCount = 6;

    private static string FilePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
    }

    /// <summary>Slot có nằm trong 1..<see cref="SlotCount"/> hay không.</summary>
    public static bool IsValidSlot(int slot)
    {
        return slot >= 1 && slot <= SlotCount;
    }

    /// <summary>Có tồn tại file save cho slot này hay không.</summary>
    public static bool Exists(int slot)
    {
        return IsValidSlot(slot) && File.Exists(FilePath(slot));
    }

    /// <summary>Đọc save của slot; không có file hoặc lỗi đọc thì trả về <see cref="SaveData"/> mặc định.</summary>
    public static SaveData Load(int slot)
    {
        if (!Exists(slot))
        {
            return new SaveData();
        }

        try
        {
            string json = File.ReadAllText(FilePath(slot));
            return JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SaveSystem] Không đọc được save slot {slot}, tạo mới: {exception.Message}");
            return new SaveData();
        }
    }

    /// <summary>Ghi save của slot ra file (định dạng dễ đọc để tiện kiểm tra thủ công).</summary>
    public static void Save(int slot, SaveData data)
    {
        if (!IsValidSlot(slot))
        {
            Debug.LogError($"[SaveSystem] Slot không hợp lệ khi ghi: {slot}");
            return;
        }

        try
        {
            File.WriteAllText(FilePath(slot), JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveSystem] Không ghi được save slot {slot}: {exception.Message}");
        }
    }

    /// <summary>Xóa file save của slot (dùng cho New Game/xóa slot).</summary>
    public static void Delete(int slot)
    {
        if (!Exists(slot))
        {
            return;
        }

        try
        {
            File.Delete(FilePath(slot));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SaveSystem] Không xóa được save slot {slot}: {exception.Message}");
        }
    }
}
