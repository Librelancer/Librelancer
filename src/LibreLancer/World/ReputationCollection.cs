using System.Collections.Generic;
using LibreLancer.Data.GameData;

namespace LibreLancer.World;

public class ReputationCollection
{
    public Dictionary<Faction, float> Reputations = new();

    public float GetReputation(Faction? f)
    {
        if (f == null)
        {
            return 0;
        }

        return Reputations.TryGetValue(f, out var r) ? r : 0;
    }
}
