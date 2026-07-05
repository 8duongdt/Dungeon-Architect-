using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ não chiến dịch của phe Human: cấp mục tiêu "tinh thể chưa-bị-chiếm gần nhất" cho từng
/// <see cref="CrystalCampaignAgent"/>, quản lý đồn trú theo tinh thể đã chiếm, hẹn giờ ngẫu nhiên
/// 60-120 giây rồi tách NỬA đồn trú đi chiếm cục kế tiếp, và khi người chơi tái chiếm một cục thì
/// điều toàn bộ đồn trú ở cục-đỏ gần nhất kéo về phản công. Singleton scene theo mẫu
/// <see cref="LevelProgressTracker"/>; nghe <see cref="CrystalNode.AnyStateChanged"/> một chỗ
/// thay vì subscribe từng node.
/// </summary>
public class HumanCampaignDirector : MonoBehaviour
{
    public static HumanCampaignDirector Instance { get; private set; }

    [Tooltip("Khoảng thời gian (giây) tối thiểu đồn trú giữ vị trí trước khi tách nửa quân đi chiếm tiếp.")]
    [SerializeField] private float minAdvanceSeconds = 60f;

    [Tooltip("Khoảng thời gian (giây) tối đa đồn trú giữ vị trí trước khi tách nửa quân đi chiếm tiếp.")]
    [SerializeField] private float maxAdvanceSeconds = 120f;

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
            TickGarrison(garrison);
        }
        garrisons.RemoveAll(record => record.Crystal == null);
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

    // Tách nửa quân (làm tròn xuống) đi chiếm mục tiêu kế tiếp, nửa còn lại giữ vị trí.
    private void AdvanceHalfGarrison(GarrisonRecord garrison)
    {
        CrystalNode nextObjective = RequestObjective(garrison.Crystal.transform.position);
        int advancingCount = garrison.Members.Count / 2;
        bool hasAdvance = nextObjective != null && nextObjective != garrison.Crystal && advancingCount > 0;
        if (!hasAdvance)
        {
            return;
        }

        for (int i = 0; i < advancingCount; i++)
        {
            CrystalCampaignAgent member = garrison.Members[garrison.Members.Count - 1];
            member.AssignObjective(nextObjective);
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
