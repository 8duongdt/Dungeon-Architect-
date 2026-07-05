using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trên mọi tinh thể: theo dõi unit phe người chơi đứng trong vùng ảnh hưởng (collider trigger
/// lớn dùng chung của <see cref="CrystalNode"/>). Đứng liên tục đủ
/// <see cref="activationThresholdSeconds"/> giây khi tinh thể còn Inactive (xám) thì tinh thể
/// chuyển Active (màu nguyên bản) - gương đối xứng của <see cref="CrystalCaptureZone"/> (phe quái
/// chiếm tinh thể Active thành Captured). Chỉ lo Inactive -> Active; đường Captured -> Active
/// đã do CrystalCaptureZone tự tái chiếm khi quét sạch quái, không xử lý trùng ở đây.
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class CrystalActivationZone : MonoBehaviour
{
    [Tooltip("Số giây unit phe người chơi phải đứng liên tục trong vùng để kích hoạt tinh thể.")]
    [SerializeField] private float activationThresholdSeconds = 3f;

    private CrystalNode crystalNode;
    private readonly HashSet<UnitHealth> playersInside = new HashSet<UnitHealth>();
    private float activationTimer;

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
    }

    private void Update()
    {
        if (crystalNode.State == CrystalState.Inactive)
        {
            TickActivationTimer();
        }
    }

    private void TickActivationTimer()
    {
        if (playersInside.Count == 0)
        {
            activationTimer = 0f;
            return;
        }

        activationTimer += Time.deltaTime;
        if (activationTimer >= activationThresholdSeconds)
        {
            activationTimer = 0f;
            crystalNode.Activate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        if (faction == null || faction.Faction != FactionType.Player)
        {
            return;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || !playersInside.Add(health))
        {
            return;
        }

        health.Died += HandlePlayerUnitDied;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemovePlayerUnit(other.GetComponentInParent<UnitHealth>());
    }

    private void HandlePlayerUnitDied(UnitHealth health)
    {
        RemovePlayerUnit(health);
    }

    private void RemovePlayerUnit(UnitHealth health)
    {
        if (health == null || !playersInside.Remove(health))
        {
            return;
        }

        health.Died -= HandlePlayerUnitDied;
    }
}
