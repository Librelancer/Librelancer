using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.World;

public static class BaseNpcRules
{
    public static bool IsBribeAvailable(BaseNpcBribe bribe, ReputationCollection reputations)
    {
        return bribe.Faction != null &&
               bribe.Ids != 0 &&
               reputations.GetReputation(bribe.Faction) < Faction.FriendlyThreshold;
    }
}
