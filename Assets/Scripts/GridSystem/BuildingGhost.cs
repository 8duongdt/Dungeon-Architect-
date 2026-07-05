using UnityEngine;

/// <summary>
/// "Bóng mờ" của công trình đang chọn, bám theo ô lưới dưới chuột để người chơi biết sẽ xây ở đâu.
/// Tô xanh khi đặt được, đỏ khi không. Tự ẩn khi chưa chọn loại công trình nào.
/// </summary>
[RequireComponent(typeof(GridBuildingSystem))]
public class BuildingGhost : MonoBehaviour
{
    [SerializeField]
    private Color canBuildColor = new Color(0f, 1f, 0f, 0.5f);

    [SerializeField]
    private Color cannotBuildColor = new Color(1f, 0f, 0f, 0.5f);

    [Tooltip("Tùy chọn - hiện vòng bán kính tinh thể cùng loại gần nhất khi đang cầm công trình khai thác.")]
    [SerializeField]
    private RadiusIndicatorDisplay crystalRadiusHint;

    private GridBuildingSystem buildingSystem;
    private PlacedObjectTypeSO shownType;
    private Transform visualInstance;
    private SpriteRenderer[] visualRenderers;

    private void Awake()
    {
        buildingSystem = GetComponent<GridBuildingSystem>();
    }

    private void Update()
    {
        PlacedObjectTypeSO activeType = buildingSystem.ActiveType;
        RefreshVisual(activeType);

        if (activeType == null)
        {
            crystalRadiusHint?.Hide();
            return;
        }

        Vector2Int origin = buildingSystem.GetMouseCell();
        visualInstance.position = buildingSystem.GetPlacementWorldPosition(activeType, origin);
        ApplyTint(buildingSystem.CanBuildAt(activeType, origin));
        UpdateCrystalRadiusHint(activeType, visualInstance.position);
    }

    // Đang cầm máy đào/lò ma thuật thì hiện luôn vòng bán kính của tinh thể cùng loại gần nhất,
    // để người chơi biết đặt trong vùng nào mới hoạt động được (xem CrystalBuildRestriction).
    private void UpdateCrystalRadiusHint(PlacedObjectTypeSO activeType, Vector3 placementWorldPosition)
    {
        if (crystalRadiusHint == null)
        {
            return;
        }

        CrystalType? requiredType = CrystalBuildRestriction.GetRequiredCrystalType(activeType);
        CrystalNode nearestSameType = requiredType.HasValue
            ? CrystalNodeRegistry.FindNearestOfType(placementWorldPosition, requiredType.Value)
            : null;

        if (nearestSameType == null)
        {
            crystalRadiusHint.Hide();
            return;
        }

        crystalRadiusHint.Show(nearestSameType.transform.position, nearestSameType.InfluenceRadius, nearestSameType.RadiusColor);
    }

    // Đảm bảo instance ghost khớp với loại đang chọn (tạo mới khi đổi loại, hủy khi bỏ chọn).
    private void RefreshVisual(PlacedObjectTypeSO activeType)
    {
        if (activeType == shownType)
        {
            return;
        }
        shownType = activeType;

        if (visualInstance != null)
        {
            Destroy(visualInstance.gameObject);
            visualInstance = null;
            visualRenderers = null;
        }

        if (activeType != null && activeType.visual != null)
        {
            visualInstance = Instantiate(activeType.visual, transform);
            visualRenderers = visualInstance.GetComponentsInChildren<SpriteRenderer>();
        }
    }

    private void ApplyTint(bool canBuild)
    {
        if (visualRenderers == null)
        {
            return;
        }

        Color tint = canBuild ? canBuildColor : cannotBuildColor;
        foreach (SpriteRenderer renderer in visualRenderers)
        {
            renderer.color = tint;
        }
    }
}
