using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

[ParsedSection]
public partial class DataNode : VignetteNode
{
    [Entry("offer_group")]
    public string[] OfferGroup;
    [Entry("hostile_group")]
    public string[] HostileGroup;

    [Entry("difficulty")]
    public Vector2? Difficulty;

    [Entry("weight")]
    public int? Weight;

    [Entry("allowable_zone_types")]
    public string[] AllowableZoneTypes;

    [Entry("Implemented")]
    public bool Implemented = true;

    public VignetteString FailureText;
    public VignetteString RewardText;
    public List<VignetteString> ObjectiveTexts = new();
    public List<OfferTextEntry> OfferTexts = new();
    public List<CommSequence> CommSequences = new();

    [EntryHandler("failure_text", MinComponents = 2)]
    void HandleFailureText(Entry e) => VignetteString.TryParse(true, e, out FailureText);
    [EntryHandler("reward_text", MinComponents = 2)]
    void HandleRewardText(Entry e) => VignetteString.TryParse(true, e, out RewardText);

    [EntryHandler("objective_text", MinComponents = 2, Multiline = true)]
    void HandleObjectiveText(Entry e)
    {
        if(VignetteString.TryParse(false, e, out var ot))
            ObjectiveTexts.Add(ot);
    }

    [EntryHandler("offer_text", MinComponents = 2, Multiline = true)]
    void HandleOfferText(Entry e) => OfferTexts.Add(OfferTextEntry.FromEntry(e));

    [EntryHandler("comm_sequence", MinComponents = 7, Multiline = true)]
    void HandleCommSequence(Entry e) => CommSequences.Add(CommSequence.FromEntry(e));
}
