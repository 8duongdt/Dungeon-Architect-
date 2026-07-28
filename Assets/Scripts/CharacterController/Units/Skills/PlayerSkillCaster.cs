using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hệ kỹ năng của NGƯỜI CHƠI: nhấn phím 1-4 để thi triển skill ở ô tương ứng.
/// Skill bắn ra theo HƯỚNG NHÌN của nhân vật (chuột lái hướng nhìn qua PlayerControll):
/// tự bắt kẻ địch gần nhất trong hình quạt trước mặt; không có địch thì các skill dạng
/// điểm rơi (projectile/nổ/vùng/bẫy/xoáy) rơi dọc hướng nhìn (xa gần theo chuột, kẹp vào
/// tầm), chỉ các skill bắt buộc mục tiêu (chain/kết liễu/đánh thẳng) mới từ chối cast.
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

    [Tooltip("Nửa góc hình quạt trước mặt nhân vật để tự bắt mục tiêu (độ).")]
    [SerializeField] private float aimConeHalfAngle = 40f;

    // Bộ đệm quét vật lý dùng lại giữa các lần cast - không cấp phát rác mỗi lần bấm skill.
    private static readonly List<Collider2D> overlapResults = new List<Collider2D>(64);
    private static readonly ContactFilter2D anyColliderFilter = CreateAnyColliderFilter();

    private PlayerControll playerControll;
    private UnitFaction faction;
    private UnitHealth health;
    private readonly float[] nextCastTimes = new float[SlotCount];

    /// <summary>Hệ số buff sát thương từ bên ngoài (vd cây công trình trong Phase 2) - mặc định 1.</summary>
    public float ExternalDamageMultiplier { get; set; } = 1f;

    private void Awake()
    {
        playerControll = GetComponent<PlayerControll>();
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
    /// Thi triển skill ở ô slotIndex theo hướng nhìn nhân vật: ưu tiên kẻ địch trong quạt
    /// trước mặt, không có thì cast dọc hướng nhìn (trừ skill bắt buộc mục tiêu).
    /// Public để UI/hệ khác gọi thẳng.
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

        // Bậc nâng cấp nhân sát thương; riêng Shield dùng hệ số này nhân lượng khiên + thời gian.
        float upgradeMultiplier = PlayerProgression.GetSkillMultiplier(skill.SkillName);
        float damage = skill.ComputeDamage(magicPower) * upgradeMultiplier * ExternalDamageMultiplier;
        var context = new SkillCastContext(
            transform, faction, damage, target, aimPoint, this, upgradeMultiplier);
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
    /// Xác định mục tiêu + điểm rơi của skill theo hướng nhìn nhân vật.
    /// Trả false = không cast được (skill bắt buộc mục tiêu mà không có địch trước mặt).
    /// </summary>
    private bool TryResolveAim(SkillDefinitionSO skill, out UnitHealth target, out Vector3 aimPoint)
    {
        if (skill.Mechanic == SkillMechanic.SelfShield)
        {
            target = health;
            aimPoint = transform.position;
            return true;
        }

        Vector2 facing = ResolveFacingDirection();
        target = FindTargetInFacingCone(facing);
        if (target == null && RequiresUnitTarget(skill.Mechanic))
        {
            aimPoint = default;
            return false;
        }

        aimPoint = target != null ? target.transform.position : ResolvePointDrop(facing);
        return true;
    }

    /// <summary>Các cơ chế vô nghĩa trên đất trống (nảy chuỗi/ngưỡng kết liễu/đánh thẳng mục tiêu).</summary>
    private static bool RequiresUnitTarget(SkillMechanic mechanic)
    {
        return mechanic == SkillMechanic.InstantStrike
            || mechanic == SkillMechanic.ChainStrike
            || mechanic == SkillMechanic.ExecuteStrike;
    }

    /// <summary>Hướng nhìn của nhân vật; thiếu PlayerControll thì suy từ nhân vật tới chuột.</summary>
    private Vector2 ResolveFacingDirection()
    {
        if (playerControll != null)
        {
            return playerControll.FacingDirection;
        }

        if (TryGetMouseWorldPoint(out Vector3 mouseWorld))
        {
            Vector2 toMouse = mouseWorld - transform.position;
            if (toMouse.sqrMagnitude > 0.0001f)
            {
                return toMouse.normalized;
            }
        }

        return Vector2.down;
    }

    /// <summary>
    /// Điểm rơi cho skill đặt xuống đất: luôn dọc hướng nhìn, khoảng cách lấy theo chuột
    /// (kẹp vào tầm) để người chơi vẫn chỉnh được đặt gần hay xa - cast không bao giờ câm lặng.
    /// </summary>
    private Vector3 ResolvePointDrop(Vector2 facing)
    {
        float dropDistance = castRange;
        if (TryGetMouseWorldPoint(out Vector3 mouseWorld))
        {
            dropDistance = Mathf.Min(Vector2.Distance(transform.position, mouseWorld), castRange);
        }

        return transform.position + (Vector3)(facing * dropDistance);
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

    /// <summary>Kẻ địch gần nhất nằm trong hình quạt trước mặt nhân vật (tâm quạt = hướng nhìn).</summary>
    private UnitHealth FindTargetInFacingCone(Vector2 facing)
    {
        UnitHealth closestTarget = null;
        float closestSqrDistance = float.MaxValue;
        int hitCount = Physics2D.OverlapCircle(transform.position, castRange, anyColliderFilter, overlapResults);
        for (int i = 0; i < hitCount; i++)
        {
            UnitHealth candidate = overlapResults[i].GetComponentInParent<UnitHealth>();
            if (candidate == closestTarget || !IsCastableTarget(candidate))
            {
                continue;
            }

            Vector2 toCandidate = candidate.transform.position - transform.position;
            bool isInsideAimCone = Vector2.Angle(facing, toCandidate) <= aimConeHalfAngle;
            bool isCloser = toCandidate.sqrMagnitude < closestSqrDistance;
            if (isInsideAimCone && isCloser)
            {
                closestSqrDistance = toCandidate.sqrMagnitude;
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

    private static ContactFilter2D CreateAnyColliderFilter()
    {
        var filter = new ContactFilter2D();
        filter.NoFilter();
        return filter;
    }

    private void OnValidate()
    {
        magicPower = Mathf.Max(0f, magicPower);
        castRange = Mathf.Max(0f, castRange);
        aimConeHalfAngle = Mathf.Clamp(aimConeHalfAngle, 1f, 180f);
    }
}
