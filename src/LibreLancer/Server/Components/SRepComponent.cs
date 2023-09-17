using System.Collections.Generic;
using LibreLancer.GameData;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

// Base component for determining reputations between objects server-side
public class SRepComponent : GameComponent
{
    public Faction Faction;
    public List<GameObject> HostileNPCs = new List<GameObject>();

    public bool IsHostileTo(GameObject other)
    {
        if (HostileNPCs.Contains(other))
            return true;
        if (Faction == null)
            return false;
        if (other.TryGetComponent<SPlayerComponent>(out var sp))
        {
            if (sp.Player.Character.Reputation.GetReputation(Faction) < -0.4f)
                return true;
        }
        else if (other.TryGetComponent<SRepComponent>(out var r))
        {
            if (r.Faction != null && Faction.GetReputation(r.Faction) < -0.4f)
                return true;
        }
        return false;
    }

    public SRepComponent(GameObject parent) : base(parent)
    {
    }
}
