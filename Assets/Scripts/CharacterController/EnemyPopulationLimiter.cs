using UnityEngine;

/// <summary>
/// Giới hạn TỔNG quân Human (phe Enemy) còn sống trên toàn bản đồ, dùng chung cho mọi
/// nguồn sinh quân (cổng <see cref="EnemySpawner"/>, tiếp viện <see cref="CrystalReinforcementSpawner"/>).
/// Mỗi nguồn gọi <see cref="HasGlobalCapacity"/> trước khi sinh và <see cref="Register"/> sau khi sinh;
/// limiter tự nghe <see cref="UnitHealth.Died"/> để trả chỗ trống (theo mẫu death-tracking của EnemySpawner).
///
/// Kiêm luôn ÁP LỰC THÍCH ỨNG (rubber-band): người chơi càng đông quân thì quái càng ra nhanh và
/// trần dân số càng cao - chống việc quái vừa ra đã bị quây giết sạch nên trận đấu hoá nhạt.
///
/// Là singleton scene tối giản theo mẫu <see cref="ResourceManager"/> - không có limiter trong scene
/// thì các nguồn sinh hoạt động như cũ (không giới hạn toàn cục, không rubber-band).
/// </summary>
public class EnemyPopulationLimiter : MonoBehaviour
{
    // Nhịp tính lại áp lực - đếm quân mỗi frame là thừa, theo mẫu nhịp thưa của Phase2Director.
    private const float PressureRecalcInterval = 0.5f;

    /// <summary>Mức giảm nhịp sinh quái tối đa. Quái ra nhanh gấp đôi đã là trần chịu đựng của
    /// người chơi, nên cap cứng ở đây dù có nuôi bao nhiêu quân đi nữa.</summary>
    public const float MaxIntervalReduction = 0.5f;

    public static EnemyPopulationLimiter Instance { get; private set; }

    [Tooltip("Tổng số quân Human còn sống tối đa trên toàn bản đồ (mọi cổng + mọi tinh thể cộng lại).")]
    [SerializeField]
    [Min(0)]
    private int maxAlivePopulation = 24;

    [Header("Áp lực thích ứng (rubber-band)")]
    [Tooltip("Số quân người chơi coi là bình thường - từ mức này trở xuống không tăng áp lực.")]
    [SerializeField]
    [Min(1)]
    private int baselinePlayerUnits = 6;

    [Tooltip("Cứ thêm chừng này quân người chơi thì lên một bậc áp lực.")]
    [SerializeField]
    [Min(1)]
    private int unitsPerPressureStep = 3;

    [Tooltip("Mỗi bậc áp lực cộng thêm chừng này vào trần dân số quái.")]
    [SerializeField]
    [Min(0)]
    private int bonusPopulationPerStep = 4;

    [Tooltip("Trần cộng thêm tối đa - chặn dân số quái phình vô hạn.")]
    [SerializeField]
    [Min(0)]
    private int maxBonusPopulation = 16;

    [Tooltip("Mỗi bậc áp lực giảm chừng này tỉ lệ nhịp sinh quái (0.1 = quái ra nhanh hơn 10%).")]
    [SerializeField]
    [Min(0f)]
    private float intervalReductionPerStep = 0.1f;

    private int alivePopulation;

    // Giá trị dẫn xuất từ số quân người chơi, tính lại theo nhịp thưa rồi cache cho các nguồn sinh đọc.
    private int bonusPopulation;
    private float intervalMultiplier = 1f;
    private float nextPressureRecalcTime;

    /// <summary>Còn chỗ trống trong giới hạn toàn cục không - không có limiter thì luôn còn.</summary>
    public static bool HasGlobalCapacity()
    {
        return Instance == null || Instance.alivePopulation < Instance.EffectiveMaxPopulation;
    }

    /// <summary>Hệ số nhân nhịp sinh quái (1 = như cấu hình, 0.5 = ra nhanh gấp đôi) - không có
    /// limiter thì trả 1 để nguồn sinh chạy đúng nhịp gốc.</summary>
    public static float SpawnIntervalMultiplier()
    {
        return Instance == null ? 1f : Instance.intervalMultiplier;
    }

    private int EffectiveMaxPopulation => maxAlivePopulation + bonusPopulation;

    /// <summary>Ghi nhận một quân vừa sinh và tự trừ lại khi nó chết.</summary>
    public void Register(GameObject unit)
    {
        alivePopulation++;

        UnitHealth health = unit.GetComponentInChildren<UnitHealth>();
        if (health != null)
        {
            health.Died += OnUnitDied;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Time.time < nextPressureRecalcTime)
        {
            return;
        }

        nextPressureRecalcTime = Time.time + PressureRecalcInterval;
        RecalculatePressure();
    }

    // Quân người chơi vượt mức bình thường bao nhiêu bậc thì quái được cộng ngần ấy áp lực.
    private void RecalculatePressure()
    {
        int steps = PressureSteps(CountPlayerUnits());

        bonusPopulation = Mathf.Min(steps * bonusPopulationPerStep, maxBonusPopulation);

        float reduction = Mathf.Min(steps * intervalReductionPerStep, MaxIntervalReduction);
        intervalMultiplier = 1f - reduction;
    }

    private int PressureSteps(int playerUnitCount)
    {
        int surplus = playerUnitCount - baselinePlayerUnits;
        return surplus <= 0 ? 0 : surplus / unitsPerPressureStep;
    }

    // Chỉ đếm QUÂN của người chơi: unit có Unit (quân cơ động) mới tính - avatar Demon King và
    // công trình cũng mang UnitFaction phe Player nhưng không phải quân nên phải loại ra.
    private static int CountPlayerUnits()
    {
        int count = 0;
        foreach (UnitFaction unit in UnitFaction.ActiveUnits)
        {
            bool isPlayerArmyUnit = unit != null
                && unit.Faction == FactionType.Player
                && unit.GetComponent<Unit>() != null;
            if (isPlayerArmyUnit)
            {
                count++;
            }
        }

        return count;
    }

    private void OnUnitDied(UnitHealth health)
    {
        health.Died -= OnUnitDied;
        alivePopulation = Mathf.Max(0, alivePopulation - 1);
    }
}
