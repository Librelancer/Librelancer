using WattleScript.Interpreter;

namespace LibreLancer.Client;

[WattleScriptUserData]
public class PlayerStats
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
}
