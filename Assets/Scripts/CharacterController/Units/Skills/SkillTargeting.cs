using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiện ích tìm mục tiêu dùng chung cho các cơ chế skill: quét kẻ địch còn sống,
/// thù địch với phe người thi triển trong một bán kính (dedupe vì unit có nhiều collider).
/// </summary>
public static class SkillTargeting
{
    public static HashSet<UnitHealth> FindAttackableInRadius(Vector3 center, float radius, UnitFaction casterFaction)
    {
        var targets = new HashSet<UnitHealth>();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(center, radius))
        {
            UnitHealth candidate = collider.GetComponentInParent<UnitHealth>();
            if (IsAttackable(candidate, casterFaction))
            {
                targets.Add(candidate);
            }
        }

        return targets;
    }

    public static bool IsAttackable(UnitHealth candidate, UnitFaction casterFaction)
    {
        if (candidate == null || candidate.IsDead)
        {
            return false;
        }

        UnitFaction targetFaction = candidate.GetComponentInParent<UnitFaction>();
        return casterFaction != null && casterFaction.CanAttack(targetFaction);
    }
}
