using UnityEngine;

/// <summary>
/// Pha lê đen - công trình lãnh thổ của người chơi (đặt qua GridBuildingSystem như mọi công
/// trình khác). Unit phe người chơi ra khỏi bán kính này bị <see cref="TerritoryZone"/> áp penalty
/// toàn bộ chỉ số. Placeholder hình ảnh giống <see cref="CrystalNode"/>: SpriteRenderer sprite
/// tròn mặc định, tô màu tối - đổi sang asset thật sau chỉ cần đổi field <see cref="bodyRenderer"/>.
/// </summary>
[DisallowMultipleComponent]
public class TerritoryController : MonoBehaviour, IHasInfluenceRadius
{
    [Tooltip("Bán kính vùng lãnh thổ - unit phe người chơi ra khỏi vùng này bị giảm chỉ số.")]
    [SerializeField] private float territoryRadius = 15f;

    [Tooltip("Màu vòng hiển thị khi chuột phải vào pha lê đen.")]
    [SerializeField] private Color radiusColor = Color.green;

    [Tooltip("Placeholder hình ảnh - SpriteRenderer dùng sprite tròn mặc định, tô màu tối.")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("Collider trigger LỚN phủ hết territoryRadius - TerritoryZone lắng nghe enter/exit trên đây.")]
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
