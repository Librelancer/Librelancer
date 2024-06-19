using System;
using System.Linq;
using LibreLancer.Data.Storyline;

namespace LibreLancer.Server;

public class StoryProgress
{
    public StoryMission CurrentMission;
    public StoryItem CurrentStory;
    public int MissionNum;
    public float NextLevelWorth;

    public void Advance(Player player)
    {
        if (!CurrentStory.Skip &&
            !CurrentStory.Acceptance)
            player.LevelUp();
        foreach (var x in CurrentStory.Actions)
        {
            if(x.Type == StoryActionType.AddRTC)
                player.AddRTC(x.Argument);
        }
        var old = CurrentStory.Nickname;
        var next = player.Game.GameData.Ini.Storyline.Items[MissionNum + 1];
        MissionNum++;
        bool loadMission = !next.Skip &&
                           !string.Equals(CurrentStory.Mission, next.Mission, StringComparison.OrdinalIgnoreCase);
        CurrentStory = next;
        CurrentMission = player.Game.GameData.Ini.Storyline.Missions
            .FirstOrDefault(x => x.Nickname.Equals(next.Mission, StringComparison.OrdinalIgnoreCase));
        if(loadMission)
            player.LoadMission();
        FLLog.Info("Mission", $"Transitioned from {old} to {next.Nickname}");
        // Skip if needed
        Update(player);
    }

    public void MissionAccepted(Player player)
    {
        if(CurrentStory.Acceptance)
            Advance(player);
    }

    public void Update(Player player)
    {
        if(CurrentStory.Skip) {
            Advance(player);
            return;
        }
    }
}
