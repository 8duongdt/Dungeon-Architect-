/// <summary>
/// Bước (dùng ở Lobby) hoàn thành khi người chơi tiêu ít nhất một điểm - mở/nâng node ở cây skill
/// hoặc cây công trình. Không có event nên so tổng điểm (skill + công trình) với mốc lúc bắt đầu.
/// </summary>
public class SpendPointStep : TutorialStep
{
    private int baselineTotalPoints;

    protected override void StartWatching()
    {
        baselineTotalPoints = CurrentTotalPoints();
    }

    protected override void StopWatching()
    {
    }

    private void Update()
    {
        if (!IsWatching)
        {
            return;
        }

        if (CurrentTotalPoints() < baselineTotalPoints)
        {
            NotifyComplete();
        }
    }

    private static int CurrentTotalPoints()
    {
        return PlayerProgression.SkillPoints + PlayerProgression.BuildingPoints;
    }
}
