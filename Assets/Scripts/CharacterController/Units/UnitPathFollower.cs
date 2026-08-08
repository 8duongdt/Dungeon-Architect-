using Pathfinding;
using UnityEngine;

/// <summary>
/// Dịch vụ bám đường A* dùng chung cho mọi nơi cần lái unit tới một đích (ChaseState đuổi đánh,
/// Unit.MoveTo lệnh tay, CrystalCampaignAgent hành quân). CHỈ trả lời "đi đâu tiếp theo" qua
/// <see cref="GetSteeringPoint"/> - không tự điều khiển Rigidbody2D/animation, bên gọi vẫn dùng
/// UnitMovement/Unit của riêng mình để giữ nguyên vật lý/hoạt ảnh/hiệu ứng tốc độ đã có.
/// </summary>
[RequireComponent(typeof(Seeker))]
public class UnitPathFollower : MonoBehaviour
{
    /// <summary>Điểm cần lái tới ngay bây giờ, và cho biết đó là waypoint giữa đường hay đích cuối.</summary>
    public readonly struct SteeringResult
    {
        public readonly Vector3 Point;
        public readonly bool IsFinalDestination;

        public SteeringResult(Vector3 point, bool isFinalDestination)
        {
            Point = point;
            IsFinalDestination = isFinalDestination;
        }
    }

    [Tooltip("Chu kỳ (giây) tính lại đường - lưới an toàn khi bị va chạm đẩy lệch khỏi path.")]
    [SerializeField] private float repathSeconds = 0.4f;

    [Tooltip("Khoảng cách coi như đã qua một waypoint để chuyển sang waypoint kế.")]
    [SerializeField] private float waypointTolerance = 0.35f;

    [Tooltip("Đích lệch quá khoảng này so với lần tính đường trước thì tính lại ngay, không chờ hết chu kỳ.")]
    [SerializeField] private float destinationJumpThreshold = 1f;

    private Seeker seeker;
    private Path currentPath;
    private int waypointIndex;
    private float repathTimer;
    private bool waitingForPath;
    private bool hasRequestedPath;
    private Vector3 lastRequestedDestination;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
    }

    /// <summary>Điểm kế tiếp trên đường A* tới destination - gọi mỗi tick khi cần bám đường.</summary>
    public SteeringResult GetSteeringPoint(Vector3 destination)
    {
        TickRepath(destination);
        return NextWaypoint(destination);
    }

    private void TickRepath(Vector3 destination)
    {
        bool destinationJumped = hasRequestedPath
            && Vector2.Distance(destination, lastRequestedDestination) >= destinationJumpThreshold;

        repathTimer -= Time.deltaTime;
        if (!destinationJumped && (repathTimer > 0f || waitingForPath))
        {
            return;
        }

        repathTimer = repathSeconds;
        waitingForPath = true;
        hasRequestedPath = true;
        lastRequestedDestination = destination;
        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    private void OnPathComplete(Path path)
    {
        waitingForPath = false;
        if (path.error)
        {
            return;
        }

        currentPath = path;
        waypointIndex = 0;
    }

    private SteeringResult NextWaypoint(Vector3 destination)
    {
        if (currentPath == null)
        {
            // Chưa có đường (đang chờ A* tính lần đầu) - đứng yên tại chỗ thay vì lao thẳng xuyên tường.
            return new SteeringResult(transform.position, false);
        }

        var waypoints = currentPath.vectorPath;
        while (waypointIndex < waypoints.Count
            && Vector2.Distance(transform.position, waypoints[waypointIndex]) <= waypointTolerance)
        {
            waypointIndex++;
        }

        bool onFinalWaypoint = waypointIndex >= waypoints.Count;
        Vector3 point = onFinalWaypoint ? destination : waypoints[waypointIndex];
        return new SteeringResult(point, onFinalWaypoint);
    }
}
