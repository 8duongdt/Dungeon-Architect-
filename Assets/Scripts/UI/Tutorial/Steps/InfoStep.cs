/// <summary>
/// Bước chỉ để đọc: không canh thao tác nào, người chơi bấm Next để qua. Nhớ tick
/// <c>isInfoOnly</c> trên component để overlay hiện nút Next.
/// </summary>
public class InfoStep : TutorialStep
{
    protected override void StartWatching()
    {
        // Không canh gì - chuỗi cho qua bằng nút Next.
    }

    protected override void StopWatching()
    {
    }
}
