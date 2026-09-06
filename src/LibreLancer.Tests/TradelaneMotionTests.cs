using System;
using System.Numerics;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class TradelaneMotionTests
{
    [Theory]
    [InlineData(0, 2500)]
    [InlineData(0.5f, 850)]
    [InlineData(1, 300)]
    public void SlowdownSpeedUsesTheFullThrottleTarget(float progress, float expected)
    {
        Assert.Equal(expected, TradelaneMotion.SlowdownSpeed(progress, 300), 3);
    }

    [Fact]
    public void SlowdownSpeedDoesNotIncrease()
    {
        var previous = TradelaneMotion.SlowdownSpeed(0, 300);
        for (var i = 1; i <= 100; i++)
        {
            var current = TradelaneMotion.SlowdownSpeed(i / 100f, 300);
            Assert.True(current <= previous);
            previous = current;
        }
    }

    [Fact]
    public void ManualExitDoesNotBankTheShip()
    {
        var start = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(20));
        var target = TradelaneMotion.TurnRight(start, TradelaneMotion.ManualTurnDegrees);
        var orientation = TradelaneMotion.ManualExitOrientation(start, target, 0.5f);
        var up = Vector3.Transform(Vector3.UnitY, orientation);

        Assert.Equal(Vector3.UnitY.X, up.X, 3);
        Assert.Equal(Vector3.UnitY.Y, up.Y, 3);
        Assert.Equal(Vector3.UnitY.Z, up.Z, 3);
    }

    [Fact]
    public void AutomaticSlowdownRequiresEnoughApproachDistanceAndARealPenultimateRing()
    {
        Assert.True(TradelaneMotion.CanStartAutomaticSlowdown(2500, true, true));
        Assert.False(TradelaneMotion.CanStartAutomaticSlowdown(2500, false, true));
        Assert.False(TradelaneMotion.CanStartAutomaticSlowdown(2500, true, false));
        Assert.False(TradelaneMotion.CanStartAutomaticSlowdown(3001, true, true));
    }

    [Fact]
    public void ManualExitTurnsThirtyDegreesToTheRight()
    {
        var orientation = TradelaneMotion.TurnRight(
            Quaternion.Identity, TradelaneMotion.ManualTurnDegrees);
        var forward = TradelaneMotion.Forward(orientation);
        var right = Vector3.UnitX;

        Assert.Equal(MathF.Sin(MathHelper.DegreesToRadians(30)), Vector3.Dot(forward, right), 3);
    }
}
