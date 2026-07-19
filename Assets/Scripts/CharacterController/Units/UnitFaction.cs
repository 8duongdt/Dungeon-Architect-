using System.Collections.Generic;
using UnityEngine;

public enum FactionType
{
    Player,
    Enemy
}

[DisallowMultipleComponent]
public class UnitFaction : MonoBehaviour
{
    [SerializeField] private FactionType faction = FactionType.Player;
    [SerializeField] private bool canBeTargeted = true;
    [SerializeField] private bool useDefaultHostiles = true;
    [SerializeField] private FactionType[] hostileFactions = new FactionType[0];
    [Tooltip("Cho phép unit cùng phe đi xuyên qua nhau (bỏ va chạm vật lý) để không bị đẩy/trôi khi đánh hoặc di chuyển.")]
    [SerializeField] private bool ignoreCollisionWithAllies = true;

    // Danh sách toàn bộ unit đang hoạt động để thiết lập bỏ va chạm giữa đồng minh khi spawn.
    private static readonly List<UnitFaction> activeUnits = new List<UnitFaction>();
    private Collider2D[] bodyColliders;

    public FactionType Faction => faction;
    public bool CanBeTargeted => canBeTargeted;

    private void Awake()
    {
        CacheBodyColliders();
    }

    private void OnEnable()
    {
        if (ignoreCollisionWithAllies)
        {
            foreach (UnitFaction other in activeUnits)
            {
                if (other != null && other != this && other.faction == faction)
                {
                    SetIgnoreCollision(this, other, true);
                }
            }
        }

        activeUnits.Add(this);
        MinimapRegistry.Register(this);
    }

    private void OnDisable()
    {
        activeUnits.Remove(this);
        MinimapRegistry.Unregister(this);
    }

    private void CacheBodyColliders()
    {
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>(true);
        List<Collider2D> solidColliders = new List<Collider2D>();
        foreach (Collider2D collider in allColliders)
        {
            // Chỉ lấy collider vật lý (không phải trigger) — giữ nguyên các vùng
            // phát hiện/tầm đánh (trigger) để không phá logic combat.
            if (collider != null && !collider.isTrigger)
            {
                solidColliders.Add(collider);
            }
        }

        bodyColliders = solidColliders.ToArray();
    }

    private static void SetIgnoreCollision(UnitFaction a, UnitFaction b, bool ignore)
    {
        if (a.bodyColliders == null || b.bodyColliders == null)
        {
            return;
        }

        foreach (Collider2D colliderA in a.bodyColliders)
        {
            if (colliderA == null)
            {
                continue;
            }

            foreach (Collider2D colliderB in b.bodyColliders)
            {
                if (colliderB == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(colliderA, colliderB, ignore);
            }
        }
    }

    public bool CanAttack(UnitFaction targetFaction)
    {
        if (targetFaction == null || targetFaction == this || !targetFaction.CanBeTargeted)
        {
            return false;
        }

        if (useDefaultHostiles)
        {
            return IsDefaultHostile(targetFaction.Faction);
        }

        for (int i = 0; i < hostileFactions.Length; i++)
        {
            if (hostileFactions[i] == targetFaction.Faction)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDefaultHostile(FactionType targetFaction)
    {
        return (faction == FactionType.Player && targetFaction == FactionType.Enemy)
            || (faction == FactionType.Enemy && targetFaction == FactionType.Player);
    }
}
