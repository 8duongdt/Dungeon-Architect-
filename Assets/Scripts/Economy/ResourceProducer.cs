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

    public ResourceKind Kind => kind;

    /// <summary>Sản lượng quy đổi mỗi phút (đã tính hệ số nâng cấp) - dùng cho Cửa sổ Trạng thái.</summary>
    public float ProductionPerMinute => cycleSeconds > 0f ? amountPerCycle * outputMultiplier / cycleSeconds * 60f : 0f;

    public string TypeLabel => kind == ResourceKind.Gold ? "Gold Mine" : "Mana Well";

    public IEnumerable<string> GetStatLines()
    {
        string resourceName = kind == ResourceKind.Gold ? "Gold" : "Mana";
        yield return $"{resourceName} Generation: {ProductionPerMinute:0.#} / min";
    }

    private void Update()
    {
        if (ResourceManager.Instance == null || cycleSeconds <= 0f)
        {
            return;
        }

        cycleTimer += Time.deltaTime;
        if (cycleTimer < cycleSeconds)
        {
            return;
        }

        cycleTimer -= cycleSeconds;
        ProduceOneCycle();
    }

    private void ProduceOneCycle()
    {
        int produced = Mathf.RoundToInt(amountPerCycle * outputMultiplier);
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
