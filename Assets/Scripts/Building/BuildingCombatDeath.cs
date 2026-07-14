using UnityEngine;

/// <summary>
/// Cầu nối giữa cái chết trong chiến đấu (UnitHealth.Died) và hệ xây dựng: khi công trình bị
/// quái phá hủy, ô lưới nó chiếm phải được giải phóng để xây lại - KHÔNG hoàn trả tài nguyên
/// (khác demolish tự nguyện của người chơi, hoàn 70% qua GridBuildingSystem.DemolishPlaced).
/// GameObject tự hủy qua UnitHealth.destroyOnDeath - component này chỉ lo phần lưới.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(UnitHealth))]
public class BuildingCombatDeath : MonoBehaviour
{
    private UnitHealth health;

    private void Awake()
    {
        health = GetComponent<UnitHealth>();
    }

    private void Start()
    {
        // PlacedObject được AddComponent runtime SAU Instantiate (xem PlacedObject.Create) nên phải
        // tra ở Start - Awake của component này chạy trước khi PlacedObject tồn tại trên GameObject.
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        health.Died -= OnDied;
    }

    private void OnDied(UnitHealth deadHealth)
    {
        PlacedObject placedObject = GetComponent<PlacedObject>();
        if (placedObject == null)
        {
            return;
        }

        GridBuildingSystem gridSystem = FindFirstObjectByType<GridBuildingSystem>();
        gridSystem?.FreeCellsWithoutRefund(placedObject);
    }
}
