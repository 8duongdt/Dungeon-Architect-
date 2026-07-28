using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trên tinh thể Progress: khi có unit phe người chơi đứng trong vùng ảnh hưởng (collider trigger
/// lớn dùng chung của <see cref="CrystalNode"/>) và tinh thể đang Active, tích tiến trình THỨC TỈNH
/// vào <see cref="PhaseManager"/> mỗi giây - đầy 100% là qua Phase 2. Đây là đường THẮNG duy nhất
/// của Phase 1 (ngoài phím debug của PhaseManager). Không làm gì nếu tinh thể không phải loại
/// Progress - nhờ vậy có thể gắn sẵn trên prefab tinh thể dùng chung cho cả ba loại mà không cần
/// kiểm tra riêng ở nơi rải (<see cref="CrystalScatterSpawner"/>).
///
/// Theo đúng mẫu enter/exit + lắng nghe UnitHealth.Died của <see cref="CrystalActivationZone"/>:
/// unit CHẾT trong vùng bị Destroy mà KHÔNG bắn OnTriggerExit2D, nên phải gỡ qua sự kiện Died -
/// nếu không phần tử chết kẹt lại và thanh thức tỉnh tự chạy dù không còn ai đứng kênh.
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class ProgressCrystalChannel : MonoBehaviour
{
    // PhaseManager đo tiến trình theo thang 0..1, còn field dưới đây tính theo phần trăm cho dễ chỉnh.
    private const float PercentToFraction = 0.01f;

    [Tooltip("Phần trăm tiến trình thức tỉnh cộng mỗi giây khi có ít nhất một unit phe người chơi " +
        "đứng trong vùng (2 = giữ đủ 50 giây thì đầy thanh - nhắm Phase 1 gói trong 4-5 phút).")]
    [SerializeField] private float progressPerSecond = 2f;

    private CrystalNode crystalNode;
    private readonly HashSet<UnitHealth> occupants = new HashSet<UnitHealth>();

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
    }

    private void Update()
    {
        // Phòng hờ unit bị Destroy không qua Died (vd tắt scene con): dọn phần tử fake-null trước khi đếm.
        occupants.RemoveWhere(occupant => occupant == null);

        bool isChanneling = occupants.Count > 0
            && crystalNode.Type == CrystalType.Progress
            && crystalNode.State == CrystalState.Active;

        if (!isChanneling || PhaseManager.Instance == null)
        {
            return;
        }

        PhaseManager.Instance.AddProgress(progressPerSecond * PercentToFraction * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        if (faction == null || faction.Faction != FactionType.Player)
        {
            return;
        }

        UnitHealth health = other.GetComponentInParent<UnitHealth>();
        if (health == null || !occupants.Add(health))
        {
            return;
        }

        health.Died += HandleOccupantDied;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemoveOccupant(other.GetComponentInParent<UnitHealth>());
    }

    private void HandleOccupantDied(UnitHealth health)
    {
        RemoveOccupant(health);
    }

    private void RemoveOccupant(UnitHealth health)
    {
        if (health == null || !occupants.Remove(health))
        {
            return;
        }

        health.Died -= HandleOccupantDied;
    }
}
