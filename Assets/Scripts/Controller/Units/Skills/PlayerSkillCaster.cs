using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hệ kỹ năng của NGƯỜI CHƠI: nhấn phím 1-4 để thi triển skill ở ô tương ứng.
/// Có kẻ địch gần con trỏ chuột thì ưu tiên khóa mục tiêu; không có thì các skill
/// dạng điểm rơi (projectile/nổ/vùng/bẫy/xoáy) vẫn cast tại điểm chuột (kẹp vào tầm),
/// chỉ các skill bắt buộc mục tiêu (chain/kết liễu/đánh thẳng) mới từ chối cast.
/// Tiêu tốn Mana toàn cục (ResourceManager); không đủ Mana / đang hồi chiêu thì
/// không cast và KHÔNG mất Mana. Tái dùng SkillExecutor chung với unit AI.
/// </summary>
[DisallowMultipleComponent]
public class PlayerSkillCaster : MonoBehaviour
{
    public const int SlotCount = 4;

    [Tooltip("Skill gán cho phím 1-4 (phần tử trống = phím đó không làm gì).")]
    [SerializeField] private SkillDefinitionSO[] skillSlots = new SkillDefinitionSO[SlotCount];

    [Tooltip("Sức mạnh phép của người chơi - nhân với statScaling của skill.")]
    [SerializeField] private float magicPower = 10f;

    [Tooltip("Tầm thi triển tối đa tính từ nhân vật.")]
    [SerializeField] private float castRange = 8f;

    [Tooltip("Bán kính quanh con trỏ chuột để bắt mục tiêu.")]
    [SerializeField] private float targetSearchRadius = 1.5f;

    private UnitFaction faction;
    private UnitHealth health;
    private readonly float[] nextCastTimes = new float[SlotCount];

    private void Awake()
    {
        faction = GetComponent<UnitFaction>();
        health = GetComponent<UnitHealth>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            return;
        }

        int pressedSlot = ReadPressedSlot();
        if (pressedSlot >= 0)
        {
            CastSlot(pressedSlot);
        }
    }

    /// <summary>
    /// Thi triển skill ở ô slotIndex: ưu tiên kẻ địch gần chuột, không có thì cast
    /// tại điểm chuột (trừ skill bắt buộc mục tiêu). Public để UI/hệ khác gọi thẳng.
    /// </summary>
    public bool CastSlot(int slotIndex)
    {
        SkillDefinitionSO skill = GetSlotSkill(slotIndex);
        bool isReady = skill != null && Time.time >= nextCastTimes[slotIndex];
        if (!isReady)
        {
            return false;
        }

        if (!TryResolveAim(skill, out UnitHealth target, out Vector3 aimPoint) || !TrySpendMana(skill.ManaCost))
        {
            return false;
        }

        var context = new SkillCastContext(
            transform, faction, skill.ComputeDamage(magicPower), target, aimPoint, this);
        SkillExecutor.Execute(skill, context);
        nextCastTimes[slotIndex] = Time.time + skill.Cooldown;
        return true;
    }

    /// <summary>Gán skill cho ô lúc runtime (loader trang bị / cây kỹ năng) - reset hồi chiêu ô đó.</summary>
    public void SetSlotSkill(int slotIndex, SkillDefinitionSO skill)
    {
        bool isValidSlot = slotIndex >= 0 && slotIndex < skillSlots.Length;
        if (!isValidSlot)
        {
            return;
        }

        skillSlots[slotIndex] = skill;
        nextCastTimes[slotIndex] = 0f;
    }

    /// <summary>
    /// Xác định mục tiêu + điểm rơi của skill. Trả false = không cast được
    /// (skill bắt buộc mục tiêu mà không có địch gần chuột, hoặc không đọc được chuột/camera).
    /// </summary>
    private bool TryResolveAim(SkillDefinitionSO skill, out UnitHealth target, out Vector3 aimPoint)
    {
        if (skill.Mechanic == SkillMechanic.SelfShield)
        {
            target = health;
            aimPoint = transform.position;
            return true;
        }

        target = null;
        aimPoint = default;
        if (!TryGetMouseWorldPoint(out Vector3 mouseWorld))
        {
            return false;
        }

        target = FindTargetNearMouse(mouseWorld);
        if (target == null && RequiresUnitTarget(skill.Mechanic))
        {
            return false;
        }

        aimPoint = target != null ? target.transform.position : ClampToCastRange(mouseWorld);
        return true;
    }

    /// <summary>Các cơ chế vô nghĩa trên đất trống (nảy chuỗi/ngưỡng kết liễu/đánh thẳng mục tiêu).</summary>
    private static bool RequiresUnitTarget(SkillMechanic mechanic)
    {
        return mechanic == SkillMechanic.InstantStrike
            || mechanic == SkillMechanic.ChainStrike
            || mechanic == SkillMechanic.ExecuteStrike;
    }

    /// <summary>Kẹp điểm chuột vào tầm thi triển thay vì từ chối - cast không bao giờ câm lặng.</summary>
    private Vector3 ClampToCastRange(Vector3 mouseWorld)
    {
        Vector2 toMouse = mouseWorld - transform.position;
        Vector2 clamped = Vector2.ClampMagnitude(toMouse, castRange);
        return transform.position + (Vector3)clamped;
    }

    /// <summary>Skill đang gán ở ô - HUD đọc để hiển thị icon/mana (null = ô trống).</summary>
    public SkillDefinitionSO GetSlotSkill(int slotIndex)
    {
        bool isValidSlot = slotIndex >= 0 && slotIndex < skillSlots.Length;
        return isValidSlot ? skillSlots[slotIndex] : null;
    }

    /// <summary>Tỉ lệ hồi chiêu còn lại của ô (1 = vừa cast, 0 = sẵn sàng) - HUD vẽ lớp phủ quét tròn.</summary>
    public float CooldownFraction(int slotIndex)
    {
        SkillDefinitionSO skill = GetSlotSkill(slotIndex);
        if (skill == null || skill.Cooldown <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01((nextCastTimes[slotIndex] - Time.time) / skill.Cooldown);
    }

    private static int ReadPressedSlot()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return -1;
        }

        if (keyboard.digit1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame) return 3;
        return -1;
    }

    /// <summary>Điểm chuột trong tọa độ thế giới (z = 0). Trả false khi thiếu chuột/camera.</summary>
    private static bool TryGetMouseWorldPoint(out Vector3 mouseWorld)
    {
        mouseWorld = default;
        if (Mouse.current == null || Camera.main == null)
        {
            return false;
        }

        mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;
        return true;
    }

    /// <summary>Kẻ địch gần con trỏ chuột nhất, trong bán kính bắt mục tiêu và trong tầm thi triển.</summary>
    private UnitHealth FindTargetNearMouse(Vector3 mouseWorld)
    {
        UnitHealth closestTarget = null;
        float closestSqrDistance = float.MaxValue;
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(mouseWorld, targetSearchRadius))
        {
            UnitHealth candidate = collider.GetComponentInParent<UnitHealth>();
            if (!IsCastableTarget(candidate))
            {
                continue;
            }

            float sqrDistance = ((Vector2)candidate.transform.position - (Vector2)mouseWorld).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTarget = candidate;
            }
        }

        return closestTarget;
    }

    private bool IsCastableTarget(UnitHealth candidate)
    {
        if (candidate == null || candidate.IsDead)
        {
            return false;
        }

        bool isInCastRange = Vector2.Distance(transform.position, candidate.transform.position) <= castRange;
        UnitFaction targetFaction = candidate.GetComponentInParent<UnitFaction>();
        return isInCastRange && faction != null && faction.CanAttack(targetFaction);
    }

    private static bool TrySpendMana(int manaCost)
    {
        if (manaCost <= 0)
        {
            return true;
        }

        // Scene không có hệ kinh tế (vd Phase 2 chưa đặt ResourceManager) -> cast miễn phí,
        // tránh khóa cứng toàn bộ skill chỉ vì thiếu manager.
        ResourceManager resources = ResourceManager.Instance;
        return resources == null || resources.TrySpendMana(manaCost);
    }

    private void OnValidate()
    {
        magicPower = Mathf.Max(0f, magicPower);
        castRange = Mathf.Max(0f, castRange);
        targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
    }
}
