using UnityEngine;

/// <summary>
/// Cầu nối giữa bộ chỉ số trung tâm (<see cref="UnitStatsSO"/>) và các component tiêu thụ chỉ số trên
/// cùng unit. Chạy sớm (DefaultExecutionOrder âm) để đẩy chỉ số GỐC vào trước khi UnitHealth.Awake
/// kẹp máu. Là NƠI DUY NHẤT giữ tham chiếu tới asset chỉ số trên prefab.
///
/// Không gán asset -> no-op, unit giữ nguyên số serialize trên prefab (tương thích ngược).
/// Component nào không tồn tại thì bỏ qua (vd avatar người chơi không có AttackState).
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class UnitStats : MonoBehaviour
{
    [SerializeField] private UnitStatsSO stats;

    public UnitStatsSO Stats => stats;

    private void Awake()
    {
        ApplyStats();
    }

    private void ApplyStats()
    {
        if (stats == null)
        {
            return;
        }

        var health = GetComponent<UnitHealth>();
        if (health != null)
        {
            health.ApplyBaseStats(stats.MaxHealth, stats.Defense);
        }

        var attackState = GetComponent<AttackState>();
        if (attackState != null)
        {
            attackState.ApplyCombatStats(stats.AttackDamage, stats.AttackCooldown);
        }

        // Cả hai đường di chuyển đều nhận SPD: Unit cho lệnh di chuyển tay, UnitMovement cho AI đuổi đánh.
        var unit = GetComponent<Unit>();
        if (unit != null)
        {
            unit.ApplySpeed(stats.MoveSpeed);
        }

        var movement = GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.ApplySpeed(stats.MoveSpeed);
        }
    }
}
