using UnityEngine;

[DisallowMultipleComponent]
public class RangedAttackArea : AttackAreaBase
{
    private void Reset()
    {
        detectionRadius = 7f;
        // attackRange là khe hở mép tối đa để bắn (xem AttackAreaBase.IsInRange). Ở tầm xa như thế này
        // chênh lệch giữa đo mép và đo tâm không đáng kể nên giữ nguyên giá trị cũ.
        attackRange = 5f;
        loseTargetRadiusMultiplier = 1.25f;
    }
}