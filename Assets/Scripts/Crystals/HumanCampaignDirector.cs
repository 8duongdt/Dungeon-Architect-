using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ não chiến dịch của phe Human: cấp mục tiêu "tinh thể chưa-bị-chiếm gần nhất" cho từng
/// <see cref="CrystalCampaignAgent"/>, quản lý đồn trú theo tinh thể đã chiếm, hẹn giờ ngẫu nhiên
/// 60-120 giây rồi tách NỬA đồn trú đi chiếm cục kế tiếp, và khi người chơi tái chiếm một cục thì
/// điều toàn bộ đồn trú ở cục-đỏ gần nhất kéo về phản công. Singleton scene theo mẫu
/// <see cref="ResourceManager"/>; nghe <see cref="CrystalNode.AnyStateChanged"/> một chỗ
/// thay vì subscribe từng node.
/// </summary>
public class HumanCampaignDirector : MonoBehaviour
{
    public static HumanCampaignDirector Instance { get; private set; }

    [Tooltip("Khoảng thời gian (giây) tối thiểu đồn trú giữ vị trí trước khi tách nửa quân đi chiếm tiếp.")]
    [SerializeField] private float minAdvanceSeconds = 60f;

    [Tooltip("Khoảng thời gian (giây) tối đa đồn trú giữ vị trí trước khi tách nửa quân đi chiếm tiếp.")]
    [SerializeField] private float maxAdvanceSeconds = 120f;

    [Tooltip("Bán kính (đơn vị) quanh tinh thể vừa chiếm: có công trình người chơi trong tầm này thì ưu tiên phái quân đi phá.")]
    [SerializeField] private float buildingRaidRadius = 40f;

    [Tooltip("Xác suất (0-1) một đợt tách quân đi PHÁ CÔNG TRÌNH thay vì đi chiếm tinh thể kế tiếp, kể cả khi công trình ở xa.")]
    [Range(0f, 1f)]
    [SerializeField] private float buildingRaidChance = 0.5f;

    private class GarrisonRecord
    {
        public CrystalNode Crystal;
        public readonly List<CrystalCampaignAgent> Members = new List<CrystalCampaignAgent>();
        public float AdvanceTimer;
    }

    private readonly List<GarrisonRecord> garrisons = new List<GarrisonRecord>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        CrystalNode.AnyStateChanged += HandleCrystalStateChanged;
    }

    private void OnDisable()
    {
        CrystalNode.AnyStateChanged -= HandleCrystalStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        foreach (GarrisonRecord garrison in garrisons)
        {
            // Dọn agent chết ở MỌI record (kể cả record giữ tinh thể Active chưa chiếm) - TickGarrison
            // chỉ prune khi đang giữ Captured, nên nếu không dọn ở đây member null tích lũy theo trận.
            garrison.Members.RemoveAll(member => member == null);
            TickGarrison(garrison);
        }
        // Bỏ record của tinh thể đã hủy HOẶC record rỗng không còn giữ Captured (dọn rác chiến dịch).
        garrisons.RemoveAll(record => record.Crystal == null
            || (record.Members.Count == 0 && record.Crystal.State != CrystalState.Captured));
    }

    /// <summary>Tinh thể chưa-bị-chiếm gần vị trí này nhất - mục tiêu hành quân kế tiếp; null nếu hết.</summary>
    public CrystalNode RequestObjective(Vector3 fromPosition)
    {
        CrystalNode nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (CrystalNode node in CrystalNodeRegistry.All)
        {
            if (node == null || node.State == CrystalState.Captured)
            {
                continue;
            }

            float sqr = ((Vector2)node.transform.position - (Vector2)fromPosition).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = node;
            }
        }
        return nearest;
    }

    /// <summary>Agent tới nơi và vào đồn trú tại tinh thể này (kể cả khi cục chưa kịp chuyển đỏ).</summary>
    public void EnterGarrison(CrystalNode crystal, CrystalCampaignAgent agent)
    {
        if (crystal == null || agent == null)
        {
            return;
        }

        RemoveAgent(agent);
        GarrisonRecord record = FindOrCreateRecord(crystal);
        record.Members.Add(agent);
    }

    /// <summary>Gỡ agent khỏi mọi đồn trú (chết/bị điều đi nơi khác/disable).</summary>
    public void RemoveAgent(CrystalCampaignAgent agent)
    {
        foreach (GarrisonRecord garrison in garrisons)
        {
            garrison.Members.Remove(agent);
        }
    }

    private void HandleCrystalStateChanged(CrystalNode node)
    {
        if (node.State == CrystalState.Captured)
        {
            GarrisonRecord record = FindOrCreateRecord(node);
            record.AdvanceTimer = RollAdvanceDelay();
            DispatchInitialRaid(record);
            return;
        }

        // Cục đỏ vừa tuột khỏi tay Human (người chơi quét sạch đồn trú) -> phản công.
        GarrisonRecord lostRecord = FindRecord(node);
        if (lostRecord != null)
        {
            garrisons.Remove(lostRecord);
            OrderCounterattack(node);
        }
    }

    // Điều TOÀN BỘ đồn trú ở tinh thể-đỏ gần cục vừa mất nhất kéo về tái chiếm.
    private void OrderCounterattack(CrystalNode lostCrystal)
    {
        GarrisonRecord responder = FindNearestCapturedGarrison(lostCrystal.transform.position);
        if (responder == null)
        {
            return;
        }

        // Duyệt trên bản sao vì AssignObjective sẽ gỡ agent khỏi danh sách gốc.
        foreach (CrystalCampaignAgent member in responder.Members.ToArray())
        {
            if (member != null)
            {
                member.AssignObjective(lostCrystal);
            }
        }
    }

    private void TickGarrison(GarrisonRecord garrison)
    {
        bool isHolding = garrison.Crystal != null && garrison.Crystal.State == CrystalState.Captured;
        if (!isHolding)
        {
            return;
        }

        garrison.Members.RemoveAll(member => member == null);
        garrison.AdvanceTimer -= Time.deltaTime;
        if (garrison.AdvanceTimer > 0f)
        {
            return;
        }

        garrison.AdvanceTimer = RollAdvanceDelay();
        AdvanceHalfGarrison(garrison);
    }

    // Tách nửa quân (làm tròn xuống) đi làm nhiệm vụ kế tiếp - phá công trình HOẶC chiếm tinh thể -
    // nửa còn lại giữ vị trí.
    private void AdvanceHalfGarrison(GarrisonRecord garrison)
    {
        garrison.Members.RemoveAll(member => member == null);
        int advancingCount = garrison.Members.Count / 2;
        if (advancingCount <= 0)
        {
            return;
        }

        Vector3 from = garrison.Crystal.transform.position;
        Transform building = PlayerBuildingRegistry.FindNearest(from);
        if (ShouldRaidBuilding(building, from))
        {
            SendMembersToRaid(garrison, advancingCount, building);
            return;
        }

        CrystalNode nextObjective = RequestObjective(from);
        if (nextObjective != null && nextObjective != garrison.Crystal)
        {
            SendMembersToCrystal(garrison, advancingCount, nextObjective);
        }
    }

    // Ngay khi vừa chiếm được tinh thể: nếu có công trình người chơi trong tầm thì phái nửa đồn trú đi phá luôn.
    private void DispatchInitialRaid(GarrisonRecord garrison)
    {
        garrison.Members.RemoveAll(member => member == null);
        int raiderCount = garrison.Members.Count / 2;
        if (raiderCount <= 0)
        {
            return;
        }

        Vector3 from = garrison.Crystal.transform.position;
        Transform building = PlayerBuildingRegistry.FindNearest(from);
        if (building != null && IsWithinRaidRadius(building, from))
        {
            SendMembersToRaid(garrison, raiderCount, building);
        }
    }

    // Ưu tiên phá công trình khi có công trình gần (trong buildingRaidRadius) HOẶC theo xác suất ngẫu nhiên.
    private bool ShouldRaidBuilding(Transform building, Vector3 from)
    {
        if (building == null)
        {
            return false;
        }

        return IsWithinRaidRadius(building, from) || Random.value < buildingRaidChance;
    }

    private bool IsWithinRaidRadius(Transform building, Vector3 from)
    {
        return ((Vector2)building.position - (Vector2)from).sqrMagnitude <= buildingRaidRadius * buildingRaidRadius;
    }

    // Tách 'count' thành viên cuối danh sách cho đi phá công trình (AssignRaid tự gỡ họ khỏi Members).
    private void SendMembersToRaid(GarrisonRecord garrison, int count, Transform building)
    {
        for (int i = 0; i < count && garrison.Members.Count > 0; i++)
        {
            CrystalCampaignAgent member = garrison.Members[garrison.Members.Count - 1];
            if (member == null)
            {
                garrison.Members.RemoveAt(garrison.Members.Count - 1);
                continue;
            }
            member.AssignRaid(building);
        }
    }

    // Tách 'count' thành viên cuối danh sách cho đi chiếm tinh thể kế (AssignObjective tự gỡ họ khỏi Members).
    private void SendMembersToCrystal(GarrisonRecord garrison, int count, CrystalNode objective)
    {
        for (int i = 0; i < count && garrison.Members.Count > 0; i++)
        {
            CrystalCampaignAgent member = garrison.Members[garrison.Members.Count - 1];
            if (member == null)
            {
                garrison.Members.RemoveAt(garrison.Members.Count - 1);
                continue;
            }
            member.AssignObjective(objective);
        }
    }

    private GarrisonRecord FindOrCreateRecord(CrystalNode crystal)
    {
        GarrisonRecord record = FindRecord(crystal);
        if (record == null)
        {
            record = new GarrisonRecord { Crystal = crystal, AdvanceTimer = RollAdvanceDelay() };
            garrisons.Add(record);
        }
        return record;
    }

    private GarrisonRecord FindRecord(CrystalNode crystal)
    {
        foreach (GarrisonRecord garrison in garrisons)
        {
            if (garrison.Crystal == crystal)
            {
                return garrison;
            }
        }
        return null;
    }

    private GarrisonRecord FindNearestCapturedGarrison(Vector3 position)
    {
        GarrisonRecord nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (GarrisonRecord garrison in garrisons)
        {
            bool isEligible = garrison.Crystal != null
                && garrison.Crystal.State == CrystalState.Captured
                && garrison.Members.Count > 0;
            if (!isEligible)
            {
                continue;
            }

            float sqr = ((Vector2)garrison.Crystal.transform.position - (Vector2)position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = garrison;
            }
        }
        return nearest;
    }

    private float RollAdvanceDelay()
    {
        return Random.Range(minAdvanceSeconds, maxAdvanceSeconds);
    }
}
