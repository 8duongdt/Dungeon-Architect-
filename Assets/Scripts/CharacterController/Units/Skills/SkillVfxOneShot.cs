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

    private void Start()
    {
        Destroy(gameObject, ResolveAnimationLength() + extraLifetime);
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
