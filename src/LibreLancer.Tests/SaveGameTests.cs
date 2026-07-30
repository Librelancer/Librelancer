using LibreLancer.Data.Schema.Save;
using Xunit;

namespace LibreLancer.Tests;

public class SaveGameTests
{
    [Fact]
    public void IncompleteObjectiveTypeDoesNotThrow()
    {
        const string saveText = """
                                [NNObjective]
                                nickname = 0
                                type = 7,
                                state = 58
                                hidden = 0

                                [NNObjective]
                                nickname = 1
                                type = 2, 123, 456
                                state = 10
                                hidden = 0
                                """;

        var save = SaveGame.FromString("incomplete-objective.fl", saveText);

        Assert.Equal(2, save.Objectives.Count);
        Assert.Equal(7, save.Objectives[0].Type);
        Assert.Equal(0, save.Objectives[0].ObjNickname);
        Assert.Equal(0, save.Objectives[0].IdsOne);
        Assert.Equal(2, save.Objectives[1].Type);
        Assert.Equal(123, save.Objectives[1].ObjNickname);
        Assert.Equal(456, save.Objectives[1].IdsOne);
    }
}
