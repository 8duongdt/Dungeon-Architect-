using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn lên công trình khai thác (máy đào ra Vàng, lò ma thuật ra Mana). Cứ mỗi chu kỳ
/// <see cref="cycleSeconds"/> giây thì cộng <see cref="amountPerCycle"/> tài nguyên vào
/// <see cref="ResourceManager"/>. Null-safe nếu scene chưa có ResourceManager.
/// </summary>
public class ResourceProducer : MonoBehaviour, IConstructInfo
{
    public enum ResourceKind { Gold, Mana }

    [SerializeField] private ResourceKind kind = ResourceKind.Gold;
    [SerializeField] private int amountPerCycle = 10;
    [SerializeField] private float cycleSeconds = 2f;

    // Hệ số nhân sản lượng (mặc định 1). Điểm mở rộng cho việc nâng cấp công trình bằng Mana.
    private float outputMultiplier = 1f;
    private float cycleTimer;

    // Hệ số từ cây công trình ở Lobby (bền vững) - stack với outputMultiplier (nâng trong trận).
    // Cache một lần lúc spawn vì điểm chỉ tiêu được ở Lobby, không đổi giữa trận.
    private float progressionOutputMultiplier = 1f;
    private float progressionCycleMultiplier = 1f;

    // Tinh thể cùng loại mà công trình này đứng trong bán kính - null nếu không đứng gần tinh thể
    // nào (không nên xảy ra qua luồng xây bình thường vì CrystalBuildRestriction đã chặn từ trước).
    private CrystalNode ownerCrystal;

    public ResourceKind Kind => kind;

    /// <summary>Sản lượng quy đổi mỗi phút (đã tính mọi hệ số nâng cấp) - dùng cho Cửa sổ Trạng thái.</summary>
    public float ProductionPerMinute => EffectiveCycleSeconds > 0f
        ? amountPerCycle * outputMultiplier * progressionOutputMultiplier / EffectiveCycleSeconds * 60f
        : 0f;

    // Chu kỳ thực tế sau khi áp hệ số tăng tốc từ cây công trình.
    private float EffectiveCycleSeconds => cycleSeconds * progressionCycleMultiplier;

    public string TypeLabel => kind == ResourceKind.Gold ? "Gold Mine" : "Mana Well";

    public IEnumerable<string> GetStatLines()
    {
        string resourceName = kind == ResourceKind.Gold ? "Gold" : "Mana";
        yield return $"{resourceName} Generation: {ProductionPerMinute:0.#} / min";
    }

    private void Awake()
    {
        ResolveOwnerCrystal();
        progressionOutputMultiplier = BuildingProgressionEffects.GetProductionMultiplier(kind);
        progressionCycleMultiplier = BuildingProgressionEffects.GetCycleTimeMultiplier();
    }

    // Tìm tinh thể cùng loại (Gold/Mana) mà vị trí công trình nằm trong bán kính ảnh hưởng.
    // ƯU TIÊN tinh thể đang Active: khi hai tinh thể cùng loại chồng vùng, phải bind vào đúng cục
    // đã thỏa CrystalBuildRestriction lúc xây chứ không phải cục Inactive/Captured tình cờ gần hơn.
    private void ResolveOwnerCrystal()
    {
        CrystalType matchingType = kind == ResourceKind.Gold ? CrystalType.Gold : CrystalType.Mana;
        CrystalNode fallback = null;

        foreach (CrystalNode node in CrystalNodeRegistry.All)
        {
            bool isInRange = node != null
                && node.Type == matchingType
                && Vector2.Distance(node.transform.position, transform.position) <= node.InfluenceRadius;
            if (!isInRange)
            {
                continue;
            }

            if (node.State == CrystalState.Active)
            {
                ownerCrystal = node;
                return;
            }

            if (fallback == null)
            {
                fallback = node;
            }
        }

        ownerCrystal = fallback;
    }

    private void Update()
    {
        if (ResourceManager.Instance == null || cycleSeconds <= 0f)
        {
            return;
        }

        // Owner mất (bị destroy) hoặc không còn Active -> thử bind lại (tinh thể có thể vừa được
        // tái kích hoạt hoặc một cục Active khác chồng vùng). KHÔNG có owner Active thì ngừng sản
        // xuất - mỏ không được phép chạy "chui" ngoài vùng tinh thể sống.
        if (ownerCrystal == null || ownerCrystal.State != CrystalState.Active)
        {
            ResolveOwnerCrystal();
        }

        if (ownerCrystal == null || ownerCrystal.State != CrystalState.Active)
        {
            return;
        }

        cycleTimer += Time.deltaTime;
        if (cycleTimer < EffectiveCycleSeconds)
        {
            return;
        }

        cycleTimer -= EffectiveCycleSeconds;
        ProduceOneCycle();
    }

    private void ProduceOneCycle()
    {
        int produced = Mathf.RoundToInt(amountPerCycle * outputMultiplier * progressionOutputMultiplier);
        if (produced <= 0)
        {
            return;
        }

        if (kind == ResourceKind.Gold)
        {
            ResourceManager.Instance.AddGold(produced);
        }
        else
        {
            ResourceManager.Instance.AddMana(produced);
        }
    }

    /// <summary>Đặt hệ số nhân sản lượng (dùng khi nâng cấp công trình).</summary>
    public void SetOutputMultiplier(float multiplier)
    {
        outputMultiplier = Mathf.Max(0f, multiplier);
    }
}
