using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;

namespace LibreLancer.World;

public static class BaseNpcRules
{
    public const float BribeReputation = 0.6f;

    public static bool IsBribeAvailable(BaseNpcBribe bribe, ReputationCollection reputations)
    {
        return bribe.Faction != null &&
               bribe.Ids != 0 &&
               reputations.GetReputation(bribe.Faction) < Faction.FriendlyThreshold;
    }

    public static float RumorReputationThreshold(Faction? faction) =>
        faction?.Properties?.Legality == Legality.Unlawful ? 0.4f : 0.2f;

    public static bool CanHearRumors(Faction? faction, ReputationCollection reputations) =>
        reputations.GetReputation(faction) >= RumorReputationThreshold(faction);
}
