using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiếp viện tại tinh thể bị chiếm (đỏ): trong lúc <see cref="CrystalNode.State"/> == Captured,
/// tinh thể sinh quân Human định kỳ theo LOẠI của nó - Gold sinh cận chiến, Mana sinh tầm xa,
/// Progress không sinh. Giới hạn <see cref="maxAlive"/> quân sống do cục này quản lý để không tràn
/// màn (theo mẫu death-tracking của <see cref="EnemySpawner"/>). Quân sinh ra có sẵn
/// <see cref="CrystalCampaignAgent"/> nên tự nhập đồn trú/chiến dịch qua IdleState.
/// Mỗi lần sinh còn tôn trọng trần dân số chung toàn bản đồ (<see cref="EnemyPopulationLimiter"/>).
/// </summary>
[RequireComponent(typeof(CrystalNode))]
public class CrystalReinforcementSpawner : MonoBehaviour
{
    [Tooltip("Quân cận chiến sinh ra khi tinh thể GOLD bị chiếm.")]
    [SerializeField] private List<GameObject> meleePrefabs = new List<GameObject>();

    [Tooltip("Quân tầm xa sinh ra khi tinh thể MANA bị chiếm.")]
    [SerializeField] private List<GameObject> rangedPrefabs = new List<GameObject>();

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

    private void Awake()
    {
        crystalNode = GetComponent<CrystalNode>();
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

        List<GameObject> pool = ReinforcementPool();
        if (pool.Count == 0)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnReinforcement(pool[Random.Range(0, pool.Count)]);
        }
    }

    // Loại quân theo loại tinh thể: Gold nuôi cận chiến, Mana nuôi tầm xa, Progress không nuôi gì.
    private List<GameObject> ReinforcementPool()
    {
        switch (crystalNode.Type)
        {
            case CrystalType.Gold:
                return meleePrefabs;
            case CrystalType.Mana:
                return rangedPrefabs;
            default:
                return EmptyPool;
        }
    }

    private static readonly List<GameObject> EmptyPool = new List<GameObject>();

    private void SpawnReinforcement(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position;
        if (spawnRadius > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            spawnPosition += new Vector3(offset.x, offset.y, 0f);
        }

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
