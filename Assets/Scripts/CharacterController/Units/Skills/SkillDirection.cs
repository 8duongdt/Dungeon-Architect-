using UnityEngine;

/// <summary>
/// Xoay hiệu ứng/bẫy kỹ năng theo hướng thi triển quanh trục Z. Gắn component này
/// vào prefab nào cần định hướng (SkillVfx_Lightning, SkillTrap_Spikes...) —
/// prefab không gắn thì giữ nguyên hướng mặc định (phép nổ tròn, buff).
/// Caster/executor gọi <see cref="SetupDirection"/> ngay sau khi Instantiate.
/// </summary>
[DisallowMultipleComponent]
public class SkillDirection : MonoBehaviour
{
    [Tooltip("Góc bù vì sprite gốc vẽ theo một hướng cố định: vẽ chĩa phải = 0, chĩa lên = -90, chĩa trái = 180.")]
    [SerializeField] private float spriteFacingOffsetDegrees = 0f;

    /// <summary>Xoay object khớp hướng thi triển; hướng gần bằng 0 thì giữ nguyên mặc định.</summary>
    public void SetupDirection(Vector2 direction)
    {
        bool hasUsableDirection = direction.sqrMagnitude > 0.0001f;
        if (!hasUsableDirection)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        float angle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteFacingOffsetDegrees);
    }
}
