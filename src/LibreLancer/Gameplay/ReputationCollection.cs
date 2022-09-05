using System.Collections.Generic;
using LibreLancer.GameData;

namespace LibreLancer;

public class ReputationCollection
{
    public Dictionary<Faction, float> Reputations = new Dictionary<Faction, float>();

    public float GetReputation(Faction f)
    {
        if (Reputations.TryGetValue(f, out var r)) return r;
        else return 0;
    }
}