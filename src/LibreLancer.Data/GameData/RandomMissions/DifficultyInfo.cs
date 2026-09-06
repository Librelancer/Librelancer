using System.Collections.Generic;

namespace LibreLancer.Data.GameData.RandomMissions;

public class DifficultyInfo
{
    public List<(float Difficulty, float Money)> MoneyGraph = [];
    public List<(StoryIndex Index, float Difficulty)> StoryGraph = [];

    public bool TryGetDifficultyCenter(int? story, out float value)
    {
        value = 0;
        if (story == null || StoryGraph.Count == 0)
            return false;
        for (int i = 0; i < StoryGraph.Count; i++)
        {
            if (StoryGraph[i].Index.Index == story)
            {
                value = StoryGraph[i].Difficulty;
                return true;
            }
            if (StoryGraph[i].Index.Index > story)
                return true;
            value = StoryGraph[i].Difficulty;
        }
        return true;
    }

    public int GetMissionReward(float difficulty)
    {
        if (MoneyGraph.Count == 0)
            return (int)(difficulty * 22_000f);
        if (difficulty <= MoneyGraph[0].Difficulty)
            return (int)MoneyGraph[0].Money;
        for (int i = 0; i < MoneyGraph.Count - 1; i++)
        {
            if (difficulty >= MoneyGraph[i].Difficulty &&
                difficulty < MoneyGraph[i + 1].Difficulty)
            {
                return (int)Easing.Ease(EasingTypes.Linear, difficulty, MoneyGraph[i].Difficulty,
                    MoneyGraph[i + 1].Difficulty, MoneyGraph[i].Money, MoneyGraph[i + 1].Money);
            }
        }
        return (int)MoneyGraph[^1].Money;
    }

}
