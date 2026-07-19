using Pathfinding;
using UnityEngine;

/// <summary>
/// Chân hành quân chiến dịch của một unit Human: implement <see cref="ICrystalSeeker"/> để
/// <see cref="IdleState"/> gọi khi unit rảnh (không thấy địch, không có lệnh tay). Xin mục tiêu từ
/// <see cref="HumanCampaignDirector"/>, nhờ A* (Seeker) tính đường rồi bám theo từng waypoint bằng
/// <see cref="UnitMovement.MoveTowards(Vector3)"/> - combat vẫn tự ngắt chiến dịch vì IdleState quét
/// địch TRƯỚC khi gọi seeker. Tới vùng ảnh hưởng của tinh thể thì dừng lại đồn trú (đứng gác,
/// CrystalCaptureZone sẽ chuyển cục sang đỏ sau vài giây có mặt).
/// </summary>
[RequireComponent(typeof(Seeker))]
public class CrystalCampaignAgent : MonoBehaviour, ICrystalSeeker
{
    [Tooltip("Chu kỳ (giây) tính lại đường - lưới an toàn khi bị va chạm đẩy lệch khỏi path.")]
    [SerializeField] private float repathSeconds = 2f;

    [Tooltip("Khoảng cách coi như đã qua một waypoint để chuyển sang waypoint kế.")]
    [SerializeField] private float waypointTolerance = 0.35f;

    [Tooltip("Hệ số nhân bán kính ảnh hưởng của tinh thể để coi là 'đã tới nơi' (nhỏ hơn 1 = vào sâu trong vùng).")]
    [SerializeField] private float arriveRadiusFactor = 0.7f;

    private Seeker seeker;
    private UnitMovement movement;
    private CrystalNode objective;
    private Path currentPath;
    private int waypointIndex;
    private float repathTimer;
    private bool waitingForPath;
    private bool isGarrisoned;

    // Mục tiêu "đi phá công trình người chơi" - ưu tiên hơn chiến dịch tinh thể khi được Director giao.
    // Tới gần công trình thì IdleState.FindVisibleTarget tự bắt mục tiêu -> Chase -> Attack phá hủy.
    private Transform raidTarget;
    private bool isRaiding;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        movement = GetComponent<UnitMovement>();
    }

    private void OnDisable()
    {
        HumanCampaignDirector.Instance?.RemoveAgent(this);
    }

    /// <summary>Director điều unit này sang mục tiêu khác (tách quân đi chiếm tiếp / kéo về phản công).</summary>
    public void AssignObjective(CrystalNode crystal)
    {
        HumanCampaignDirector.Instance?.RemoveAgent(this);
        objective = crystal;
        isGarrisoned = false;
        StopRaiding();
        ClearPath();
    }

    /// <summary>Director điều unit này đi PHÁ một công trình người chơi (sau khi chiếm được tinh thể).</summary>
    public void AssignRaid(Transform building)
    {
        if (building == null)
        {
            return;
        }

        HumanCampaignDirector.Instance?.RemoveAgent(this);
        raidTarget = building;
        isRaiding = true;
        objective = null;
        isGarrisoned = false;
        ClearPath();
    }

    public bool TrySeekNearestCrystal()
    {
        if (isRaiding)
        {
            // Công trình còn sống -> hành quân tới đó; combat tự tiếp quản khi vào tầm.
            if (raidTarget != null)
            {
                SeekPosition(raidTarget.position);
                return true;
            }

            // Công trình đã bị phá -> kết thúc raid, quay lại chiến dịch tinh thể ngay tick này.
            StopRaiding();
        }

        if (isGarrisoned)
        {
            return false;
        }

        if (NeedsNewObjective())
        {
            objective = HumanCampaignDirector.Instance != null
                ? HumanCampaignDirector.Instance.RequestObjective(transform.position)
                : null;
            ClearPath();
            if (objective == null)
            {
                return false;
            }
        }

        if (HasArrivedAtObjective())
        {
            EnterGarrison();
            return false;
        }

        SeekPosition(objective.transform.position);
        return true;
    }

    private void StopRaiding()
    {
        isRaiding = false;
        raidTarget = null;
    }

    // Bám đường A* tới một vị trí thế giới bất kỳ (dùng chung cho cả đi chiếm tinh thể lẫn đi phá công trình).
    private void SeekPosition(Vector3 destination)
    {
        TickRepath(destination);
        FollowPath(destination);
    }

    // Mục tiêu mất hiệu lực khi chưa có, đã bị hủy, hoặc mũi khác đã chiếm xong trước khi tới.
    private bool NeedsNewObjective()
    {
        return objective == null || objective.State == CrystalState.Captured;
    }

    private bool HasArrivedAtObjective()
    {
        float arriveDistance = objective.InfluenceRadius * arriveRadiusFactor;
        return Vector2.Distance(transform.position, objective.transform.position) <= arriveDistance;
    }

    private void EnterGarrison()
    {
        movement.Stop();
        isGarrisoned = true;
        ClearPath();
        HumanCampaignDirector.Instance?.EnterGarrison(objective, this);
    }

    private void TickRepath(Vector3 destination)
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f || waitingForPath)
        {
            return;
        }

        repathTimer = repathSeconds;
        waitingForPath = true;
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

    private void FollowPath(Vector3 destination)
    {
        if (currentPath == null)
        {
            // Chưa có đường (đang chờ A* tính) - đứng yên thay vì lao thẳng xuyên tường.
            movement.Stop();
            return;
        }

        var waypoints = currentPath.vectorPath;
        while (waypointIndex < waypoints.Count
            && Vector2.Distance(transform.position, waypoints[waypointIndex]) <= waypointTolerance)
        {
            waypointIndex++;
        }

        Vector3 moveTarget = waypointIndex < waypoints.Count
            ? waypoints[waypointIndex]
            : destination;
        movement.MoveTowards(moveTarget, 0f);
    }

    private void ClearPath()
    {
        currentPath = null;
        waypointIndex = 0;
        repathTimer = 0f;
    }
}
