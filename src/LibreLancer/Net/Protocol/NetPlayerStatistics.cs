namespace LibreLancer.Net.Protocol;

public struct NetPlayerStatistics
{
    public int TotalMissions;
    public int TotalKills;
    public int SystemsVisited;
    public int BasesVisited;
    public int JumpHolesFound;

    public int FightersKilled;
    public int FreightersKilled;
    public int TransportsKilled;
    public int BattleshipsKilled;

    public static NetPlayerStatistics Read(PacketReader reader) => new()
    {
        TotalMissions = reader.GetVariableInt32(),
        TotalKills = reader.GetVariableInt32(),
        SystemsVisited = reader.GetVariableInt32(),
        BasesVisited = reader.GetVariableInt32(),
        JumpHolesFound = reader.GetVariableInt32(),

        FightersKilled = reader.GetVariableInt32(),
        FreightersKilled = reader.GetVariableInt32(),
        TransportsKilled = reader.GetVariableInt32(),
        BattleshipsKilled = reader.GetVariableInt32(),
    };

    public void Put(PacketWriter message)
    {
        message.PutVariableInt32(TotalMissions);
        message.PutVariableInt32(TotalKills);
        message.PutVariableInt32(SystemsVisited);
        message.PutVariableInt32(BasesVisited);
        message.PutVariableInt32(JumpHolesFound);

        message.PutVariableInt32(FightersKilled);
        message.PutVariableInt32(FreightersKilled);
        message.PutVariableInt32(TransportsKilled);
        message.PutVariableInt32(BattleshipsKilled);
    }
}
