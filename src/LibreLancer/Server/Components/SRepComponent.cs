using System.Collections.Generic;
using LibreLancer.Data.GameData;
using LibreLancer.Net;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

// Base component for determining reputations between objects server-side
public class SRepComponent : GameComponent
{
    public Faction Faction;

    public Dictionary<GameObject, RepAttitude> forcedReps = new Dictionary<GameObject, RepAttitude>();

    public void SetAttitude(GameObject go, RepAttitude a)
    {
        forcedReps[go] = a;
        if (go.TryGetComponent<SPlayerComponent>(out var p))
        {
            p.Player.RpcClient.UpdateAttitude(new ObjNetId(Parent.NetID), a);
        }
    }

    static RepAttitude FromNumber(float a)
    {
        if (a <= Faction.HostileThreshold)
            return RepAttitude.Hostile;
        if (a >= Faction.FriendlyThreshold)
            return RepAttitude.Friendly;
        return 0;
    }

    public RepAttitude GetRep(GameObject other)
    {
        if (forcedReps.TryGetValue(other, out var f))
            return f;
        if (Faction == null)
            return RepAttitude.Neutral;
        if (other.TryGetComponent<SPlayerComponent>(out var sp))
        {
            return FromNumber(sp.Player.Character.Reputation.GetReputation(Faction));
        }
        if (other.TryGetComponent<SRepComponent>(out var r))
        {
            if (r.Faction != null)
                return FromNumber(Faction.GetReputation(r.Faction));
        }
        return RepAttitude.Neutral;
    }

    public bool IsHostileTo(GameObject other) => GetRep(other) == RepAttitude.Hostile;

    public SRepComponent(GameObject parent) : base(parent)
    {
    }
}
