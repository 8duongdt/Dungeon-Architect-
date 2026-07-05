using System;
using UnityEngine;

/// <summary>
/// Một cục tinh thể tài nguyên trên map (Gold/Mana/Progress). Tự đăng ký vào
/// <see cref="CrystalNodeRegistry"/> để các hệ thống khác (giới hạn xây dựng, AI chiếm đóng...)
/// tra cứu mà không cần FindObjectsByType. Hình ảnh đổi theo <see cref="CrystalState"/> qua
/// <see cref="CrystalTypeConfigSO"/>: mỗi trạng thái một sprite riêng (xám/nguyên bản/đỏ);
/// trạng thái nào chưa có sprite thì rơi về tint màu như thiết kế placeholder cũ.
/// </summary>
[DisallowMultipleComponent]
public class CrystalNode : MonoBehaviour, IHasInfluenceRadius
{
    public enum SizeTier
    {
        Small,
        Large
    }

    [Tooltip("Cấu hình sprite/màu/bán kính theo loại tinh thể (Gold/Mana/Progress) - gán sẵn trên prefab.")]
    [SerializeField] private CrystalTypeConfigSO config;

    [Tooltip("Nhỏ hay to - quyết định dùng smallRadius hay largeRadius của config.")]
    [SerializeField] private SizeTier sizeTier = SizeTier.Small;

    [Tooltip("SpriteRenderer thân tinh thể - sprite đổi theo trạng thái qua config.")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    [Tooltip("Collider trigger LỚN phủ hết InfluenceRadius - dùng chung cho mọi component cần biết " +
        "ai đang đứng trong vùng ảnh hưởng (ProgressCrystalChannel, CrystalCaptureZone...). " +
        "Tách biệt với collider nhỏ để chuột phải chọn tinh thể.")]
    [SerializeField] private CircleCollider2D influenceCollider;

    private CrystalState state = CrystalState.Inactive;

    public CrystalState State => state;
    public CrystalType Type => config.type;
    public float InfluenceRadius => sizeTier == SizeTier.Large ? config.largeRadius : config.smallRadius;

    public float Radius => InfluenceRadius;
    public Color RadiusColor => config.ColorFor(state);
    public Transform Origin => transform;

    /// <summary>Bắn khi trạng thái đổi - để capture zone/UI khác phản ứng theo.</summary>
    public event Action<CrystalNode> StateChanged;

    /// <summary>Bắn khi BẤT KỲ tinh thể nào đổi trạng thái - để hệ thống điều phối (chiến dịch
    /// Human) nghe một chỗ thay vì subscribe từng node theo vòng đời đăng ký registry.</summary>
    public static event Action<CrystalNode> AnyStateChanged;

    private void Awake()
    {
        RefreshInfluenceCollider();
    }

    private void OnEnable()
    {
        CrystalNodeRegistry.Register(this);
    }

    private void OnDisable()
    {
        CrystalNodeRegistry.Unregister(this);
    }

    /// <summary>Gán kích cỡ ngay sau khi Instantiate - CrystalScatterSpawner gọi mỗi lần rải.
    /// Loại/config đã nằm sẵn trên prefab từng loại nên không cần truyền vào nữa.</summary>
    public void Configure(SizeTier tier)
    {
        sizeTier = tier;
        RefreshInfluenceCollider();
        SetState(CrystalState.Inactive);
    }

    public void Activate()
    {
        SetState(CrystalState.Active);
    }

    public void Deactivate()
    {
        SetState(CrystalState.Inactive);
    }

    public void Capture()
    {
        SetState(CrystalState.Captured);
    }

    public void Reactivate()
    {
        SetState(CrystalState.Active);
    }

    private void SetState(CrystalState newState)
    {
        state = newState;
        RefreshVisual();
        StateChanged?.Invoke(this);
        AnyStateChanged?.Invoke(this);
    }

    // Trạng thái có sprite riêng thì hiển thị nguyên màu art (tint trắng); chưa có sprite
    // thì giữ cơ chế tint màu cũ để prefab placeholder vẫn phân biệt được trạng thái.
    private void RefreshVisual()
    {
        if (bodyRenderer == null || config == null)
        {
            return;
        }

        Sprite stateSprite = config.SpriteFor(state);
        if (stateSprite != null)
        {
            bodyRenderer.sprite = stateSprite;
            bodyRenderer.color = Color.white;
        }
        else
        {
            bodyRenderer.color = config.ColorFor(state);
        }
    }

    private void RefreshInfluenceCollider()
    {
        if (influenceCollider == null || config == null)
        {
            return;
        }

        influenceCollider.isTrigger = true;
        influenceCollider.radius = InfluenceRadius;
    }

    private void OnValidate()
    {
        RefreshVisual();
        RefreshInfluenceCollider();
    }
}
