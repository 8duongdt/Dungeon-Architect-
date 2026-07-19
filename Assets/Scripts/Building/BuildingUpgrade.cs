using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống nâng cấp tại chỗ cho công trình. Giữ cấp hiện tại và bảng các cấp; mỗi lần nâng sẽ trừ
/// Vàng/Mana rồi áp hiệu ứng (trần HP, hệ số sản xuất, tốc độ/giới hạn/mở khóa huấn luyện) lên các
/// thành phần anh em trên cùng GameObject, đồng thời thay diện mạo (sprite + animator) sang art của
/// cấp mới. Cấp 1 là cấp gốc (chi phí 0). Cửa sổ Trạng thái đọc thông tin cấp kế tiếp để hiển thị
/// nút [NÂNG CẤP].
/// </summary>
public class BuildingUpgrade : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeLevel
    {
        [Tooltip("Chi phí Vàng để đạt cấp này (cấp 1 = 0).")]
        [Min(0)] public int goldCost;

        [Tooltip("Chi phí Mana để đạt cấp này (cấp 1 = 0).")]
        [Min(0)] public int manaCost;

        [Tooltip("Trần độ bền (HP) ở cấp này.")]
        [Min(1f)] public float maxDurability = 1000f;

        [Tooltip("Hệ số nhân sản lượng cho ResourceProducer ở cấp này.")]
        [Min(0f)] public float productionMultiplier = 1f;

        [Tooltip("Hệ số nhân thời gian hồi triệu hồi (nhỏ hơn 1 = nhanh hơn).")]
        [Min(0.01f)] public float trainIntervalMultiplier = 1f;

        [Tooltip("Cộng thêm số lính tối đa cho trại huấn luyện ở cấp này.")]
        [Min(0)] public int maxUnitsBonus;

        [Tooltip("Số loại lính mở khóa ở cấp này (đếm từ đầu danh sách trại; 0 = mở khóa tất cả).")]
        [Min(0)] public int unlockedUnitCount;

        [Tooltip("Sprite diện mạo của cấp này - bỏ trống thì giữ nguyên hình hiện tại.")]
        public Sprite sprite;

        [Tooltip("Animator controller diện mạo của cấp này - bỏ trống thì giữ nguyên animation hiện tại.")]
        public RuntimeAnimatorController animatorController;

        [Tooltip("Mô tả lợi ích hiển thị khi xem trước cấp này.")]
        [TextArea] public string benefitDescription;
    }

    [SerializeField] private List<UpgradeLevel> levels = new List<UpgradeLevel>();
    [SerializeField] [Min(1)] private int currentLevel = 1;

    private BuildingDurability durability;
    private UnitHealth combatHealth;
    private ResourceProducer producer;
    private UnitTrainingBuilding training;
    private SpriteRenderer bodyRenderer;
    private Animator bodyAnimator;

    public int CurrentLevel => currentLevel;
    public bool HasNextLevel => currentLevel < levels.Count;
    public int NextGoldCost => HasNextLevel ? levels[currentLevel].goldCost : 0;
    public int NextManaCost => HasNextLevel ? levels[currentLevel].manaCost : 0;
    public string NextBenefitDescription => HasNextLevel ? levels[currentLevel].benefitDescription : string.Empty;

    private void Awake()
    {
        durability = GetComponent<BuildingDurability>();
        combatHealth = GetComponent<UnitHealth>();
        producer = GetComponent<ResourceProducer>();
        training = GetComponent<UnitTrainingBuilding>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        bodyAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        ApplyLevelEffects(currentLevel);
    }

    public bool CanAffordUpgrade()
    {
        if (!HasNextLevel)
        {
            return false;
        }

        ResourceManager resources = ResourceManager.Instance;
        return resources == null || resources.CanAfford(NextGoldCost, NextManaCost);
    }

    public bool TryUpgrade()
    {
        if (!HasNextLevel || !CanAffordUpgrade())
        {
            return false;
        }

        SpendNextCost();
        currentLevel++;
        ApplyLevelEffects(currentLevel);
        return true;
    }

    private void SpendNextCost()
    {
        ResourceManager resources = ResourceManager.Instance;
        if (resources == null)
        {
            return;
        }

        resources.TrySpendGold(NextGoldCost);
        resources.TrySpendMana(NextManaCost);
    }

    // Áp các chỉ số của cấp <paramref name="level"/> (1-based) lên các thành phần công trình.
    private void ApplyLevelEffects(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        if (levels.Count == 0)
        {
            return;
        }

        UpgradeLevel data = levels[index];
        // Máu THẬT (UnitHealth - thứ quyết định sống chết trong chiến đấu) phải lên trần mới và hồi
        // đầy cùng lúc với thanh độ bền - nâng cấp đồng thời là sửa chữa, hai pool không được lệch.
        if (combatHealth != null)
        {
            combatHealth.ApplyBaseStats(data.maxDurability, combatHealth.Defense);
        }
        if (durability != null)
        {
            durability.SetMax(data.maxDurability, refillToFull: true);
        }
        if (producer != null)
        {
            producer.SetOutputMultiplier(data.productionMultiplier);
        }
        if (training != null)
        {
            training.ApplyUpgrade(data.trainIntervalMultiplier, data.maxUnitsBonus, data.unlockedUnitCount);
        }
        ApplyLevelVisual(data);
    }

    // Thay diện mạo sang art của cấp mới - đổi controller trước rồi mới đổi sprite để frame đầu
    // của animation cấp mới không bị sprite cũ đè trong một khung hình.
    private void ApplyLevelVisual(UpgradeLevel data)
    {
        if (bodyAnimator != null && data.animatorController != null)
        {
            bodyAnimator.runtimeAnimatorController = data.animatorController;
        }
        if (bodyRenderer != null && data.sprite != null)
        {
            bodyRenderer.sprite = data.sprite;
        }
    }
}
