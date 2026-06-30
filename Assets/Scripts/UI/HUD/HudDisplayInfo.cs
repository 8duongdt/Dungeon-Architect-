using UnityEngine;

/// <summary>
/// Cung cấp thông tin hiển thị thống nhất cho bảng đơn vị trên HUD: tên, chân dung, máu, sát thương.
/// Gắn lên prefab lính và lên nhân vật chính (Commander) để bảng đọc dữ liệu giống nhau, không cần
/// biết đó là <see cref="Unit"/> hay <see cref="PlayerControll"/>.
/// </summary>
public class HudDisplayInfo : MonoBehaviour
{
    [SerializeField] private string displayName = "UNIT";
    [SerializeField] private Sprite portrait;

    [Tooltip("Đặt > 0 để ghi đè chỉ số sát thương hiển thị (vd cho Commander không có AttackState).")]
    [SerializeField] private float attackOverride = -1f;

    private UnitHealth health;
    private AttackState attackState;

    public string DisplayName => displayName;
    public Sprite Portrait => portrait;

    private void Awake()
    {
        health = GetComponent<UnitHealth>();
        attackState = GetComponent<AttackState>();
    }

    public bool HasHealth => ResolveHealth() != null;
    public float CurrentHealth => ResolveHealth() != null ? ResolveHealth().CurrentHealth : 0f;
    public float MaxHealth => ResolveHealth() != null ? ResolveHealth().MaxHealth : 0f;

    public float AttackValue
    {
        get
        {
            if (attackOverride >= 0f)
            {
                return attackOverride;
            }

            AttackState resolved = ResolveAttackState();
            return resolved != null ? resolved.AttackDamage : 0f;
        }
    }

    // Awake có thể chưa chạy khi bảng đọc sớm -> phân giải lười, an toàn.
    private UnitHealth ResolveHealth()
    {
        if (health == null)
        {
            health = GetComponent<UnitHealth>();
        }
        return health;
    }

    private AttackState ResolveAttackState()
    {
        if (attackState == null)
        {
            attackState = GetComponent<AttackState>();
        }
        return attackState;
    }
}
