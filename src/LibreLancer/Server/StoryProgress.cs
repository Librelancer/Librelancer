using System;
using System.Linq;
using LibreLancer.Data.Schema.Storyline;

namespace LibreLancer.Server;

public class StoryProgress
{
    public StoryMission CurrentMission;
    public StoryItem CurrentStory;
    public int MissionNum;
    public float NextLevelWorth;

    public void Advance(Player player)
    {
        var oldStory = CurrentStory;
        if (!oldStory.Skip &&
            !oldStory.Acceptance)
            player.LevelUp();
        foreach (var x in oldStory.Actions)
        {
            if(x.Type == StoryActionType.AddRTC)
                player.AddRTC(x.Argument);
        }
        var old = oldStory.Nickname;
        var next = player.Game.GameData.Items.Ini.Storyline.Items[Math.Clamp(MissionNum + 1, 0, player.Game.GameData.Items.Ini.Storyline.Items.Count - 1)];
        MissionNum++;
        // here in loadmission was a !next.Skip, but seems to be wrong, mentioned here just in case in the future is relevant, comment will be deleted later.
        bool loadMission = !string.Equals(oldStory.Mission, next.Mission, StringComparison.OrdinalIgnoreCase);
        CurrentStory = next;
        CurrentMission = player.Game.GameData.Items.Ini.Storyline.Missions
            .FirstOrDefault(x => x.Nickname.Equals(next.Mission, StringComparison.OrdinalIgnoreCase));
        if(loadMission)
            player.LoadMission();
        if (oldStory.CashUp > 0)
        {
            NextLevelWorth = player.CalculateNetWorth() + oldStory.CashUp;
            FLLog.Info("Mission", $"SET Next Level Worth: {NextLevelWorth} (+{oldStory.CashUp})");
        }
        FLLog.Info("Mission", $"Transitioned from {old} to {next.Nickname}");
        player.UpdateProgress();
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
        if (CurrentStory.Skip)
        {
            Advance(player);
        }
        else if (CurrentStory.CashUp > 0)
        {
            var playerNet = player.CalculateNetWorth();
            if(playerNet >= NextLevelWorth)
            {
                FLLog.Info("Mission", $"Current worth {playerNet} > {NextLevelWorth}");
                Advance(player);
            }
        }
    }
}
