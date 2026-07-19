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
    private BuildingDurability durability;
    private bool isRegisteredAsRaidTarget;

    private void Awake()
    {
        health = GetComponent<UnitHealth>();
        durability = GetComponent<BuildingDurability>();
    }

    private void Start()
    {
        // PlacedObject được AddComponent runtime SAU Instantiate (xem PlacedObject.Create) nên phải
        // tra ở Start - Awake của component này chạy trước khi PlacedObject tồn tại trên GameObject.
        health.Died += OnDied;
        // UnitHealth là NGUỒN SỰ THẬT duy nhất về máu công trình; BuildingDurability chỉ là hình
        // chiếu cho cửa sổ Trạng thái - đồng bộ tuyệt đối mỗi khi máu thật đổi (kể cả đòn kết liễu).
        health.Damaged += OnHealthDamaged;
        SyncDurability();

        // Đăng ký làm mục tiêu để AI Human kéo quân tới phá - chỉ công trình PHE NGƯỜI CHƠI.
        // Làm ở Start (không phải OnEnable) vì UnitFaction/PlacedObject gắn runtime sau Instantiate.
        if (IsPlayerBuilding())
        {
            PlayerBuildingRegistry.Register(transform);
            isRegisteredAsRaidTarget = true;
        }
    }

    private void OnDestroy()
    {
        health.Died -= OnDied;
        health.Damaged -= OnHealthDamaged;
        if (isRegisteredAsRaidTarget)
        {
            PlayerBuildingRegistry.Unregister(transform);
        }
    }

    private bool IsPlayerBuilding()
    {
        UnitFaction faction = GetComponent<UnitFaction>();
        return faction != null && faction.Faction == FactionType.Player;
    }

    private void OnHealthDamaged(UnitHealth damagedHealth, float amount)
    {
        SyncDurability();
    }

    private void SyncDurability()
    {
        durability?.SyncTo(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDied(UnitHealth deadHealth)
    {
        SyncDurability();

        PlacedObject placedObject = GetComponent<PlacedObject>();
        if (placedObject == null)
        {
            return;
        }

        GridBuildingSystem gridSystem = FindAnyObjectByType<GridBuildingSystem>();
        gridSystem?.FreeCellsWithoutRefund(placedObject);
    }
}
