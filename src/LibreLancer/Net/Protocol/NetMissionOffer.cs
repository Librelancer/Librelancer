using WattleScript.Interpreter;

namespace LibreLancer.Net.Protocol;

[WattleScriptUserData]
public struct NetMissionOffer
{
    public int Id;
    public int NpcIdsName;
    public int FactionIdsName;
    public int SystemIdsName;
    public int Reward;
    public int Seed;
    public string MissionType;
    public string OfferText;
    public string TargetName;

    public void Put(PacketWriter message)
    {
        message.PutVariableInt32(Id);
        message.PutVariableInt32(NpcIdsName);
        message.PutVariableInt32(FactionIdsName);
        message.PutVariableInt32(SystemIdsName);
        message.PutVariableInt32(Reward);
        message.PutVariableInt32(Seed);
        message.Put(MissionType);
        message.Put(OfferText ?? "");
        message.Put(TargetName ?? "");
    }

    public static NetMissionOffer Read(PacketReader message) => new()
    {
        Id = message.GetVariableInt32(),
        NpcIdsName = message.GetVariableInt32(),
        FactionIdsName = message.GetVariableInt32(),
        SystemIdsName = message.GetVariableInt32(),
        Reward = message.GetVariableInt32(),
        Seed = message.GetVariableInt32(),
        MissionType = message.GetString()!,
        OfferText = message.GetString()!,
        TargetName = message.GetString()!
    };
}
