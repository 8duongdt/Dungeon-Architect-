using UnityEngine;

/// <summary>
/// Âm thanh của hệ thống xây dựng: lắng nghe các event có sẵn trên
/// <see cref="GridBuildingSystem"/> (đặt/đặt-thất-bại/thiếu-tài-nguyên/phá/bị-phá-hủy) để
/// GridBuildingSystem không phải ôm mảng AudioClip.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GridBuildingSystem))]
public class BuildingAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] placedClips;
    [SerializeField] private AudioClip[] placementFailedClips;
    [SerializeField] private AudioClip[] insufficientFundsClips;
    [SerializeField] private AudioClip[] demolishedClips;
    [SerializeField] private AudioClip[] destroyedInCombatClips;

    private GridBuildingSystem buildingSystem;

    private void Awake()
    {
        buildingSystem = GetComponent<GridBuildingSystem>();
    }

    private void OnEnable()
    {
        buildingSystem.Placed += HandlePlaced;
        buildingSystem.PlacementFailed += HandlePlacementFailed;
        buildingSystem.InsufficientFunds += HandleInsufficientFunds;
        buildingSystem.Demolished += HandleDemolished;
        buildingSystem.DestroyedInCombat += HandleDestroyedInCombat;
    }

    private void OnDisable()
    {
        buildingSystem.Placed -= HandlePlaced;
        buildingSystem.PlacementFailed -= HandlePlacementFailed;
        buildingSystem.InsufficientFunds -= HandleInsufficientFunds;
        buildingSystem.Demolished -= HandleDemolished;
        buildingSystem.DestroyedInCombat -= HandleDestroyedInCombat;
    }

    private void HandlePlaced(PlacedObject placedObject)
    {
        AudioManager.Instance.PlayRandom(placedClips);
    }

    private void HandlePlacementFailed()
    {
        AudioManager.Instance.PlayRandom(placementFailedClips);
    }

    private void HandleInsufficientFunds()
    {
        AudioManager.Instance.PlayRandom(insufficientFundsClips);
    }

    private void HandleDemolished()
    {
        AudioManager.Instance.PlayRandom(demolishedClips);
    }

    private void HandleDestroyedInCombat()
    {
        AudioManager.Instance.PlayRandom(destroyedInCombatClips);
    }
}
