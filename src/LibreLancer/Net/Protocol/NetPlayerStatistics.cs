namespace LibreLancer.Net.Protocol;

public struct NetPlayerStatistics
{
    public long TotalMissions;
    public long SystemsVisited;
    public long BasesVisited;
    public long JumpHolesFound;

    public long FightersKilled;
    public long FreightersKilled;
    public long TransportsKilled;
    public long BattleshipsKilled;

    public static NetPlayerStatistics Read(PacketReader reader) => new()
    {
        TotalMissions = (long)reader.GetVariableUInt64(),
        SystemsVisited = (long)reader.GetVariableUInt64(),
        BasesVisited = (long)reader.GetVariableUInt64(),
        JumpHolesFound = (long)reader.GetVariableUInt64(),

        FightersKilled = (long)reader.GetVariableUInt64(),
        FreightersKilled = (long)reader.GetVariableUInt64(),
        TransportsKilled = (long)reader.GetVariableUInt64(),
        BattleshipsKilled = (long)reader.GetVariableUInt64(),
    };

    public void Put(PacketWriter message)
    {
        message.PutVariableUInt64((ulong)TotalMissions);
        message.PutVariableUInt64((ulong)SystemsVisited);
        message.PutVariableUInt64((ulong)BasesVisited);
        message.PutVariableUInt64((ulong)JumpHolesFound);

        message.PutVariableUInt64((ulong)FightersKilled);
        message.PutVariableUInt64((ulong)FreightersKilled);
        message.PutVariableUInt64((ulong)TransportsKilled);
        message.PutVariableUInt64((ulong)BattleshipsKilled);
    }
}
