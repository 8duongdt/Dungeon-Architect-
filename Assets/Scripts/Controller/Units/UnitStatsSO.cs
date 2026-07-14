using UnityEngine;

/// <summary>Hạng quái - dùng để tham chiếu độ khó khi cân bằng (Mob rẻ, Elite thử thách, Boss cao trào).</summary>
public enum UnitTier
{
    Mob,
    Elite,
    Boss,
}

/// <summary>
/// Bộ chỉ số cốt lõi dùng chung cho mọi unit (người chơi + quái). Mỗi loại/hạng một asset để cân bằng
/// tập trung thay vì chỉnh số rải trên từng prefab. <see cref="UnitStats"/> đọc asset này rồi đẩy giá
/// trị GỐC vào UnitHealth/AttackState/UnitMovement/Unit; hệ số nhân runtime (hiệu ứng) vẫn chồng lên.
/// </summary>
[CreateAssetMenu(menuName = "Units/Unit Stats", fileName = "UnitStats")]
public class UnitStatsSO : ScriptableObject
{
    [Tooltip("Tên hiển thị (tùy chọn) - để trống nếu không cần.")]
    [SerializeField] private string displayName;

    [Tooltip("Hạng để tham chiếu khi cân bằng độ khó.")]
    [SerializeField] private UnitTier tier = UnitTier.Mob;

    [Header("Chỉ số cốt lõi")]
    [Tooltip("HP - máu tối đa.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("ATK - sát thương mỗi đòn đánh thường.")]
    [SerializeField] private float attackDamage = 10f;

    [Tooltip("DEF - giáp, giảm sát thương theo % qua công thức DEF/(DEF+K).")]
    [SerializeField] private float defense = 0f;

    [Tooltip("SPD di chuyển - tốc độ đi lại.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("SPD đánh - thời gian chờ (giây) giữa hai đòn; càng nhỏ càng đánh nhanh.")]
    [SerializeField] private float attackCooldown = 1f;

    public string DisplayName => displayName;
    public UnitTier Tier => tier;
    public float MaxHealth => maxHealth;
    public float AttackDamage => attackDamage;
    public float Defense => defense;
    public float MoveSpeed => moveSpeed;
    public float AttackCooldown => attackCooldown;

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        attackDamage = Mathf.Max(0f, attackDamage);
        defense = Mathf.Max(0f, defense);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        attackCooldown = Mathf.Max(0f, attackCooldown);
    }
}
