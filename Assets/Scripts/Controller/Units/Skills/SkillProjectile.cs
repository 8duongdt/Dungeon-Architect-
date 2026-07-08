using UnityEngine;

/// <summary>
/// Đạn kỹ năng bay theo đường thẳng: hướng chốt một lần lúc bắn (caster -> vị trí mục tiêu),
/// phát nổ khi trúng kẻ địch ĐẦU TIÊN (qua cổng phe CanAttack - tự loại caster/đồng minh),
/// xuyên qua vật thể không phải unit; tự hủy khi bay quá tầm hoặc quá thời gian sống.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class SkillProjectile : MonoBehaviour
{
    // Chốt an toàn nếu maxRange bị cấu hình quá lớn hoặc đạn không trúng gì.
    private const float MaxLifetimeSeconds = 5f;
    // Nhô đạn khỏi thân caster một chút cho đẹp mắt lúc xuất phát.
    private const float SpawnForwardOffset = 0.4f;

    [Tooltip("Góc bù vì frame sprite vẽ theo hướng cố định (Fire-ball vẽ hướng lên => -90).")]
    [SerializeField] private float spriteFacingOffsetDegrees = -90f;

    private UnitFaction casterFaction;
    private float damage;
    private Vector2 flightDirection;
    private float speed;
    private float maxRange;
    private GameObject impactVfxPrefab;
    private float traveledDistance;
    private bool hasExploded;

    public void Initialize(in SkillCastContext context, SkillDefinitionSO skill)
    {
        casterFaction = context.CasterFaction;
        damage = context.Damage;
        speed = skill.ProjectileSpeed;
        maxRange = skill.ProjectileMaxRange;
        impactVfxPrefab = skill.VfxPrefab;

        Vector2 toTarget = context.PrimaryTarget.transform.position - context.CasterTransform.position;
        flightDirection = toTarget.sqrMagnitude > 0f ? toTarget.normalized : Vector2.right;
        transform.position += (Vector3)(flightDirection * SpawnForwardOffset);
        ApplyFlightRotation();

        Destroy(gameObject, MaxLifetimeSeconds);
    }

    private void Update()
    {
        float stepDistance = speed * Time.deltaTime;
        transform.position += (Vector3)(flightDirection * stepDistance);
        traveledDistance += stepDistance;

        if (traveledDistance >= maxRange)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitHealth targetHealth = other.GetComponentInParent<UnitHealth>();
        if (hasExploded || !IsValidHit(targetHealth))
        {
            return;
        }

        targetHealth.TakeDamage(damage);
        Explode(targetHealth.transform.position);
    }

    private bool IsValidHit(UnitHealth targetHealth)
    {
        if (targetHealth == null || targetHealth.IsDead)
        {
            return false;
        }

        UnitFaction targetFaction = targetHealth.GetComponentInParent<UnitFaction>();
        return casterFaction != null && casterFaction.CanAttack(targetFaction);
    }

    private void Explode(Vector3 impactPosition)
    {
        hasExploded = true;
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, impactPosition, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void ApplyFlightRotation()
    {
        float flightAngle = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, flightAngle + spriteFacingOffsetDegrees);
    }
}
