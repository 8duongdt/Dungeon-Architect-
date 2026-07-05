/// <summary>
/// Điểm nối giữa máy trạng thái AI (<see cref="IdleState"/>) và cách thực sự di chuyển tới tinh
/// thể (A* Pathfinding Project, Phase 7 - chưa import vào project). IdleState chỉ cần biết giao
/// diện này nên vẫn biên dịch được ngay cả khi chưa có A* Pathfinding.
/// </summary>
public interface ICrystalSeeker
{
    /// <summary>Thử tìm và tiếp tục di chuyển tới tinh thể Active gần nhất chưa bị chiếm.
    /// true nếu đang theo đuổi một tinh thể (IdleState khỏi cần patrol/stop thêm trong tick này);
    /// false nếu không còn tinh thể nào để tìm (IdleState patrol/stop như bình thường).</summary>
    bool TrySeekNearestCrystal();
}
