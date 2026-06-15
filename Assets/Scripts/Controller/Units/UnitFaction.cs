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

    public FactionType Faction => faction;
    public bool CanBeTargeted => canBeTargeted;

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
