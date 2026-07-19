using UnityEngine;

/// <summary>
/// Giới hạn TỔNG quân Human (phe Enemy) còn sống trên toàn bản đồ, dùng chung cho mọi
/// nguồn sinh quân (cổng <see cref="EnemySpawner"/>, tiếp viện <see cref="CrystalReinforcementSpawner"/>).
/// Mỗi nguồn gọi <see cref="HasGlobalCapacity"/> trước khi sinh và <see cref="Register"/> sau khi sinh;
/// limiter tự nghe <see cref="UnitHealth.Died"/> để trả chỗ trống (theo mẫu death-tracking của EnemySpawner).
///
/// Là singleton scene tối giản theo mẫu <see cref="ResourceManager"/> - không có limiter trong scene
/// thì các nguồn sinh hoạt động như cũ (không giới hạn toàn cục).
/// </summary>
public class EnemyPopulationLimiter : MonoBehaviour
{
    public static EnemyPopulationLimiter Instance { get; private set; }

    [Tooltip("Tổng số quân Human còn sống tối đa trên toàn bản đồ (mọi cổng + mọi tinh thể cộng lại).")]
    [SerializeField]
    [Min(0)]
    private int maxAlivePopulation = 24;

    private int alivePopulation;

    /// <summary>Còn chỗ trống trong giới hạn toàn cục không - không có limiter thì luôn còn.</summary>
    public static bool HasGlobalCapacity()
    {
        return Instance == null || Instance.alivePopulation < Instance.maxAlivePopulation;
    }

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

    private void OnUnitDied(UnitHealth health)
    {
        health.Died -= OnUnitDied;
        alivePopulation = Mathf.Max(0, alivePopulation - 1);
    }
}
