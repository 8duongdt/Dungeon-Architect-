using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CỔNG SINH QUÁI (Enemy Spawner).
/// Gắn script này vào Portal Object (GameObject 2D) để cổng sinh quái theo ĐỢT bằng
/// "Hệ thống Ngân sách": mỗi chu kỳ <see cref="spawnInterval"/> giây, cổng được cấp một
/// ngân sách điểm rồi rút thăm ngẫu nhiên các loại quái trong <see cref="budgetEntries"/>
/// (mỗi loại một giá điểm) cho tới khi cạn - nên đợt có thể là nhiều quái rẻ hoặc vài quái
/// elite đắt, độ khó tổng thể vẫn được kiểm soát. Ngân sách tăng dần theo thời gian màn chơi
/// (<see cref="budgetGrowthPerMinute"/>, có trần <see cref="maxWaveBudget"/>) để game khó dần.
/// Giới hạn tổng quái sống bằng <see cref="maxEnemies"/>; quái chết trả lại chỗ trống.
/// Ngoài cap riêng của cổng, mỗi lần sinh còn tôn trọng trần dân số chung toàn bản đồ
/// (<see cref="EnemyPopulationLimiter"/>) - không có limiter trong scene thì bỏ qua.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class BudgetSpawnEntry
    {
        [Tooltip("Prefab quái của loại này.")]
        public GameObject prefab;

        [Tooltip("Giá điểm ngân sách - quái thường rẻ, elite đắt.")]
        [Min(1)]
        public int cost = 1;
    }

    [Header("Thành phần đợt quái (Budget System)")]
    [Tooltip("Các loại quái kèm giá điểm - mỗi đợt rút thăm ngẫu nhiên tới khi cạn ngân sách.")]
    [SerializeField]
    private List<BudgetSpawnEntry> budgetEntries = new List<BudgetSpawnEntry>();

    [Tooltip("Prefab dự phòng khi danh sách ngân sách trống (giữ tương thích spawner cũ).")]
    [SerializeField]
    private GameObject enemyPrefab;

    [Header("Ngân sách")]
    [Tooltip("Ngân sách điểm gốc của mỗi đợt.")]
    [SerializeField]
    [Min(1)]
    private int waveBudget = 8;

    [Tooltip("Điểm cộng thêm vào ngân sách sau mỗi phút chơi - để càng về sau đợt càng mạnh.")]
    [SerializeField]
    [Min(0f)]
    private float budgetGrowthPerMinute = 2f;

    [Tooltip("Trần ngân sách - chặn đợt quái phình to vô hạn.")]
    [SerializeField]
    [Min(1)]
    private int maxWaveBudget = 20;

    [Header("Spawn Settings")]
    [Tooltip("Thời gian chờ (giây) trước ĐỢT quái đầu tiên - cho người chơi kịp chuẩn bị. 0 = sinh ngay.")]
    [SerializeField]
    [Min(0f)]
    private float initialSpawnDelay = 30f;

    [Tooltip("Khoảng thời gian (giây) giữa hai ĐỢT sinh quái liên tiếp.")]
    [SerializeField]
    [Min(0.01f)]
    private float spawnInterval = 25f;

    [Tooltip("Số lượng quái tối đa mà cổng này quản lý tại một thời điểm.")]
    [SerializeField]
    [Min(0)]
    private int maxEnemies = 8;

    [Header("Random Offset")]
    [Tooltip("Bán kính phân tán quái quanh cổng để tránh đè chồng lên nhau. Đặt 0 để sinh ngay tại cổng.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 1.5f;

    // Bộ đếm thời gian thực và số lượng quái hiện tại do cổng này quản lý.
    private float timer;
    private int currentEnemyCount;

    private void OnEnable()
    {
        PortalMarkerRegistry.Register(transform);
    }

    private void OnDisable()
    {
        PortalMarkerRegistry.Unregister(transform);
    }

    private void Start()
    {
        // Đợt đầu chờ initialSpawnDelay; các đợt sau theo spawnInterval như cũ.
        // Đặt timer lệch trước một khoảng để cổng mất đúng initialSpawnDelay giây mới chạm ngưỡng spawnInterval.
        timer = spawnInterval - initialSpawnDelay;
    }

    private void Update()
    {
        // Kiểm tra điều kiện biên: chạm cap riêng của cổng hoặc trần dân số chung thì dừng logic spawn.
        if (currentEnemyCount >= maxEnemies || !EnemyPopulationLimiter.HasGlobalCapacity())
        {
            return;
        }

        // Tích lũy thời gian độc lập với FPS.
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnWave(ComposeWave(CurrentBudget()));
            timer = 0f;
        }
    }

    // Ngân sách hiện tại = gốc + tăng trưởng theo phút chơi, chặn trần.
    private int CurrentBudget()
    {
        float grown = waveBudget + budgetGrowthPerMinute * (Time.timeSinceLevelLoad / 60f);
        return Mathf.Min(Mathf.FloorToInt(grown), maxWaveBudget);
    }

    // Rút thăm ngẫu nhiên các loại quái tới khi ngân sách không mua nổi con nào nữa.
    private List<GameObject> ComposeWave(int budget)
    {
        var wave = new List<GameObject>();
        if (budgetEntries.Count == 0)
        {
            // Không cấu hình ngân sách -> đợt một con từ prefab dự phòng như spawner cũ.
            if (enemyPrefab != null)
            {
                wave.Add(enemyPrefab);
            }
            return wave;
        }

        int remaining = budget;
        List<BudgetSpawnEntry> affordable = AffordableEntries(remaining);
        while (affordable.Count > 0)
        {
            BudgetSpawnEntry pick = affordable[Random.Range(0, affordable.Count)];
            wave.Add(pick.prefab);
            remaining -= pick.cost;
            affordable = AffordableEntries(remaining);
        }
        return wave;
    }

    private List<BudgetSpawnEntry> AffordableEntries(int remaining)
    {
        var affordable = new List<BudgetSpawnEntry>();
        foreach (BudgetSpawnEntry entry in budgetEntries)
        {
            if (entry.prefab != null && entry.cost <= remaining)
            {
                affordable.Add(entry);
            }
        }
        return affordable;
    }

    // Sinh cả đợt, tôn trọng giới hạn quái sống - đợt bị cắt khi chạm ngưỡng riêng hoặc trần chung.
    private void SpawnWave(List<GameObject> wave)
    {
        foreach (GameObject prefab in wave)
        {
            if (currentEnemyCount >= maxEnemies || !EnemyPopulationLimiter.HasGlobalCapacity())
            {
                return;
            }
            SpawnEnemy(prefab);
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        // Định vị ngẫu nhiên trong bán kính spawnRadius để tránh các quái đè chồng nhau.
        Vector3 spawnPosition = transform.position;
        if (spawnRadius > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            spawnPosition += new Vector3(offset.x, offset.y, 0f);
        }

        GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
        currentEnemyCount++;
        EnemyPopulationLimiter.Instance?.Register(enemy);

        // Khi quái chết, giảm bộ đếm để cổng có thể tiếp tục sinh quái mới.
        TrackEnemyDeath(enemy);
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
