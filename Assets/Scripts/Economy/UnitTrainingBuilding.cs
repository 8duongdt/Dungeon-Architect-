using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn lên công trình "trại huấn luyện lính". Người chơi triệu hồi THỦ CÔNG qua Cửa sổ Trạng thái:
/// bấm avatar lính -> trừ Vàng/Mana NGAY và xếp vào hàng chờ (<see cref="TryEnqueue"/>); trại lần
/// lượt sinh từng con theo thứ tự bấm, mỗi con xong phải chờ hết thời gian hồi
/// (<see cref="spawnInterval"/>) mới sinh con kế. Bấm chuột phải avatar hủy một con trong hàng chờ
/// và hoàn lại tài nguyên (<see cref="TryCancelOne"/>). Giới hạn tổng lính sống + đang chờ bằng
/// <see cref="maxUnits"/>; lính chết thì chỗ trống được trả lại.
///
/// Nâng cấp trại (BuildingUpgrade) rút ngắn thời gian hồi, cộng giới hạn lính và mở khóa thêm
/// loại lính cao cấp trong <see cref="trainableUnits"/> (danh sách xếp theo thứ tự mở khóa).
/// Null-safe nếu scene chưa có ResourceManager (coi như miễn phí).
/// </summary>
public class UnitTrainingBuilding : MonoBehaviour, IConstructInfo
{
    [System.Serializable]
    public class TrainableUnit
    {
        [Tooltip("Prefab lính được huấn luyện.")]
        public GameObject prefab;

        [Tooltip("Avatar hiển thị trên ô triệu hồi - bỏ trống sẽ lấy sprite của prefab.")]
        public Sprite portrait;

        [Tooltip("Chi phí Vàng cho mỗi lần huấn luyện loại lính này.")]
        [Min(0)]
        public int goldCost;

        [Tooltip("Chi phí Mana cho mỗi lần huấn luyện loại lính này.")]
        [Min(0)]
        public int manaCost;
    }

    [Header("Danh sách lính (xếp theo thứ tự mở khóa khi nâng cấp)")]
    [SerializeField]
    private List<TrainableUnit> trainableUnits = new List<TrainableUnit>();

    [Header("Thiết lập triệu hồi")]
    [Tooltip("Thời gian hồi (giây) giữa hai lần triệu hồi.")]
    [SerializeField]
    [Min(0.01f)]
    private float spawnInterval = 5f;

    [Tooltip("Giới hạn tổng lính sống + đang trong hàng chờ của trại này.")]
    [SerializeField]
    [Min(0)]
    private int maxUnits = 6;

    [Tooltip("Bán kính phân tán lính quanh trại khi sinh ra.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 1f;

    // Hàng chờ triệu hồi: index vào trainableUnits, theo đúng thứ tự bấm (đã trừ tiền trước).
    private readonly List<int> pendingQueue = new List<int>();
    private float cooldownRemaining;
    private int currentUnitCount;

    // Hệ số nâng cấp: hồi nhanh hơn, cộng giới hạn lính, mở khóa thêm loại lính (do BuildingUpgrade đặt).
    private float intervalMultiplier = 1f;
    private int maxUnitsBonus;
    private int unlockedUnitCount = int.MaxValue;

    private float EffectiveInterval => Mathf.Max(0.01f, spawnInterval * intervalMultiplier);
    private int EffectiveMaxUnits => maxUnits + maxUnitsBonus;

    /// <summary>Toàn bộ loại lính cấu hình trên trại (kể cả chưa mở khóa) - UI đọc để dựng ô avatar.</summary>
    public IReadOnlyList<TrainableUnit> TrainableUnits => trainableUnits;

    /// <summary>Số loại lính đã mở khóa ở cấp trại hiện tại (đếm từ đầu danh sách).</summary>
    public int UnlockedUnitCount => Mathf.Min(trainableUnits.Count, unlockedUnitCount);

    /// <summary>Phần thời gian hồi còn lại (1 = vừa triệu hồi, 0 = sẵn sàng) - UI vẽ vòng quét tối.</summary>
    public float CooldownFraction => cooldownRemaining <= 0f ? 0f : cooldownRemaining / EffectiveInterval;

    public string TypeLabel => "Barracks";

    public IEnumerable<string> GetStatLines()
    {
        yield return $"Summon Cooldown: {EffectiveInterval:0.#}s";
        yield return $"Units: {currentUnitCount} alive + {pendingQueue.Count} queued (max {EffectiveMaxUnits})";
    }

    /// <summary>Áp hiệu ứng nâng cấp: nhân thời gian hồi, cộng giới hạn lính và mở khóa loại lính.
    /// <paramref name="newUnlockedUnitCount"/> &lt;= 0 nghĩa là mở khóa toàn bộ danh sách.</summary>
    public void ApplyUpgrade(float newIntervalMultiplier, int newMaxUnitsBonus, int newUnlockedUnitCount)
    {
        intervalMultiplier = Mathf.Max(0.01f, newIntervalMultiplier);
        maxUnitsBonus = Mathf.Max(0, newMaxUnitsBonus);
        unlockedUnitCount = newUnlockedUnitCount <= 0 ? int.MaxValue : newUnlockedUnitCount;
    }

    /// <summary>Số con của loại lính này đang nằm trong hàng chờ - UI hiện badge.</summary>
    public int QueuedCount(int unitIndex)
    {
        int count = 0;
        foreach (int queuedIndex in pendingQueue)
        {
            if (queuedIndex == unitIndex)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>Có thể bấm triệu hồi loại lính này không (đã mở khóa, đủ tiền, chưa chạm giới hạn).</summary>
    public bool CanEnqueue(int unitIndex)
    {
        bool isUnlockedValidUnit = unitIndex >= 0
            && unitIndex < UnlockedUnitCount
            && trainableUnits[unitIndex].prefab != null;
        if (!isUnlockedValidUnit)
        {
            return false;
        }

        bool isBelowCap = currentUnitCount + pendingQueue.Count < EffectiveMaxUnits;
        return isBelowCap && CanAfford(trainableUnits[unitIndex]);
    }

    /// <summary>Trừ tiền ngay và xếp một con vào hàng chờ triệu hồi.</summary>
    public bool TryEnqueue(int unitIndex)
    {
        if (!CanEnqueue(unitIndex))
        {
            return false;
        }

        PayFor(trainableUnits[unitIndex]);
        pendingQueue.Add(unitIndex);
        return true;
    }

    /// <summary>Hủy con MỚI BẤM NHẤT của loại lính này khỏi hàng chờ và hoàn lại tài nguyên.</summary>
    public bool TryCancelOne(int unitIndex)
    {
        int lastPosition = pendingQueue.LastIndexOf(unitIndex);
        if (lastPosition < 0)
        {
            return false;
        }

        pendingQueue.RemoveAt(lastPosition);
        Refund(trainableUnits[unitIndex]);
        return true;
    }

    private void Update()
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            return;
        }

        TrySummonNextQueued();
    }

    private void TrySummonNextQueued()
    {
        // Lính chờ vẫn giữ chỗ trong giới hạn, nhưng chỉ sinh ra khi còn chỗ sống thực tế.
        if (pendingQueue.Count == 0 || currentUnitCount >= EffectiveMaxUnits)
        {
            return;
        }

        int unitIndex = pendingQueue[0];
        pendingQueue.RemoveAt(0);
        SpawnUnit(trainableUnits[unitIndex].prefab);
        cooldownRemaining = EffectiveInterval;
    }

    private static bool CanAfford(TrainableUnit unit)
    {
        ResourceManager resources = ResourceManager.Instance;
        return resources == null || resources.CanAfford(unit.goldCost, unit.manaCost);
    }

    private static void PayFor(TrainableUnit unit)
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
        {
            return;
        }

        resources.TrySpendGold(unit.goldCost);
        resources.TrySpendMana(unit.manaCost);
    }

    private static void Refund(TrainableUnit unit)
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
        {
            return;
        }

        resources.AddGold(unit.goldCost);
        resources.AddMana(unit.manaCost);
    }

    private void SpawnUnit(GameObject prefab)
    {
        Vector3 spawnPosition = transform.position;
        if (spawnRadius > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            spawnPosition += new Vector3(offset.x, offset.y, 0f);
        }

        GameObject unit = Instantiate(prefab, spawnPosition, Quaternion.identity);
        currentUnitCount++;
        TrackUnitDeath(unit);
    }

    private void TrackUnitDeath(GameObject unit)
    {
        UnitHealth health = unit.GetComponentInChildren<UnitHealth>();
        if (health != null)
        {
            health.Died += OnUnitDied;
        }
    }

    private void OnUnitDied(UnitHealth health)
    {
        health.Died -= OnUnitDied;
        currentUnitCount = Mathf.Max(0, currentUnitCount - 1);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnRadius <= 0f)
        {
            return;
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
