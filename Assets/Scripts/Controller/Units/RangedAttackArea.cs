using UnityEngine;

[DisallowMultipleComponent]
public class RangedAttackArea : AttackAreaBase
{
    private void Reset()
    {
        detectionRadius = 7f;
        attackRange = 5f;
        loseTargetRadiusMultiplier = 1.25f;
    }
}