using UnityEngine;

public enum RTSFaction
{
    Player,
    Enemy
}

[DisallowMultipleComponent]
public class RTSUnitFaction : MonoBehaviour
{
    [SerializeField] private RTSFaction faction = RTSFaction.Player;
    [SerializeField] private bool canBeTargeted = true;
    [SerializeField] private bool useDefaultHostiles = true;
    [SerializeField] private RTSFaction[] hostileFactions = new RTSFaction[0];

    public RTSFaction Faction => faction;
    public bool CanBeTargeted => canBeTargeted;

    public bool CanAttack(RTSUnitFaction targetFaction)
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

    private bool IsDefaultHostile(RTSFaction targetFaction)
    {
        return (faction == RTSFaction.Player && targetFaction == RTSFaction.Enemy)
            || (faction == RTSFaction.Enemy && targetFaction == RTSFaction.Player);
    }
}
