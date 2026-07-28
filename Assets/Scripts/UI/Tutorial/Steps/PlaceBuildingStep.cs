using UnityEngine;

/// <summary>
/// Bước hoàn thành khi người chơi đặt thành công một công trình. Có thể yêu cầu đúng một loại công
/// trình qua <see cref="requiredType"/> (vd chỉ tính khi xây Gold Mine hoặc Barracks); để trống thì
/// bất kỳ công trình nào cũng tính.
/// </summary>
public class PlaceBuildingStep : TutorialStep
{
    [Tooltip("Chỉ tính khi đặt đúng loại công trình này. Trống = mọi công trình đều tính.")]
    [SerializeField] private PlacedObjectTypeSO requiredType;

    private GridBuildingSystem buildingSystem;

    // Máy khai thác cần tinh thể cùng loại -> trỏ mũi tên vào tinh thể loại đó gần nhất để dẫn đường
    // (bất kể trạng thái: nếu chưa Active người chơi tới kích hoạt rồi xây, đúng như câu chỉ dẫn).
    // Công trình không cần tinh thể (vd doanh trại) thì dùng mục tiêu gán sẵn (nếu có).
    public override Transform ResolveHighlightTarget()
    {
        CrystalType? crystalType = CrystalBuildRestriction.GetRequiredCrystalType(requiredType);
        if (crystalType == null)
        {
            return base.ResolveHighlightTarget();
        }

        return FindNearestCrystalTransform(crystalType.Value, null);
    }

    protected override void StartWatching()
    {
        buildingSystem = FindFirstObjectByType<GridBuildingSystem>();
        if (buildingSystem != null)
        {
            buildingSystem.Placed += HandlePlaced;
        }
    }

    protected override void StopWatching()
    {
        if (buildingSystem != null)
        {
            buildingSystem.Placed -= HandlePlaced;
        }
    }

    private void HandlePlaced(PlacedObject placedObject)
    {
        if (placedObject == null)
        {
            return;
        }

        if (requiredType != null && placedObject.Type != requiredType)
        {
            return;
        }

        NotifyComplete();
    }
}
