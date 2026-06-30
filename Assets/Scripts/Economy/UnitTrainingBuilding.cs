using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn lên công trình "trại huấn luyện lính". Cứ mỗi <see cref="spawnInterval"/> giây, trại lần lượt
/// (xoay vòng) huấn luyện một loại lính trong <see cref="trainableUnits"/>, trừ Vàng/Mana tương ứng
/// qua <see cref="ResourceManager"/>, rồi sinh prefab quanh trại. Giới hạn số lính sống cùng lúc
/// bằng <see cref="maxUnits"/>; lính chết thì chỗ trống được trả lại để huấn luyện tiếp.
///
/// Mỗi cấp trại (Crypt/Hellforge/Dark) cấu hình 3 loại lính (Orc/Slime/Vampire) ở cấp tương ứng,
/// mỗi loại có chi phí Vàng/Mana riêng. Null-safe nếu scene chưa có ResourceManager (coi như miễn phí).
/// </summary>
public class UnitTrainingBuilding : MonoBehaviour, IConstructInfo
{
    [System.Serializable]
    public class TrainableUnit
    {
        [Tooltip("Prefab lính được huấn luyện.")]
        public GameObject prefab;

        [Tooltip("Chi phí Vàng cho mỗi lần huấn luyện loại lính này.")]
        [Min(0)]
        public int goldCost;

        [Tooltip("Chi phí Mana cho mỗi lần huấn luyện loại lính này.")]
        [Min(0)]
        public int manaCost;
    }

    [Header("Danh sách lính huấn luyện (xoay vòng)")]
    [SerializeField]
    private List<TrainableUnit> trainableUnits = new List<TrainableUnit>();

    [Header("Thiết lập huấn luyện")]
    [Tooltip("Khoảng thời gian (giây) giữa hai lần huấn luyện.")]
    [SerializeField]
    [Min(0.01f)]
    private float spawnInterval = 5f;

    [Tooltip("Số lính sống tối đa mà trại này quản lý cùng lúc.")]
    [SerializeField]
    [Min(0)]
    private int maxUnits = 6;

    [Tooltip("Bán kính phân tán lính quanh trại khi sinh ra.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 1f;

    private float timer;
    private int currentUnitCount;
    private int roundRobinIndex;

    // Hệ số nâng cấp: chu kỳ nhanh hơn và cộng giới hạn lính (do BuildingUpgrade đặt).
    private float intervalMultiplier = 1f;
    private int maxUnitsBonus;

    private float EffectiveInterval => Mathf.Max(0.01f, spawnInterval * intervalMultiplier);
    private int EffectiveMaxUnits => maxUnits + maxUnitsBonus;

    public string TypeLabel => "Barracks";

    public IEnumerable<string> GetStatLines()
    {
        yield return $"Train Interval: {EffectiveInterval:0.#}s  (max {EffectiveMaxUnits})";
        foreach (TrainableUnit unit in trainableUnits)
        {
            if (unit.prefab != null)
            {
                yield return $"- {unit.prefab.name}: {unit.goldCost}G {unit.manaCost}M";
            }
        }
    }

    /// <summary>Áp hiệu ứng nâng cấp: nhân chu kỳ huấn luyện và cộng giới hạn lính.</summary>
    public void ApplyUpgrade(float newIntervalMultiplier, int newMaxUnitsBonus)
    {
        intervalMultiplier = Mathf.Max(0.01f, newIntervalMultiplier);
        maxUnitsBonus = Mathf.Max(0, newMaxUnitsBonus);
    }

    private void Update()
    {
        if (currentUnitCount >= EffectiveMaxUnits || trainableUnits.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer < EffectiveInterval)
        {
            return;
        }

        timer -= EffectiveInterval;
        TrainNextAffordableUnit();
    }

    private void TrainNextAffordableUnit()
    {
        int affordableIndex = FindNextAffordableIndex();
        if (affordableIndex < 0)
        {
            return;
        }

        TrainableUnit unit = trainableUnits[affordableIndex];
        roundRobinIndex = (affordableIndex + 1) % trainableUnits.Count;

        PayFor(unit);
        SpawnUnit(unit.prefab);
    }

    // Quét xoay vòng từ vị trí hiện tại, trả về loại lính đầu tiên đủ tài nguyên (và có prefab).
    private int FindNextAffordableIndex()
    {
        for (int offset = 0; offset < trainableUnits.Count; offset++)
        {
            int index = (roundRobinIndex + offset) % trainableUnits.Count;
            TrainableUnit unit = trainableUnits[index];
            if (unit.prefab != null && CanAfford(unit))
            {
                return index;
            }
        }
        return -1;
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
