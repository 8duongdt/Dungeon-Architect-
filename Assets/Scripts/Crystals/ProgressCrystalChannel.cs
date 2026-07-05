using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trên tinh thể Progress: khi có unit phe người chơi đứng trong vùng ảnh hưởng (collider trigger
/// lớn dùng chung của <see cref="CrystalNode"/>) và tinh thể đang Active, tích tiến trình vào
/// <see cref="LevelProgressTracker"/> mỗi giây. Không làm gì nếu tinh thể không phải loại Progress -
/// nhờ vậy có thể gắn sẵn trên prefab tinh thể dùng chung cho cả ba loại mà không cần kiểm tra
/// riêng ở nơi rải (<see cref="CrystalScatterSpawner"/>).
///
/// Theo đúng mẫu enter/exit + lọc UnitFaction của <see cref="UndeadWaterHazard"/>, nhưng dùng
/// HashSet vì nhiều unit có thể cùng đứng trong vùng - chỉ dừng tích khi KHÔNG còn ai.
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class ProgressCrystalChannel : MonoBehaviour
{
    [Tooltip("Tốc độ tích tiến trình mỗi giây khi có ít nhất một unit phe người chơi đứng trong vùng.")]
    [SerializeField] private float progressPerSecond = 5f;

    private CrystalNode crystalNode;
    private readonly HashSet<UnitFaction> occupants = new HashSet<UnitFaction>();

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
    }

    private void Update()
    {
        bool isChanneling = occupants.Count > 0
            && crystalNode.Type == CrystalType.Progress
            && crystalNode.State == CrystalState.Active;

        if (!isChanneling || LevelProgressTracker.Instance == null)
        {
            return;
        }

        LevelProgressTracker.Instance.AddProgress(progressPerSecond * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        if (faction != null && faction.Faction == FactionType.Player)
        {
            occupants.Add(faction);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        UnitFaction faction = other.GetComponentInParent<UnitFaction>();
        if (faction != null)
        {
            occupants.Remove(faction);
        }
    }
}
