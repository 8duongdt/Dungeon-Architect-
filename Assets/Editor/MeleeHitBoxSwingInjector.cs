using UnityEditor;
using UnityEngine;

/// <summary>
/// Công cụ Editor tiêm chuyển động HitBox vào các clip tấn công của nhân vật CẬN CHIẾN human,
/// mô phỏng đúng cách quái Orc làm: khi vung đòn, HitBox lao ra theo hướng đánh và chỉ BẬT
/// (active) trong khoảng thời gian chạm đòn, sau đó thu về và tắt.
///
/// Tham chiếu gốc: Assets/Animation/Orc_1/attack_d.anim - animate con "HitBox":
///   - Transform m_LocalPosition: (0,0,0) -> ra xa theo hướng đánh
///   - GameObject  m_IsActive:     0 -> 1 (lúc chạm) -> 0 (cuối clip)
///
/// Vì mọi prefab cận chiến đều có con tên đúng là "HitBox" nên đường dẫn animation dùng chung.
/// Sửa clip trực tiếp bằng AnimationUtility an toàn hơn nhiều so với sửa tay YAML (curve + binding
/// luôn được ghi nhất quán). Chạy MỘT lần qua menu Tools > Dungeon > Add Melee HitBox Swing,
/// rồi tinh chỉnh Reach/StrikeFraction trong cửa sổ Animation nếu cần. Chỉ chạy trong Editor.
/// </summary>
public static class MeleeHitBoxSwingInjector
{
    private const string HitBoxPath = "HitBox";

    // Khoảng cách (đơn vị world) HitBox lao ra theo hướng đánh. Sprite human ~1.4x Orc, attackRange = 1.5.
    private const float Reach = 1.0f;

    // Mốc thời gian (tỉ lệ của độ dài clip) mà HitBox lao tới đích và bật active - đầu nửa sau của đòn.
    private const float StrikeFraction = 0.3f;

    // Các unit cận chiến và 4 clip tấn công theo hướng mà blend tree tấn công của chúng tham chiếu.
    private static readonly string[] MeleeUnits =
    {
        "Swordsman_lvl1",
        "Swordsman_lvl2",
        "Swordsman_lvl3",
        "TheTank_Master",
        "TheTank_Apprentice",
    };

    private static readonly (string name, Vector2 direction)[] Directions =
    {
        ("east", Vector2.right),
        ("west", Vector2.left),
        ("north", Vector2.up),
        ("south", Vector2.down),
    };

    [MenuItem("Tools/Dungeon/Add Melee HitBox Swing")]
    private static void InjectAll()
    {
        int injectedClips = 0;
        foreach (string unit in MeleeUnits)
        {
            foreach ((string directionName, Vector2 direction) in Directions)
            {
                string clipPath = $"Assets/Animation/{unit}/attack/attack_{directionName}.anim";
                if (TryInjectClip(clipPath, direction))
                {
                    injectedClips++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MeleeHitBoxSwingInjector] Đã tiêm chuyển động HitBox vào {injectedClips} clip tấn công.");
    }

    private static bool TryInjectClip(string clipPath, Vector2 direction)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"[MeleeHitBoxSwingInjector] Bỏ qua - không tìm thấy clip: {clipPath}");
            return false;
        }

        float clipLength = clip.length;
        float strikeTime = clipLength * StrikeFraction;

        // Chỉ animate trục thực sự có chuyển động; trục còn lại giữ nguyên vị trí gốc của HitBox.
        if (!Mathf.Approximately(direction.x, 0f))
        {
            SetTransformCurve(clip, "m_LocalPosition.x", direction.x * Reach, strikeTime);
        }
        if (!Mathf.Approximately(direction.y, 0f))
        {
            SetTransformCurve(clip, "m_LocalPosition.y", direction.y * Reach, strikeTime);
        }
        SetActivationCurve(clip, strikeTime, clipLength);

        EditorUtility.SetDirty(clip);
        return true;
    }

    // HitBox lao từ vị trí gốc (0) tới targetValue tại strikeTime rồi giữ nguyên tới hết clip.
    private static void SetTransformCurve(AnimationClip clip, string property, float targetValue, float strikeTime)
    {
        AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, strikeTime, targetValue);
        var binding = EditorCurveBinding.FloatCurve(HitBoxPath, typeof(Transform), property);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    // HitBox tắt lúc đầu, bật đúng khoảnh khắc chạm đòn (strikeTime), tắt lại ở cuối clip.
    private static void SetActivationCurve(AnimationClip clip, float strikeTime, float clipLength)
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(strikeTime, 1f),
            new Keyframe(clipLength, 0f));

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }

        var binding = EditorCurveBinding.FloatCurve(HitBoxPath, typeof(GameObject), "m_IsActive");
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
}
