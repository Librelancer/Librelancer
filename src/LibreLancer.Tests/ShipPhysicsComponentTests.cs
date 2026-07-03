using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class ShipPhysicsComponentTests
{
    [Theory]
    [InlineData(0, true, 0)]
    [InlineData(0.4f, true, 0.4f)]
    [InlineData(1.2f, true, 1)]
    [InlineData(0.4f, false, 1)]
    public void FormationThrottleIsRespectedWhileCruiseCharges(float throttle, bool formationFollower,
        float expected)
    {
        Assert.Equal(expected, ShipPhysicsComponent.CruiseChargeEnginePower(throttle, formationFollower));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1.2f, 1)]
    public void RestrictedGotoThrottleIsRespectedWhileCruiseCharges(float throttle, float expected)
    {
        Assert.Equal(expected, ShipPhysicsComponent.CruiseChargeEnginePower(throttle, false, true));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(150, 0, 500)]
    [InlineData(300, 0, 1000)]
    [InlineData(0, 1, 0)]
    [InlineData(150, 1, 1500)]
    [InlineData(300, 1, 3000)]
    public void RestrictedCruiseSpeedScalesAccelerationImmediately(float cruiseSpeed, float accelerationPercent,
        float expectedForce)
    {
        var force = ShipPhysicsComponent.CruiseEngineForce(cruiseSpeed, 300, 1000, 10,
            accelerationPercent);

        Assert.Equal(expectedForce, force);
    }

    [Fact]
    public void EscortUsesLowestMemberSpeedFactor()
    {
        Assert.Equal(0.25f, ShipSteeringComponent.EscortSpeedFactor(false, [1, 0.5f, 0.25f]));
    }

    [Fact]
    public void PlayerEscortMemberIgnoresNpcSpeedFactor()
    {
        Assert.Equal(1, ShipSteeringComponent.EscortSpeedFactor(true, [0]));
    }
}
