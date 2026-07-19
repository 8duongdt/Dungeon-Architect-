using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kho tiến trình BỀN VỮNG của người chơi (điểm kỹ năng, BẬC skill đã nâng, ô đã trang bị,
/// checkpoint phase), lưu bằng file JSON (<see cref="SaveSystem"/>) nên sống qua mọi lần đổi scene
/// lẫn khởi động lại game. Class tĩnh thuần C# - không cần wire vào scene nào; Lobby/loader/phase
/// gọi thẳng. Danh tính skill là chuỗi <see cref="SkillDefinitionSO.SkillName"/> (vd "FireBall").
///
/// Toàn bộ đọc/ghi đi qua SLOT đang chọn (<see cref="ActiveSlot"/>): bảng chọn slot ở MainMenu gọi
/// <see cref="SelectSlot"/> (nạp slot cũ) hoặc <see cref="CreateNewSlot"/> (tạo mới) trước khi vào
/// game; mọi setter sau đó tự ghi xuống đúng file của slot đó.
///
/// Mỗi skill có bậc 0..<see cref="MaxSkillLevel"/>: 0 = chưa mở, 1..3 = đã mở với hệ số nhân
/// sát thương <see cref="LevelMultipliers"/> (x1 / x1.5 / x2). "Mở khóa" = nâng lên bậc 1.
/// </summary>
public static class PlayerProgression
{
    private const string DefaultSkillName = "FireBall";
    private const int StarterSkillPoints = 1;
    private const string TimestampFormat = "yyyy-MM-dd HH:mm";

    /// <summary>Bậc tối đa của một skill.</summary>
    public const int MaxSkillLevel = 3;

    // Hệ số nhân theo bậc (index = level - 1): bậc 1 = x1, bậc 2 = x1.5, bậc 3 = x2.
    private static readonly float[] LevelMultipliers = { 1f, 1.5f, 2f };

    /// <summary>Slot đang chơi. Mặc định 1 để chạy thẳng scene phase khi test trong editor vẫn có chỗ ghi.</summary>
    public static int ActiveSlot { get; private set; } = 1;

    // Save của slot đang chọn: nạp từ file một lần rồi giữ trong bộ nhớ; mỗi lần đổi giá trị lại ghi xuống file.
    private static SaveData cachedData;
    private static SaveData Data => cachedData ?? (cachedData = SaveSystem.Load(ActiveSlot));

    /// <summary>Chọn slot đã có để chơi tiếp: đặt slot hiện hành rồi nạp save của nó vào bộ nhớ.</summary>
    public static void SelectSlot(int slot)
    {
        if (!SaveSystem.IsValidSlot(slot))
        {
            return;
        }

        ActiveSlot = slot;
        cachedData = SaveSystem.Load(slot);
    }

    /// <summary>
    /// Tạo run mới trên một slot (dùng khi bấm vào ô trống): đặt slot hiện hành, làm mới save,
    /// gán mặc định lần đầu (tặng điểm + mở FireBall) rồi ghi ngay ra file cho slot đó.
    /// </summary>
    public static void CreateNewSlot(int slot)
    {
        if (!SaveSystem.IsValidSlot(slot))
        {
            return;
        }

        ActiveSlot = slot;
        cachedData = new SaveData();
        EnsureDefaults();
        Persist();
    }

    private static void Persist()
    {
        Data.lastSaved = DateTime.Now.ToString(TimestampFormat);
        SaveSystem.Save(ActiveSlot, Data);
    }

    /// <summary>Điểm kỹ năng đang có để mở/nâng node trong cây.</summary>
    public static int SkillPoints => Data.skillPoints;

    /// <summary>Điểm công trình đang có để nâng node trong cây công trình.</summary>
    public static int BuildingPoints => Data.buildingPoints;

    /// <summary>
    /// Phase đang chơi dở / mốc checkpoint (0 = chưa có run, 1 = Phase 1, 2 = Phase 2).
    /// Continue ở MainMenu chỉ hiện khi <see cref="HasSave"/>.
    /// </summary>
    public static int CurrentPhase
    {
        get => Data.currentPhase;
        set
        {
            Data.currentPhase = value;
            Persist();
        }
    }

    /// <summary>Có run để tiếp tục hay không (đã bắt đầu ít nhất một phase).</summary>
    public static bool HasSave => Data.currentPhase >= 1;

    public static void AddSkillPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Data.skillPoints += amount;
        Persist();
    }

    /// <summary>Trừ điểm nếu đủ; không đủ thì giữ nguyên và trả false.</summary>
    public static bool TrySpendSkillPoints(int cost)
    {
        if (cost < 0 || Data.skillPoints < cost)
        {
            return false;
        }

        Data.skillPoints -= cost;
        Persist();
        return true;
    }

    public static void AddBuildingPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Data.buildingPoints += amount;
        Persist();
    }

    /// <summary>Trừ điểm công trình nếu đủ; không đủ thì giữ nguyên và trả false.</summary>
    public static bool TrySpendBuildingPoints(int cost)
    {
        if (cost < 0 || Data.buildingPoints < cost)
        {
            return false;
        }

        Data.buildingPoints -= cost;
        Persist();
        return true;
    }

    /// <summary>Bậc hiện tại của node công trình (0 = chưa nâng lần nào).</summary>
    public static int GetBuildingLevel(string upgradeId)
    {
        return GetLevel(Data.buildingLevels, upgradeId);
    }

    /// <summary>
    /// Gán bậc cho node công trình rồi lưu lại. Bậc tối đa do caller kiểm theo
    /// maxLevel của node trong BuildingUpgradeTreeSO (mỗi node một trần riêng).
    /// </summary>
    public static void SetBuildingLevel(string upgradeId, int level)
    {
        if (string.IsNullOrEmpty(upgradeId))
        {
            return;
        }

        SetLevel(Data.buildingLevels, upgradeId, Mathf.Max(0, level));
        Persist();
    }

    /// <summary>Bậc hiện tại của skill (0 = chưa mở).</summary>
    public static int GetSkillLevel(string skillName)
    {
        return Mathf.Clamp(GetLevel(Data.skillLevels, skillName), 0, MaxSkillLevel);
    }

    /// <summary>Tổng số BẬC kỹ năng đã đầu tư trên toàn cây (dùng suy ra cỡ map theo tiến độ).</summary>
    public static int GetTotalSkillLevels()
    {
        int total = 0;
        foreach (LevelEntry entry in Data.skillLevels)
        {
            total += Mathf.Clamp(entry.level, 0, MaxSkillLevel);
        }
        return total;
    }

    /// <summary>Gán bậc cho skill (kẹp 0..Max), lưu lại.</summary>
    public static void SetSkillLevel(string skillName, int level)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return;
        }

        SetLevel(Data.skillLevels, skillName, Mathf.Clamp(level, 0, MaxSkillLevel));
        Persist();
    }

    /// <summary>Hệ số nhân sát thương theo bậc (chưa mở = 1).</summary>
    public static float GetSkillMultiplier(string skillName)
    {
        return GetMultiplierForLevel(GetSkillLevel(skillName));
    }

    /// <summary>Hệ số nhân ứng với một bậc skill - UI dùng chung để nhãn không lệch với gameplay.</summary>
    public static float GetMultiplierForLevel(int level)
    {
        return level >= 1 ? LevelMultipliers[Mathf.Clamp(level - 1, 0, LevelMultipliers.Length - 1)] : 1f;
    }

    public static bool IsUnlocked(string skillName)
    {
        return GetSkillLevel(skillName) >= 1;
    }

    /// <summary>Mở khóa skill = nâng lên bậc 1 (không hạ bậc nếu đã cao hơn).</summary>
    public static void Unlock(string skillName)
    {
        if (!string.IsNullOrEmpty(skillName) && GetSkillLevel(skillName) < 1)
        {
            SetSkillLevel(skillName, 1);
        }
    }

    /// <summary>Tên skill gán vào 4 ô phím 1-4 (phần tử rỗng = ô trống).</summary>
    public static string[] GetEquippedSlots()
    {
        string[] slots = new string[PlayerSkillCaster.SlotCount];
        string[] saved = Data.equippedSlots ?? new string[0];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = i < saved.Length && saved[i] != null ? saved[i] : string.Empty;
        }

        return slots;
    }

    /// <summary>Gán skill vào ô (chuỗi rỗng = xóa ô).</summary>
    public static void SetEquippedSlot(int slotIndex, string skillName)
    {
        string[] slots = GetEquippedSlots();
        bool isValidSlot = slotIndex >= 0 && slotIndex < slots.Length;
        if (!isValidSlot)
        {
            return;
        }

        slots[slotIndex] = skillName ?? string.Empty;
        Data.equippedSlots = slots;
        Persist();
    }

    /// <summary>
    /// Lần chơi đầu tiên: tặng 1 điểm kỹ năng (đúng một lần), mở sẵn FireBall và trang bị vào ô 1
    /// để người chơi luôn có skill. Gọi từ Lobby VÀ từ loader trong Phase 2 - chạy thẳng Phase 2 vẫn chạy.
    /// </summary>
    public static void EnsureDefaults()
    {
        if (!Data.starterPointGranted)
        {
            Data.starterPointGranted = true;
            AddSkillPoints(StarterSkillPoints);
        }

        if (IsUnlocked(DefaultSkillName))
        {
            return;
        }

        Unlock(DefaultSkillName);
        bool hasNoEquippedSkill = GetEquippedSlots().All(string.IsNullOrEmpty);
        if (hasNoEquippedSkill)
        {
            SetEquippedSlot(0, DefaultSkillName);
        }
    }

    /// <summary>
    /// Xóa sạch toàn bộ tiến trình đã lưu (điểm skill/công trình, bậc đã mở/nâng, ô trang bị,
    /// checkpoint phase) - dùng cho "New Game" thật sự, khác với chỉ đổi
    /// <see cref="CurrentPhase"/> (vốn giữ nguyên điểm/bậc để chơi tiếp kiểu roguelite).
    /// </summary>
    public static void ResetAll()
    {
        cachedData = new SaveData();
        SaveSystem.Delete(ActiveSlot);
    }

    /// <summary>
    /// Xóa hẳn file save của MỘT slot bất kỳ (nút xóa ở bảng chọn slot). Nếu xóa đúng slot đang
    /// chọn thì làm sạch bộ nhớ đệm về save TRỐNG - lưu ý ActiveSlot vẫn giữ nguyên, nên nếu sau
    /// đó có setter tiến trình nào chạy (không xảy ra trong luồng MainMenu) nó sẽ Persist() lại
    /// một save trống chứ không tái tạo dữ liệu cũ. Luồng bấm-vào-ô-trống sau khi xóa đi qua
    /// <see cref="CreateNewSlot"/>/<see cref="SelectSlot"/> nên luôn đặt lại ActiveSlot đúng.
    /// </summary>
    public static void DeleteSlot(int slot)
    {
        if (!SaveSystem.IsValidSlot(slot))
        {
            return;
        }

        SaveSystem.Delete(slot);
        if (slot == ActiveSlot)
        {
            cachedData = new SaveData();
        }
    }

    private static int GetLevel(List<LevelEntry> entries, string name)
    {
        LevelEntry entry = entries.FirstOrDefault(e => e.name == name);
        return entry != null ? entry.level : 0;
    }

    // Đặt bậc trong danh sách: bậc <= 0 thì gỡ hẳn entry để save gọn.
    private static void SetLevel(List<LevelEntry> entries, string name, int level)
    {
        LevelEntry entry = entries.FirstOrDefault(e => e.name == name);
        if (level <= 0)
        {
            if (entry != null)
            {
                entries.Remove(entry);
            }

            return;
        }

        if (entry != null)
        {
            entry.level = level;
        }
        else
        {
            entries.Add(new LevelEntry(name, level));
        }
    }
}
