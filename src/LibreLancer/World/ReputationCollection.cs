using System.Collections.Generic;
using LibreLancer.Data.GameData;

namespace LibreLancer.World;

public class ReputationCollection
{
    public Dictionary<Faction, float> Reputations = new Dictionary<Faction, float>();

    public float GetReputation(Faction f)
    {
        if (f == null) return 0;
        if (Reputations.TryGetValue(f, out var r)) return r;
        else return 0;
    }
}
