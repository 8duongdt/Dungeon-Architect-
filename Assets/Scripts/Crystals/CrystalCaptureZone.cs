using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trên mọi tinh thể: theo dõi quái (phe Enemy) đứng trong vùng ảnh hưởng (collider trigger lớn
/// dùng chung của <see cref="CrystalNode"/>). Đứng liên tục đủ <see cref="captureThresholdSeconds"/>
/// giây (không bị đuổi hết ra) thì tinh thể chuyển Captured - áp dụng cho CẢ tinh thể xám (Inactive)
/// lẫn tinh thể người chơi đang giữ (Active), vì chiến dịch Human chiếm mọi cục chưa thuộc về chúng.
/// Giết sạch quái trong vùng thì tự động giải phóng - tinh thể trả về đúng trạng thái trước khi bị
/// chiếm (Active nếu từng kích hoạt, Inactive nếu chưa - xem <see cref="CrystalNode.Reactivate"/>).
/// Không cần bộ đếm số lần giết riêng, chỉ cần soi tập <see cref="enemiesInside"/> đang rỗng hay không.
/// Theo mẫu theo dõi cái chết của <see cref="EnemySpawner.TrackEnemyDeath"/>.
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class CrystalCaptureZone : MonoBehaviour
{
    [Tooltip("Số giây quái phải đứng liên tục trong vùng (không bị đuổi ra hết) để chiếm tinh thể.")]
    [SerializeField] private float captureThresholdSeconds = 3f;

    private CrystalNode crystalNode;
    private readonly HashSet<UnitHealth> enemiesInside = new HashSet<UnitHealth>();
    private float captureTimer;

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
    }

    private void Update()
    {
        if (crystalNode.State != CrystalState.Captured)
        {
            TickCaptureTimer();
        }
    }

    private void TickCaptureTimer()
    {
        if (enemiesInside.Count == 0)
        {
            captureTimer = 0f;
            return;
        }

        captureTimer += Time.deltaTime;
        if (captureTimer >= captureThresholdSeconds)
        {
            captureTimer = 0f;
            crystalNode.Capture();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        if (faction == null || faction.Faction != FactionType.Enemy)
        {
            return;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || !enemiesInside.Add(health))
        {
            return;
        }

        health.Died += HandleEnemyDied;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemoveEnemy(other.GetComponentInParent<UnitHealth>());
    }

    private void HandleEnemyDied(UnitHealth health)
    {
        RemoveEnemy(health);
    }

    private void RemoveEnemy(UnitHealth health)
    {
        if (health == null || !enemiesInside.Remove(health))
        {
            return;
        }

        health.Died -= HandleEnemyDied;
        TryReactivate();
    }

    private void TryReactivate()
    {
        if (enemiesInside.Count == 0 && crystalNode.State == CrystalState.Captured)
        {
            crystalNode.Reactivate();
        }
    }
}
