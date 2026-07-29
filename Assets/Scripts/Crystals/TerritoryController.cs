using UnityEngine;

/// <summary>
/// Pha lê đen (Obelisk) - công trình lãnh thổ của người chơi (đặt qua GridBuildingSystem như mọi
/// công trình khác). Xác định bán kính lãnh thổ (dùng cho vòng hiển thị chuột phải và
/// <see cref="IHasInfluenceRadius"/>, và cho <see cref="TerritoryAuraBuff"/> buff unit phe người
/// chơi đứng trong vùng). Placeholder hình ảnh giống <see cref="CrystalNode"/>: SpriteRenderer
/// sprite tròn mặc định, tô màu tối - đổi sang asset thật sau chỉ cần đổi field
/// <see cref="bodyRenderer"/>.
/// </summary>
[DisallowMultipleComponent]
public class TerritoryController : MonoBehaviour, IHasInfluenceRadius
{
    [Tooltip("Bán kính vùng lãnh thổ - unit phe người chơi đứng TRONG vùng này được buff chỉ số.")]
    [SerializeField] private float territoryRadius = 15f;

    [Tooltip("Màu vòng hiển thị khi chuột phải vào pha lê đen.")]
    [SerializeField] private Color radiusColor = Color.green;

    [Tooltip("Placeholder hình ảnh - SpriteRenderer dùng sprite tròn mặc định, tô màu tối.")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("Collider trigger LỚN phủ hết territoryRadius (giữ để đồng bộ bán kính vùng lãnh thổ).")]
    [SerializeField] private CircleCollider2D territoryCollider;

    public float Radius => territoryRadius;
    public Color RadiusColor => radiusColor;
    public Transform Origin => transform;

    private void Awake()
    {
        RefreshTerritoryCollider();
    }

    private void OnValidate()
    {
        territoryRadius = Mathf.Max(0f, territoryRadius);
        RefreshTerritoryCollider();
    }

    private void RefreshTerritoryCollider()
    {
        if (territoryCollider == null)
        {
            return;
        }

        territoryCollider.isTrigger = true;
        territoryCollider.radius = territoryRadius;
    }
}
