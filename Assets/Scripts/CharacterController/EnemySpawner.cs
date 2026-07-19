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
/// Số CON mỗi đợt bị ghim thêm: đợt đầu đúng <see cref="firstWaveEnemyCount"/> con, các đợt sau
/// rút ngẫu nhiên 1..<see cref="maxEnemiesPerWave"/> con - nhịp dồn quái do manager quyết qua
/// <see cref="OverrideSchedule"/> (so le các cổng để toàn bản đồ mỗi chu kỳ chỉ một cổng ra đợt).
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

    [Tooltip("Trần SỐ CON mỗi đợt - mỗi đợt rút ngẫu nhiên 1..trần con; 0 = không giới hạn như cũ.")]
    [SerializeField]
    [Min(0)]
    private int maxEnemiesPerWave = 3;

    [Tooltip("Đợt ĐẦU TIÊN sinh đúng chừng này con để người chơi làm quen - 0 = như đợt thường.")]
    [SerializeField]
    [Min(0)]
    private int firstWaveEnemyCount = 1;

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

    [Tooltip("Tổng số quái cổng này sinh ra trong cả màn - 0 = sinh vô hạn như cũ.")]
    [SerializeField]
    [Min(0)]
    private int maxTotalSpawns = 0;

    [Header("Random Offset")]
    [Tooltip("Bán kính phân tán quái quanh cổng để tránh đè chồng lên nhau. Đặt 0 để sinh ngay tại cổng.")]
    [SerializeField]
    [Min(0f)]
    private float spawnRadius = 1.5f;

    // Bộ đếm thời gian thực và số lượng quái hiện tại do cổng này quản lý.
    private float timer;
    private int currentEnemyCount;
    private int totalSpawnedCount;
    private bool hasSpawnedFirstWave;

    /// <summary>Cổng đã sinh đủ hạn ngạch (chỉ có nghĩa khi maxTotalSpawns > 0) - director đọc để xét thắng.</summary>
    public bool HasFinishedSpawning => maxTotalSpawns > 0 && totalSpawnedCount >= maxTotalSpawns;

    /// <summary>Số quái của cổng này còn sống - director cộng dồn để xét điều kiện diệt sạch.</summary>
    public int AliveEnemyCount => currentEnemyCount;

    /// <summary>
    /// Cho manager so le lịch giữa các cổng (round-robin toàn bản đồ).
    /// Gọi ngay sau Instantiate - trước khi Start() dựng timer từ hai giá trị này.
    /// </summary>
    public void OverrideSchedule(float firstWaveDelay, float interval)
    {
        initialSpawnDelay = Mathf.Max(0f, firstWaveDelay);
        spawnInterval = Mathf.Max(0.01f, interval);
    }

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
        // Kiểm tra điều kiện biên: cạn hạn ngạch tổng, chạm cap riêng của cổng
        // hoặc trần dân số chung thì dừng logic spawn.
        if (HasFinishedSpawning || currentEnemyCount >= maxEnemies || !EnemyPopulationLimiter.HasGlobalCapacity())
        {
            return;
        }

        // Tích lũy thời gian độc lập với FPS.
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnWave(ComposeWave(CurrentBudget(), NextWaveEnemyLimit()));
            hasSpawnedFirstWave = true;
            timer = 0f;
        }
    }

    // Đợt đầu ghim đúng firstWaveEnemyCount con cho người chơi làm quen;
    // các đợt sau rút ngẫu nhiên 1..maxEnemiesPerWave con.
    private int NextWaveEnemyLimit()
    {
        bool capFirstWave = !hasSpawnedFirstWave && firstWaveEnemyCount > 0;
        if (capFirstWave)
        {
            return firstWaveEnemyCount;
        }

        return maxEnemiesPerWave > 0 ? Random.Range(1, maxEnemiesPerWave + 1) : int.MaxValue;
    }

    // Ngân sách hiện tại = gốc + tăng trưởng theo phút chơi, chặn trần.
    private int CurrentBudget()
    {
        float grown = waveBudget + budgetGrowthPerMinute * (Time.timeSinceLevelLoad / 60f);
        return Mathf.Min(Mathf.FloorToInt(grown), maxWaveBudget);
    }

    // Rút thăm ngẫu nhiên các loại quái tới khi cạn ngân sách hoặc chạm trần số con của đợt.
    private List<GameObject> ComposeWave(int budget, int enemyLimit)
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
        while (affordable.Count > 0 && wave.Count < enemyLimit)
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
            if (HasFinishedSpawning || currentEnemyCount >= maxEnemies || !EnemyPopulationLimiter.HasGlobalCapacity())
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
        totalSpawnedCount++;
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
