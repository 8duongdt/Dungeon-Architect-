using UnityEngine;

/// <summary>
/// Bước hoàn thành khi một tinh thể chuyển sang <see cref="CrystalState.Active"/> (người chơi đứng
/// đủ lâu trong bán kính). Có thể yêu cầu đúng một loại tinh thể (vd chỉ Gold) qua
/// <see cref="requireSpecificType"/>.
/// </summary>
public class ActivateCrystalStep : TutorialStep
{
    [Tooltip("Chỉ tính khi kích hoạt đúng loại tinh thể bên dưới.")]
    [SerializeField] private bool requireSpecificType;

    [SerializeField] private CrystalType requiredType = CrystalType.Gold;

    // Trỏ mũi tên vào tinh thể còn Inactive gần nhất (đúng loại nếu có yêu cầu) để người chơi tới kích hoạt.
    public override Transform ResolveHighlightTarget()
    {
        CrystalType? type = requireSpecificType ? requiredType : (CrystalType?)null;
        return FindNearestCrystalTransform(type, CrystalState.Inactive);
    }

    protected override void StartWatching()
    {
        CrystalNode.AnyStateChanged += HandleCrystalStateChanged;
    }

    protected override void StopWatching()
    {
        CrystalNode.AnyStateChanged -= HandleCrystalStateChanged;
    }

    private void HandleCrystalStateChanged(CrystalNode crystal)
    {
        if (crystal == null || crystal.State != CrystalState.Active)
        {
            return;
        }

        if (requireSpecificType && crystal.Type != requiredType)
        {
            return;
        }

        NotifyComplete();
    }
}
