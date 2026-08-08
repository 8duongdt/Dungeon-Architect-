using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiếp viện tại tinh thể bị chiếm (đỏ): trong lúc <see cref="CrystalNode.State"/> == Captured,
/// tinh thể sinh quân Human định kỳ theo LOẠI của nó - Gold sinh cận chiến, Mana sinh tầm xa,
/// Progress không sinh. Giới hạn <see cref="maxAlive"/> quân sống do cục này quản lý để không tràn
/// màn (theo mẫu death-tracking của <see cref="EnemySpawner"/>). Quân sinh ra có sẵn
/// <see cref="CrystalCampaignAgent"/> nên tự nhập đồn trú/chiến dịch qua IdleState.
/// Mỗi lần sinh còn tôn trọng trần dân số chung toàn bản đồ (<see cref="EnemyPopulationLimiter"/>)
/// và nhịp sóng toàn cục (<see cref="EnemyPopulationLimiter.TryClaimWaveSlot"/> - tránh trùng lúc với
/// cổng/tinh thể khác). Mỗi loại quân còn có <see cref="CrystalSpawnEntry.minSizeLevel"/> - tự suy ra
/// cỡ map hiện tại từ <see cref="MapSizeProgression"/> (giống EnemySpawner) để chỉ chọn loại đã mở khóa.
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class CrystalReinforcementSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CrystalSpawnEntry
    {
        [Tooltip("Prefab quân của loại này.")]
        public GameObject prefab;

        [Tooltip("Cỡ map tối thiểu để loại quân này được chọn (1 = luôn có thể xuất hiện).")]
        [Min(1)]
        public int minSizeLevel = 1;
    }

    [Tooltip("Quân cận chiến sinh ra khi tinh thể GOLD bị chiếm.")]
    [SerializeField] private List<CrystalSpawnEntry> meleeEntries = new List<CrystalSpawnEntry>();

    [Tooltip("Quân tầm xa sinh ra khi tinh thể MANA bị chiếm.")]
    [SerializeField] private List<CrystalSpawnEntry> rangedEntries = new List<CrystalSpawnEntry>();

    [Tooltip("Cây kỹ năng - dùng suy ra cỡ map hiện tại (giống DungeonManager) để lọc quân theo minSizeLevel.")]
    [SerializeField] private SkillTreeSO skillTree;

    [Tooltip("Khoảng thời gian (giây) giữa hai lần sinh tiếp viện.")]
    [SerializeField]
    [Min(0.01f)]
    private float spawnInterval = 20f;

    [Tooltip("Số quân sống tối đa do tinh thể này quản lý.")]
    [SerializeField]
    [Min(0)]
    private int maxAlive = 5;

    [Tooltip("Bán kính phân tán quân quanh tinh thể khi sinh ra.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 1.2f;

    private CrystalNode crystalNode;
    private float timer;
    private int aliveCount;
    private int currentSizeLevel = 1;

    // Nhịp thực tế sau khi áp áp lực thích ứng: người chơi càng đông quân, tiếp viện về càng dày.
    private float EffectiveSpawnInterval => spawnInterval * EnemyPopulationLimiter.SpawnIntervalMultiplier();

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
        currentSizeLevel = MapSizeProgression.LevelFor(skillTree);
    }

    private void Update()
    {
        if (crystalNode.State != CrystalState.Captured || aliveCount >= maxAlive
            || !EnemyPopulationLimiter.HasGlobalCapacity())
        {
            // Mất cục (hoặc đầy quân / chạm trần dân số chung) thì timer về 0
            // - vừa chiếm lại phải chờ trọn một chu kỳ.
            timer = 0f;
            return;
        }

        List<CrystalSpawnEntry> pool = UnlockedReinforcementPool();
        if (pool.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;
        // Chưa tới lượt (nguồn sinh khác vừa nhả sóng) thì KHÔNG reset timer - tick sau thử lại ngay
        // thay vì bỏ lỡ nguyên một chu kỳ spawnInterval.
        if (timer >= EffectiveSpawnInterval && EnemyPopulationLimiter.TryClaimWaveSlot())
        {
            timer = 0f;
            SpawnReinforcement(pool[Random.Range(0, pool.Count)].prefab);
        }
    }

    // Loại quân theo loại tinh thể (Gold nuôi cận chiến, Mana nuôi tầm xa, Progress không nuôi gì),
    // đã lọc theo cỡ map hiện tại.
    private List<CrystalSpawnEntry> UnlockedReinforcementPool()
    {
        List<CrystalSpawnEntry> pool = ReinforcementPool();
        var unlocked = new List<CrystalSpawnEntry>();
        foreach (CrystalSpawnEntry entry in pool)
        {
            if (entry.prefab != null && entry.minSizeLevel <= currentSizeLevel)
            {
                unlocked.Add(entry);
            }
        }
        return unlocked;
    }

    private List<CrystalSpawnEntry> ReinforcementPool()
    {
        switch (crystalNode.Type)
        {
            case CrystalType.Gold:
                return meleeEntries;
            case CrystalType.Mana:
                return rangedEntries;
            default:
                return EmptyPool;
        }
    }

    private static readonly List<CrystalSpawnEntry> EmptyPool = new List<CrystalSpawnEntry>();

    private void SpawnReinforcement(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        // Chỉ nhận ô đi lại được quanh tinh thể - không rơi vào tường/ngoài map trên map quần đảo.
        Vector3 spawnPosition = SpawnPlacement.RollSpawnPosition(transform.position, spawnRadius);

        GameObject unit = Instantiate(prefab, spawnPosition, Quaternion.identity);
        aliveCount++;
        EnemyPopulationLimiter.Instance?.Register(unit);
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
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }
}
