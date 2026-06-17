using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CỔNG SINH QUÁI (Enemy Spawner).
/// Gắn script này vào Portal Object (GameObject 2D) để cổng tự sinh quái vật
/// theo chu kỳ thời gian, có giới hạn số lượng tối đa.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    [Tooltip("Prefab quái vật được nhân bản. Nếu bỏ trống sẽ dùng danh sách Wave bên dưới.")]
    [SerializeField]
    private GameObject enemyPrefab;

    [Tooltip("Danh sách prefab cho hệ thống theo đợt (Wave). Nếu có phần tử sẽ được ưu tiên dùng.")]
    [SerializeField]
    private List<GameObject> enemyWavePrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    [Tooltip("Khoảng thời gian (giây) giữa hai lần sinh quái liên tiếp.")]
    [SerializeField]
    [Min(0.01f)]
    private float spawnInterval = 3f;

    [Tooltip("Số lượng quái tối đa mà cổng này quản lý tại một thời điểm.")]
    [SerializeField]
    [Min(0)]
    private int maxEnemies = 5;

    [Header("Random Offset")]
    [Tooltip("Bán kính phân tán quái quanh cổng để tránh đè chồng lên nhau. Đặt 0 để sinh ngay tại cổng.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 0.5f;

    // Bộ đếm thời gian thực và số lượng quái hiện tại do cổng này quản lý.
    private float timer;
    private int currentEnemyCount;
    private int waveIndex;

    private void Start()
    {
        // Khởi tạo timer bằng spawnInterval để con quái đầu tiên xuất hiện ngay lập tức.
        timer = spawnInterval;
    }

    private void Update()
    {
        // Kiểm tra điều kiện biên: đã đạt ngưỡng tối đa thì dừng logic spawn.
        if (currentEnemyCount >= maxEnemies)
        {
            return;
        }

        // Tích lũy thời gian độc lập với FPS.
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        GameObject prefab = GetNextPrefab();
        if (prefab == null)
        {
            return;
        }

        // Định vị ngẫu nhiên trong bán kính spawnRadius để tránh các quái đè chồng nhau.
        Vector3 spawnPosition = transform.position;
        if (spawnRadius > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            spawnPosition += new Vector3(offset.x, offset.y, 0f);
        }

        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        currentEnemyCount++;

        // Khi quái chết, giảm bộ đếm để cổng có thể tiếp tục sinh quái mới.
        TrackEnemyDeath(enemy);
    }

    // Lấy prefab kế tiếp: ưu tiên hệ thống Wave nếu có, ngược lại dùng enemyPrefab đơn.
    private GameObject GetNextPrefab()
    {
        if (enemyWavePrefabs != null && enemyWavePrefabs.Count > 0)
        {
            GameObject prefab = enemyWavePrefabs[waveIndex % enemyWavePrefabs.Count];
            waveIndex++;
            return prefab;
        }
        return enemyPrefab;
    }

    private void TrackEnemyDeath(GameObject enemy)
    {
        UnitHealth health = enemy.GetComponentInChildren<UnitHealth>();
        if (health != null)
        {
            health.Died += OnEnemyDied;
        }
    }

    private void OnEnemyDied(UnitHealth health)
    {
        health.Died -= OnEnemyDied;
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
    }

    // Hiển thị bán kính sinh quái trong Scene view để dễ tinh chỉnh.
    private void OnDrawGizmosSelected()
    {
        if (spawnRadius <= 0f)
        {
            return;
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
