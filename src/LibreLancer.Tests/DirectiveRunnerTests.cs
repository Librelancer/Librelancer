using LibreLancer.Missions.Directives;
using LibreLancer.World;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class DirectiveRunnerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AvoidanceDirectiveUpdatesAutopilot(bool enabled)
    {
        var ship = new GameObject();
        var autopilot = new AutopilotComponent(ship);
        ship.AddComponent(autopilot);
        var runner = new DirectiveRunnerComponent(ship);

        runner.SetDirectives([new AvoidanceDirective { Avoidance = enabled }], null!);

        Assert.Equal(enabled, autopilot.AvoidanceEnabled);
    }
}
