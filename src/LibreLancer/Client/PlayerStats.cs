using WattleScript.Interpreter;

namespace LibreLancer.Client;

[WattleScriptUserData]
public class PlayerStats
{
    public long TotalMissions;
    public long TotalKills;
    public long SystemsVisited;
    public long BasesVisited;
    public long JumpHolesFound;

    public long FightersKilled;
    public long FreightersKilled;
    public long TransportsKilled;
    public long BattleshipsKilled;
}
