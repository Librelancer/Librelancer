using System.Collections.Generic;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Server.RandomMissions;

public class VignetteStrings
{
    public List<string> Messages = [];
    public Dictionary<string, VignetteString> ObjectiveStrings = new();
    public Dictionary<string, CommSequence> CommSequences = new();
    public VignetteString RewardText;
    public VignetteString FailureText;
    public List<OfferTextItem> OfferText = [];
}
