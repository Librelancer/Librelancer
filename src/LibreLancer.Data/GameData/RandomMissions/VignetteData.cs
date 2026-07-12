using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Data.GameData.RandomMissions;

public class VignetteData : VignetteTreeNode
{
    public int Id;
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
}
