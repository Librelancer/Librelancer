using LibreLancer.Server;

namespace LibreLancer.Missions;

public class MissionTransitions
{
    //This needs to become configurable later on
    public static void NextMission(Player player)
    {
        if (player.CurrentMissionNumber == 1) {
            //Hardcoded in Vanilla engine: Add RTC for pittsburgh m01a-m01b
            player.AddRTC("missions\\m01a\\M001a_s006x_Li01_02_nrml.ini");
        }
    }
}
