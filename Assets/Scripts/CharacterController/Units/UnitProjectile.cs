using UnityEngine;

/// <summary>
/// Đạn đánh thường của unit tầm xa: bay BÁM theo mục tiêu đã khóa lúc bắn, chạm mới gây
/// sát thương - tách khoảnh khắc "trúng đòn" khỏi frame vung tay để không còn cảnh đứng xa
/// đánh không khí mà mục tiêu vẫn mất máu. Mục tiêu chết/biến mất giữa chừng thì đạn tự hủy.
/// </summary>
[DisallowMultipleComponent]
public class UnitProjectile : MonoBehaviour
{
    // Chốt an toàn khi mục tiêu chạy nhanh hơn đạn hoặc cấu hình tốc độ quá thấp.
    private const float MaxLifetimeSeconds = 4f;
    // Khoảng cách tới tâm mục tiêu được tính là chạm.
    private const float HitDistance = 0.35f;

    private UnitHealth target;
    private float damage;
    private float speed;
    private System.Action onImpact;

    /// <summary>Bên phóng gọi ngay sau Instantiate; onImpact dùng phát âm thanh trúng đòn.</summary>
    public void Initialize(UnitHealth targetHealth, float damageAmount, float flightSpeed, System.Action impactCallback)
    {
        target = targetHealth;
        damage = damageAmount;
        speed = flightSpeed;
        onImpact = impactCallback;
        Destroy(gameObject, MaxLifetimeSeconds);
    }

    private void Update()
    {
        bool targetLost = target == null || target.IsDead;
        if (targetLost)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        float stepDistance = speed * Time.deltaTime;
        bool reachedTarget = toTarget.magnitude <= Mathf.Max(stepDistance, HitDistance);
        if (reachedTarget)
        {
            Impact();
            return;
        }

        Vector3 flightDirection = toTarget.normalized;
        transform.position += flightDirection * stepDistance;
        FaceFlightDirection(flightDirection);
    }

    private void Impact()
    {
        target.TakeDamage(damage);
        onImpact?.Invoke();
        Destroy(gameObject);
    }

    // Sprite đạn vẽ đầu chĩa sang phải nên góc quay lấy thẳng từ Atan2.
    private void FaceFlightDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
