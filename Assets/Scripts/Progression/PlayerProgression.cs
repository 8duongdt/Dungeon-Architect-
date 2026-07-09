using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Kho tiến trình BỀN VỮNG của người chơi (điểm kỹ năng, skill đã mở, ô đã trang bị),
/// lưu bằng PlayerPrefs nên sống qua mọi lần đổi scene lẫn khởi động lại game.
/// Class tĩnh thuần C# - không cần wire vào scene nào; Lobby/loader/phase gọi thẳng.
/// Danh tính skill là chuỗi <see cref="SkillDefinitionSO.SkillName"/> (vd "FireBall").
/// </summary>
public static class PlayerProgression
{
    private const string SkillPointsKey = "Progression.SkillPoints";
    private const string UnlockedSkillsKey = "Progression.UnlockedSkills";
    private const string EquippedSlotsKey = "Progression.EquippedSlots";
    private const string HasSeenIntroKey = "Progression.HasSeenIntro";

    private const char ListSeparator = ';';
    private const string DefaultSkillName = "FireBall";

    /// <summary>Điểm kỹ năng đang có để mua node trong cây.</summary>
    public static int SkillPoints => PlayerPrefs.GetInt(SkillPointsKey, 0);

    /// <summary>Đã xem StoryIntro chưa - MainMenu dùng để chỉ chiếu intro lần đầu.</summary>
    public static bool HasSeenIntro
    {
        get => PlayerPrefs.GetInt(HasSeenIntroKey, 0) != 0;
        set => SaveInt(HasSeenIntroKey, value ? 1 : 0);
    }

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

    public static bool IsUnlocked(string skillName)
    {
        return ReadUnlockedSkills().Contains(skillName);
    }

    public static void Unlock(string skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return;
        }

        HashSet<string> unlocked = ReadUnlockedSkills();
        if (unlocked.Add(skillName))
        {
            SaveString(UnlockedSkillsKey, string.Join(ListSeparator.ToString(), unlocked));
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
    /// Lần chơi đầu tiên: mở sẵn FireBall và trang bị vào ô 1 để người chơi luôn có skill.
    /// Gọi từ Lobby VÀ từ loader trong Phase 2 - chạy thẳng Phase 2 vẫn hoạt động.
    /// </summary>
    public static void EnsureDefaults()
    {
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

    private static HashSet<string> ReadUnlockedSkills()
    {
        string saved = PlayerPrefs.GetString(UnlockedSkillsKey, string.Empty);
        return new HashSet<string>(
            saved.Split(new[] { ListSeparator }, StringSplitOptions.RemoveEmptyEntries));
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
