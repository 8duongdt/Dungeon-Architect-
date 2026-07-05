using UnityEngine;

/// <summary>
/// Cấu hình cho MỘT loại tinh thể (Gold/Mana/Progress): sprite/màu theo trạng thái + bán kính
/// ảnh hưởng theo kích cỡ (nhỏ/to). Tách khỏi <see cref="CrystalNode"/> để designer chỉnh
/// sprite/bán kính bằng asset, không phải sửa code hay từng instance ngoài scene.
/// </summary>
[CreateAssetMenu(fileName = "CrystalTypeConfig_", menuName = "Building/CrystalTypeConfig")]
public class CrystalTypeConfigSO : ScriptableObject
{
    [Tooltip("Loại tinh thể mà config này áp dụng.")]
    public CrystalType type;

    [Header("Sprite theo trạng thái (mỗi loại tinh thể có bộ art xám/nguyên bản/đỏ riêng)")]
    public Sprite inactiveSprite;
    public Sprite activeSprite;
    public Sprite capturedSprite;

    [Header("Màu theo trạng thái (fallback tint khi trạng thái chưa có sprite riêng)")]
    public Color inactiveColor = Color.gray;
    public Color activeColor = Color.white;
    public Color capturedColor = Color.red;

    [Header("Bán kính ảnh hưởng theo kích cỡ")]
    [Min(0f)]
    public float smallRadius = 4f;

    [Min(0f)]
    public float largeRadius = 7f;

    /// <summary>Màu tương ứng với một trạng thái - chỉ dùng khi trạng thái đó chưa có sprite riêng.</summary>
    public Color ColorFor(CrystalState state)
    {
        switch (state)
        {
            case CrystalState.Active:
                return activeColor;
            case CrystalState.Captured:
                return capturedColor;
            default:
                return inactiveColor;
        }
    }

    /// <summary>Sprite tương ứng với một trạng thái - null nếu loại này chưa được gán art cho trạng thái đó.</summary>
    public Sprite SpriteFor(CrystalState state)
    {
        switch (state)
        {
            case CrystalState.Active:
                return activeSprite;
            case CrystalState.Captured:
                return capturedSprite;
            default:
                return inactiveSprite;
        }
    }
}
