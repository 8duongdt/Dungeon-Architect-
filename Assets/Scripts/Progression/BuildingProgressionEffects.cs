using UnityEngine;

/// <summary>
/// Cửa ngõ tĩnh để gameplay đọc hiệu ứng cây công trình đã mua (bậc lưu ở PlayerProgression):
/// ResourceProducer hỏi hệ số sản lượng/chu kỳ, Phase2BuffApplier hỏi buff Phase 2.
/// Tự Resources.Load cây công trình một lần; thiếu asset thì cảnh báo một lần và
/// trả giá trị trung tính (hệ số 1, lượng 0) để game không gãy.
/// </summary>
public static class BuildingProgressionEffects
{
    private const string TreeResourcePath = "BuildingUpgradeTree";

    // Quy tắc cân bằng: giảm thời gian mạnh hơn tăng sản lượng rất nhiều,
    // nên tổng mức giảm chu kỳ bị cap cứng dù cây có mở rộng thêm bậc sau này.
    public const float MaxCycleTimeReduction = 0.5f;

    // Buff lãnh thổ Obelisk: 20% cố định + tối đa 20% cộng thêm từ nâng cấp, trần cứng 40%.
    public const float ObeliskBaseBonus = 0.2f;
    public const float ObeliskMaxBonus = 0.4f;

    private static BuildingUpgradeTreeSO cachedTree;
    private static bool hasWarnedMissingTree;

    /// <summary>Hệ số nhân sản lượng Vàng/Mana mỗi chu kỳ (chưa nâng = 1).</summary>
    public static float GetProductionMultiplier(ResourceProducer.ResourceKind kind)
    {
        BuildingUpgradeEffect effect = kind == ResourceProducer.ResourceKind.Gold
            ? BuildingUpgradeEffect.GoldOutput
            : BuildingUpgradeEffect.ManaOutput;
        return 1f + GetTotalEffectValue(effect);
    }

    /// <summary>Hệ số nhân thời gian chu kỳ sản xuất (0.7 = nhanh hơn 30%), không xuống dưới cap 50%.</summary>
    public static float GetCycleTimeMultiplier()
    {
        float reduction = GetTotalEffectValue(BuildingUpgradeEffect.CycleTimeReduction);
        return Mathf.Max(1f - reduction, 1f - MaxCycleTimeReduction);
    }

    /// <summary>Hệ số nhân sát thương skill người chơi trong Phase 2 (chưa nâng = 1).</summary>
    public static float GetPhase2DamageMultiplier()
    {
        return 1f + GetTotalEffectValue(BuildingUpgradeEffect.Phase2Damage);
    }

    /// <summary>Lượng khiên HP cấp sẵn khi vào Phase 2 (chưa nâng = 0).</summary>
    public static float GetPhase2ShieldAmount()
    {
        return GetTotalEffectValue(BuildingUpgradeEffect.Phase2Shield);
    }

    /// <summary>HP hồi mỗi giây trong Phase 2 (chưa nâng = 0).</summary>
    public static float GetPhase2RegenPerSecond()
    {
        return GetTotalEffectValue(BuildingUpgradeEffect.Phase2Regen);
    }

    /// <summary>Tổng % buff lãnh thổ Obelisk (20% gốc + bậc đã mua), không vượt trần 40%.</summary>
    public static float GetObeliskBonusFraction()
    {
        float bonus = ObeliskBaseBonus + GetTotalEffectValue(BuildingUpgradeEffect.ObeliskAuraBonus);
        return Mathf.Min(bonus, ObeliskMaxBonus);
    }

    /// <summary>Hệ số nhân sẵn dùng cho UnitEffectModifier.ApplyBuff: (dame, máu tối đa, tốc-đánh, tốc-chạy).</summary>
    public static (float damage, float maxHealth, float attackCooldown, float moveSpeed) GetObeliskMultipliers()
    {
        float bonus = GetObeliskBonusFraction();
        return (1f + bonus, 1f + bonus, 1f - bonus, 1f + bonus);
    }

    // Lợi ích tuyến tính: bậc đã mua × giá trị mỗi bậc của node mang hiệu ứng đó.
    private static float GetTotalEffectValue(BuildingUpgradeEffect effect)
    {
        BuildingUpgradeTreeSO.BuildingUpgradeNode node = ResolveTree()?.FindNodeByEffect(effect);
        if (node == null)
        {
            return 0f;
        }

        return PlayerProgression.GetBuildingLevel(node.id) * node.valuePerLevel;
    }

    private static BuildingUpgradeTreeSO ResolveTree()
    {
        if (cachedTree != null)
        {
            return cachedTree;
        }

        cachedTree = Resources.Load<BuildingUpgradeTreeSO>(TreeResourcePath);
        if (cachedTree == null && !hasWarnedMissingTree)
        {
            hasWarnedMissingTree = true;
            Debug.LogWarning(
                "[BuildingProgressionEffects] Thiếu Assets/Resources/BuildingUpgradeTree.asset "
                + "- chạy Tools > Dungeon > Setup Building Tree Asset. Tạm coi như chưa nâng cấp gì.");
        }

        return cachedTree;
    }
}
