/// <summary>Bước hoàn thành khi người chơi ra lệnh di chuyển (chuột phải) cho các unit đang chọn.</summary>
public class MoveOrderStep : TutorialStep
{
    private UnitController controller;

    protected override void StartWatching()
    {
        controller = FindFirstObjectByType<UnitController>();
        if (controller != null)
        {
            controller.MoveOrderIssued += HandleMoveOrderIssued;
        }
    }

    protected override void StopWatching()
    {
        if (controller != null)
        {
            controller.MoveOrderIssued -= HandleMoveOrderIssued;
        }
    }

    private void HandleMoveOrderIssued()
    {
        NotifyComplete();
    }
}
