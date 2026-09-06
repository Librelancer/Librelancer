using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save;

[ParsedSection]
public partial class SaveRandomMission : IWriteSection
{
    [Entry("type")]
    public string Type = "DestroyMission"; //Should always be DestroyMission
    [Entry("offer_group")]
    public int OfferGroup; //FactionHash, not CreateID HashValue
    [Entry("offer_base")]
    public HashValue OfferBase;
    [Entry("offer_ids")]
    public int OfferIds; //Unused, 0
    [Entry("reward")]
    public int Reward;
    [Entry("rep_reward")]
    public float RepReward;
    [Entry("rep_hit")]
    public float RepHit;
    [Entry("difficulty")]
    public float Difficulty;
    [Entry("jump_pref")]
    public int JumpPref;
    [Entry("enemy_group")]
    public int EnemyGroup; //FactionHash, not CreateID HashValue
    [Entry("dest_sys")]
    public HashValue DestSys;
    [Entry("objective_pos")]
    public Vector3 ObjectivePos;
    [Entry("objective_zone")]
    public HashValue ObjectiveZone;
    [Entry("random_seed")]
    public int RandomSeed; //15-bit value for srand()


    public void WriteTo(IniBuilder builder) =>
        builder.Section("RandomMission")
            .Entry("type", Type)
            .Entry("offer_group", OfferGroup)
            .Entry("offer_base", OfferBase)
            .Entry("offer_ids", OfferIds)
            .Entry("reward", Reward)
            .Entry("rep_reward", RepReward)
            .Entry("rep_hit", RepHit)
            .Entry("difficulty", Difficulty)
            .Entry("jump_pref", JumpPref)
            .Entry("enemy_group", EnemyGroup)
            .Entry("dest_sys", DestSys)
            .Entry("objective_pos", ObjectivePos)
            .Entry("objective_zone", ObjectiveZone)
            .Entry("random_seed", RandomSeed);
}
