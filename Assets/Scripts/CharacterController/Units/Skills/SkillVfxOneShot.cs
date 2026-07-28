using UnityEngine;

/// <summary>
/// Hiệu ứng phép một lần: tự hủy GameObject sau khi clip animation dài nhất chạy xong.
/// Gắn trên prefab VFX (SpriteRenderer + Animator với controller một state không loop).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class SkillVfxOneShot : MonoBehaviour
{
    private const float FallbackLifetime = 1f;

    [Tooltip("Thời gian nán lại thêm sau khi animation kết thúc, tránh cắt frame cuối.")]
    [SerializeField] private float extraLifetime = 0.1f;

    private float? lifetimeOverride;

    /// <summary>
    /// Ghi đè thời gian sống thay vì tự hủy theo độ dài clip - dùng cho hiệu ứng kéo dài
    /// theo thông số skill (vd khiên sống đúng ShieldDuration). Gọi ngay sau Instantiate.
    /// </summary>
    public void OverrideLifetime(float seconds)
    {
        lifetimeOverride = Mathf.Max(0f, seconds);
    }

    private void Start()
    {
        float lifetime = lifetimeOverride ?? ResolveAnimationLength() + extraLifetime;
        Destroy(gameObject, lifetime);
    }

    private float ResolveAnimationLength()
    {
        var animator = GetComponent<Animator>();
        if (animator.runtimeAnimatorController == null)
        {
            return FallbackLifetime;
        }

        float longestClipLength = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            longestClipLength = Mathf.Max(longestClipLength, clip.length);
        }

        return longestClipLength > 0f ? longestClipLength : FallbackLifetime;
    }
}
