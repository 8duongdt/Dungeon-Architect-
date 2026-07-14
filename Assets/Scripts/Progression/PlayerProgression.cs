using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kho tiến trình BỀN VỮNG của người chơi (điểm kỹ năng, BẬC skill đã nâng, ô đã trang bị,
/// checkpoint phase), lưu bằng PlayerPrefs nên sống qua mọi lần đổi scene lẫn khởi động lại game.
/// Class tĩnh thuần C# - không cần wire vào scene nào; Lobby/loader/phase gọi thẳng.
/// Danh tính skill là chuỗi <see cref="SkillDefinitionSO.SkillName"/> (vd "FireBall").
///
/// Mỗi skill có bậc 0..<see cref="MaxSkillLevel"/>: 0 = chưa mở, 1..3 = đã mở với hệ số nhân
/// sát thương <see cref="LevelMultipliers"/> (x1 / x1.5 / x2). "Mở khóa" = nâng lên bậc 1.
/// </summary>
public static class PlayerProgression
{
    private const string SkillPointsKey = "Progression.SkillPoints";
    private const string SkillLevelsKey = "Progression.SkillLevels";
    private const string BuildingPointsKey = "Progression.BuildingPoints";
    private const string BuildingLevelsKey = "Progression.BuildingLevels";
    private const string LegacyUnlockedSkillsKey = "Progression.UnlockedSkills";
    private const string EquippedSlotsKey = "Progression.EquippedSlots";
    private const string HasSeenIntroKey = "Progression.HasSeenIntro";
    private const string StarterPointGrantedKey = "Progression.StarterPointGranted";
    private const string CurrentPhaseKey = "Progression.CurrentPhase";

    private const char ListSeparator = ';';
    private const char LevelSeparator = ':';
    private const string DefaultSkillName = "FireBall";
    private const int StarterSkillPoints = 1;

    /// <summary>Bậc tối đa của một skill.</summary>
    public const int MaxSkillLevel = 3;

    // Hệ số nhân theo bậc (index = level - 1): bậc 1 = x1, bậc 2 = x1.5, bậc 3 = x2.
    private static readonly float[] LevelMultipliers = { 1f, 1.5f, 2f };

    /// <summary>Điểm kỹ năng đang có để mở/nâng node trong cây.</summary>
    public static int SkillPoints => PlayerPrefs.GetInt(SkillPointsKey, 0);

    /// <summary>Điểm công trình đang có để nâng node trong cây công trình.</summary>
    public static int BuildingPoints => PlayerPrefs.GetInt(BuildingPointsKey, 0);

    /// <summary>Đã xem StoryIntro chưa - MainMenu dùng để chỉ chiếu intro lần đầu.</summary>
    public static bool HasSeenIntro
    {
        get => PlayerPrefs.GetInt(HasSeenIntroKey, 0) != 0;
        set => SaveInt(HasSeenIntroKey, value ? 1 : 0);
    }

    /// <summary>
    /// Phase đang chơi dở / mốc checkpoint (0 = chưa có run, 1 = Phase 1, 2 = Phase 2).
    /// Continue ở MainMenu chỉ hiện khi <see cref="HasSave"/>.
    /// </summary>
    public static int CurrentPhase
    {
        get => PlayerPrefs.GetInt(CurrentPhaseKey, 0);
        set => SaveInt(CurrentPhaseKey, value);
    }

    /// <summary>Có run để tiếp tục hay không (đã bắt đầu ít nhất một phase).</summary>
    public static bool HasSave => CurrentPhase >= 1;

    public static void AddSkillPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SaveInt(SkillPointsKey, SkillPoints + amount);
    }

    /// <summary>Trừ điểm nếu đủ; không đủ thì giữ nguyên và trả false.</summary>
    public static bool TrySpendSkillPoints(int cost)
    {
        if (cost < 0 || SkillPoints < cost)
        {
            return false;
        }

        SaveInt(SkillPointsKey, SkillPoints - cost);
        return true;
    }

    public static void AddBuildingPoints(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SaveInt(BuildingPointsKey, BuildingPoints + amount);
    }

    /// <summary>Trừ điểm công trình nếu đủ; không đủ thì giữ nguyên và trả false.</summary>
    public static bool TrySpendBuildingPoints(int cost)
    {
        if (cost < 0 || BuildingPoints < cost)
        {
            return false;
        }

        SaveInt(BuildingPointsKey, BuildingPoints - cost);
        return true;
    }

    /// <summary>Bậc hiện tại của node công trình (0 = chưa nâng lần nào).</summary>
    public static int GetBuildingLevel(string upgradeId)
    {
        return ReadLevelMap(BuildingLevelsKey).TryGetValue(upgradeId, out int level) ? level : 0;
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

        Dictionary<string, int> levels = ReadLevelMap(BuildingLevelsKey);
        if (level <= 0)
        {
            levels.Remove(upgradeId);
        }
        else
        {
            levels[upgradeId] = level;
        }

        WriteLevelMap(BuildingLevelsKey, levels);
    }

    /// <summary>Bậc hiện tại của skill (0 = chưa mở).</summary>
    public static int GetSkillLevel(string skillName)
    {
        return ReadSkillLevels().TryGetValue(skillName, out int level) ? level : 0;
    }

    /// <summary>Gán bậc cho skill (kẹp 0..Max), lưu lại.</summary>
    public static void SetSkillLevel(string skillName, int level)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return;
        }

        Dictionary<string, int> levels = ReadSkillLevels();
        int clamped = Mathf.Clamp(level, 0, MaxSkillLevel);
        if (clamped <= 0)
        {
            levels.Remove(skillName);
        }
        else
        {
            levels[skillName] = clamped;
        }

        WriteSkillLevels(levels);
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
        string[] saved = PlayerPrefs.GetString(EquippedSlotsKey, string.Empty).Split(ListSeparator);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = i < saved.Length ? saved[i] : string.Empty;
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
        SaveString(EquippedSlotsKey, string.Join(ListSeparator.ToString(), slots));
    }

    /// <summary>
    /// Lần chơi đầu tiên: tặng 1 điểm kỹ năng (đúng một lần), mở sẵn FireBall và trang bị vào ô 1
    /// để người chơi luôn có skill. Gọi từ Lobby VÀ từ loader trong Phase 2 - chạy thẳng Phase 2 vẫn chạy.
    /// </summary>
    public static void EnsureDefaults()
    {
        if (PlayerPrefs.GetInt(StarterPointGrantedKey, 0) == 0)
        {
            AddSkillPoints(StarterSkillPoints);
            SaveInt(StarterPointGrantedKey, 1);
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
    /// cờ đã xem intro, checkpoint phase) - dùng cho "New Game" thật sự, khác với chỉ đổi
    /// <see cref="CurrentPhase"/> (vốn giữ nguyên điểm/bậc để chơi tiếp kiểu roguelite).
    /// </summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(SkillPointsKey);
        PlayerPrefs.DeleteKey(SkillLevelsKey);
        PlayerPrefs.DeleteKey(BuildingPointsKey);
        PlayerPrefs.DeleteKey(BuildingLevelsKey);
        PlayerPrefs.DeleteKey(LegacyUnlockedSkillsKey);
        PlayerPrefs.DeleteKey(EquippedSlotsKey);
        PlayerPrefs.DeleteKey(HasSeenIntroKey);
        PlayerPrefs.DeleteKey(StarterPointGrantedKey);
        PlayerPrefs.DeleteKey(CurrentPhaseKey);
        PlayerPrefs.Save();
    }

    private static Dictionary<string, int> ReadSkillLevels()
    {
        Dictionary<string, int> levels = ReadLevelMap(SkillLevelsKey);
        foreach (string name in levels.Keys.ToArray())
        {
            levels[name] = Mathf.Clamp(levels[name], 0, MaxSkillLevel);
        }

        // Migration: bản cũ lưu unlock nhị phân -> coi mỗi skill đã mở là bậc 1.
        if (levels.Count == 0)
        {
            string legacy = PlayerPrefs.GetString(LegacyUnlockedSkillsKey, string.Empty);
            foreach (string name in legacy.Split(new[] { ListSeparator }, StringSplitOptions.RemoveEmptyEntries))
            {
                levels[name] = 1;
            }
        }

        return levels;
    }

    private static void WriteSkillLevels(Dictionary<string, int> levels)
    {
        WriteLevelMap(SkillLevelsKey, levels);
    }

    // Codec "Tên:Bậc;Tên:Bậc" dùng chung cho bậc skill lẫn bậc công trình.
    private static Dictionary<string, int> ReadLevelMap(string prefKey)
    {
        var levels = new Dictionary<string, int>();
        string saved = PlayerPrefs.GetString(prefKey, string.Empty);
        foreach (string entry in saved.Split(new[] { ListSeparator }, StringSplitOptions.RemoveEmptyEntries))
        {
            int split = entry.IndexOf(LevelSeparator);
            if (split <= 0)
            {
                continue;
            }

            string name = entry.Substring(0, split);
            if (int.TryParse(entry.Substring(split + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
            {
                levels[name] = Mathf.Max(0, level);
            }
        }

        return levels;
    }

    private static void WriteLevelMap(string prefKey, Dictionary<string, int> levels)
    {
        string joined = string.Join(ListSeparator.ToString(),
            levels.Select(pair => $"{pair.Key}{LevelSeparator}{pair.Value}"));
        SaveString(prefKey, joined);
    }

    private static void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    private static void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }
}
