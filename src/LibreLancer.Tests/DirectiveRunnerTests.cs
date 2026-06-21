using LibreLancer.Missions.Directives;
using LibreLancer.World;
using Xunit;

namespace LibreLancer.Tests;

public class DirectiveRunnerTests
{
    [Fact]
    public void CruiseContinuesBetweenGotoDirectives()
    {
        Assert.False(DirectiveRunnerComponent.ShouldStopAtTarget(
            GotoKind.GotoCruise, new GotoVecDirective()));
    }

    [Fact]
    public void CruiseStopsBeforeDelayOrEndOfList()
    {
        Assert.True(DirectiveRunnerComponent.ShouldStopAtTarget(
            GotoKind.GotoCruise, new DelayDirective()));
        Assert.True(DirectiveRunnerComponent.ShouldStopAtTarget(
            GotoKind.GotoCruise, null));
    }

    [Fact]
    public void NoCruiseGotoAlwaysStopsAtTarget()
    {
        Assert.True(DirectiveRunnerComponent.ShouldStopAtTarget(
            GotoKind.GotoNoCruise, new GotoVecDirective()));
    }
}
