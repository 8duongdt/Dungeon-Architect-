using UnityEngine;

/// <summary>
/// Cầu nối giữa sự kiện chuột phải của <see cref="GridBuildingSystem"/> và
/// <see cref="RadiusIndicatorDisplay"/>: chuột phải trúng tinh thể hoặc một công trình có
/// <see cref="IHasInfluenceRadius"/> (vd pha lê đen) thì hiện vòng bán kính của nó, trúng gì khác
/// thì ẩn đi.
/// </summary>
public class CrystalRadiusPresenter : MonoBehaviour
{
    [SerializeField] private GridBuildingSystem buildingSystem;
    [SerializeField] private RadiusIndicatorDisplay indicator;

    private void OnEnable()
    {
        if (buildingSystem == null)
        {
            return;
        }

        buildingSystem.ConstructInspectRequested += HandleConstructInspectRequested;
        buildingSystem.CrystalInspectRequested += HandleCrystalInspectRequested;
    }

    private void OnDisable()
    {
        if (buildingSystem == null)
        {
            return;
        }

        buildingSystem.ConstructInspectRequested -= HandleConstructInspectRequested;
        buildingSystem.CrystalInspectRequested -= HandleCrystalInspectRequested;
    }

    private void HandleConstructInspectRequested(PlacedObject placedObject)
    {
        ShowOrHide(placedObject.GetComponent<IHasInfluenceRadius>());
    }

    private void HandleCrystalInspectRequested(CrystalNode crystal)
    {
        ShowOrHide(crystal);
    }

    private void ShowOrHide(IHasInfluenceRadius influenceSource)
    {
        if (indicator == null)
        {
            return;
        }

        if (influenceSource == null)
        {
            indicator.Hide();
            return;
        }

        indicator.Show(influenceSource.Origin.position, influenceSource.Radius, influenceSource.RadiusColor);
    }
}
