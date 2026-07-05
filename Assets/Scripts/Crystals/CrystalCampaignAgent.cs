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
        ClearPath();
    }

    public bool TrySeekNearestCrystal()
    {
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

        TickRepath();
        FollowPath();
        return true;
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

    private void TickRepath()
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f || waitingForPath)
        {
            return;
        }

        repathTimer = repathSeconds;
        waitingForPath = true;
        seeker.StartPath(transform.position, objective.transform.position, OnPathComplete);
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

    private void FollowPath()
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
            : objective.transform.position;
        movement.MoveTowards(moveTarget, 0f);
    }

    private void ClearPath()
    {
        currentPath = null;
        waypointIndex = 0;
        repathTimer = 0f;
    }
}
