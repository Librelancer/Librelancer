using System.Collections.Generic;
using System.Numerics;
using System.Text;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteData(int id) : VignetteTreeNode(id)
{
    // Selection criteria
    public float Weight;
    public Vector2 DifficultyRange = new(-1000, 1000);
    public bool Implemented;
    public AllowedZoneType AllowedZone = AllowedZoneType.All;
    public HashSet<uint>? OfferGroups = null;
    public HashSet<uint>? HostileGroups = null;

    // Mission data
    public VignetteString FailureText;
    public VignetteString RewardText;
    public List<VignetteString> ObjectiveTexts = [];
    public List<OfferTextEntry> OfferTexts = [];
    public List<CommSequence> CommSequences = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Weight != 1)
            sb.Append($" weight: {Weight}");
        if (DifficultyRange != new Vector2(-1000, 1000))
            sb.Append($" difficulty: {DifficultyRange}");
        if (!Implemented)
            sb.Append(" unimplemented");
        if (AllowedZone != AllowedZoneType.All)
            sb.Append($" zone {AllowedZone}");
        if (OfferGroups != null)
            sb.Append(" offer_filter");
        if (HostileGroups != null)
            sb.Append(" hostile_filter");
        if (FailureText.Target != null)
            sb.Append(" failure_text");
        if (RewardText.Target != null)
            sb.Append(" reward_text");
        if (ObjectiveTexts.Count > 0)
            sb.Append(" objective_text");
        if (OfferTexts.Count > 0)
            sb.Append(" offer_text");
        if (CommSequences.Count > 0)
            sb.Append(" comm_sequence");
        return $"{Id}: Data{sb}";
    }
}
