using UnityEngine;

/// <summary>
/// Nối <see cref="ResourceManager"/> với hai ô hiển thị Vàng và Mana trên HUD. Đăng ký sự kiện và
/// đẩy giá trị vào view; không tự giữ trạng thái.
/// </summary>
public class ResourceHudBinder : MonoBehaviour
{
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private ResourceCounterView goldView;
    [SerializeField] private ResourceCounterView manaView;

    private void Awake()
    {
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;
        }
    }

    private void OnEnable()
    {
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.Instance;
        }
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.GoldChanged += OnGoldChanged;
        resourceManager.ManaChanged += OnManaChanged;

        // Đồng bộ ngay giá trị hiện có (phòng khi sự kiện Start đã bắn trước lúc đăng ký).
        OnGoldChanged(resourceManager.Gold);
        OnManaChanged(resourceManager.Mana);
    }

    private void OnDisable()
    {
        if (resourceManager == null)
        {
            return;
        }

        resourceManager.GoldChanged -= OnGoldChanged;
        resourceManager.ManaChanged -= OnManaChanged;
    }

    private void OnGoldChanged(int gold)
    {
        if (goldView != null)
        {
            goldView.SetAmount(gold);
        }
    }

    private void OnManaChanged(int mana)
    {
        if (manaView != null)
        {
            manaView.SetAmount(mana);
        }
    }
}
