using System;
using UnityEngine;

/// <summary>
/// Quản lý hai tài nguyên cốt lõi: Vàng (Gold) và Mana. Không tự tăng - chỉ thay đổi khi công trình
/// khai thác (<see cref="ResourceProducer"/>) cộng vào, hoặc khi xây/nâng cấp tiêu hao.
///
/// Là singleton tối giản vì các công trình được sinh ra lúc runtime (không gán tham chiếu qua
/// Inspector được) cần truy cập tới nó. HUD thì đăng ký qua sự kiện, không cần singleton.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Giá trị khởi đầu")]
    [SerializeField] private int startGold = 500;
    [SerializeField] private int startMana = 0;

    // Cả Vàng lẫn Mana đều tăng KHÔNG giới hạn (không có trần).
    public int Gold { get; private set; }
    public int Mana { get; private set; }

    /// <summary>Bắn khi lượng Vàng đổi (giá trị mới).</summary>
    public event Action<int> GoldChanged;

    /// <summary>Bắn khi lượng Mana đổi (giá trị mới).</summary>
    public event Action<int> ManaChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Gold = Mathf.Max(0, startGold);
        Mana = Mathf.Max(0, startMana);
    }

    private void Start()
    {
        // Bắn một lần để HUD đồng bộ giá trị ban đầu.
        GoldChanged?.Invoke(Gold);
        ManaChanged?.Invoke(Mana);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    public void AddMana(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Mana += amount;
        ManaChanged?.Invoke(Mana);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        GoldChanged?.Invoke(Gold);
        return true;
    }

    public bool TrySpendMana(int amount)
    {
        if (amount < 0 || Mana < amount)
        {
            return false;
        }

        Mana -= amount;
        ManaChanged?.Invoke(Mana);
        return true;
    }

    /// <summary>Đủ tài nguyên để chi trả hay không (không trừ).</summary>
    public bool CanAfford(int goldCost, int manaCost)
    {
        return Gold >= goldCost && Mana >= manaCost;
    }
}
