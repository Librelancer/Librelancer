using System;
using System.Numerics;
using LibreLancer.World.Components;
using Xunit;

namespace LibreLancer.Tests;

public class FormationControlTests
{
    private const float Epsilon = 0.001f;

    [Fact]
    public void RotatingLeaderContributesToSlotVelocity()
    {
        var velocity = FormationControl.SlotVelocity(new Vector3(0, 0, -100), Vector3.UnitY,
            new Vector3(10, 0, 0));

        Assert.Equal(new Vector3(0, 0, -110), velocity);
    }

    [Fact]
    public void MovingSlotDoesNotReverseHeadingAfterSmallOvershoot()
    {
        var desired = FormationControl.DesiredVelocity(new Vector3(0, 0, -100), new Vector3(0, 0, 20));

        Assert.True(desired.Z < 0);
    }

    [Fact]
    public void PositionCorrectionIsCapped()
    {
        var desired = FormationControl.DesiredVelocity(Vector3.Zero, new Vector3(1000, 0, 0));

        AssertClose(120, desired.Length());
    }

    [Fact]
    public void StandardThrottleBrakesOverspeedAndLimitsMisalignment()
    {
        var forward = -Vector3.UnitZ;
        var braking = FormationControl.StandardThrottle(1, new Vector3(0, 0, -100),
            new Vector3(0, 0, -130), forward);
        var misaligned = FormationControl.StandardThrottle(1, new Vector3(100, 0, 0), Vector3.Zero, forward);

        Assert.True(braking < 1);
        Assert.True(misaligned < braking);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0)]
    [InlineData(112.5f, 0.5f)]
    [InlineData(125, 1)]
    [InlineData(250, 1)]
    public void ArrivalThrottleFallsGraduallyToZero(float distance, float expected)
    {
        var throttle = FormationControl.ArrivalThrottle(1, distance);

        AssertClose(expected, throttle);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(500, 0)]
    [InlineData(625, -75)]
    [InlineData(750, -150)]
    [InlineData(875, -225)]
    [InlineData(1000, -300)]
    [InlineData(2000, -300)]
    public void GotoReferenceGraduallyReducesCruiseSpeed(float distance, float expectedOffset)
    {
        var offset = GotoBehavior.ReferenceCruiseSpeedOffset(distance, 500, 1000, 300);

        AssertClose(expectedOffset, offset);
    }

    [Fact]
    public void GotoReferenceIgnoresInvalidDistanceRange()
    {
        Assert.Equal(0, GotoBehavior.ReferenceCruiseSpeedOffset(750, 1000, 500, 300));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, 1)]
    [InlineData(625, 0.75f)]
    [InlineData(750, 0.5f)]
    [InlineData(875, 0.25f)]
    [InlineData(1000, 0)]
    [InlineData(2000, 0)]
    public void GotoReferenceGraduallyReducesThrottle(float distance, float expectedFactor)
    {
        AssertClose(expectedFactor, GotoBehavior.ReferenceCruiseFactor(distance, 500, 1000));
    }

    [Fact]
    public void CruiseCorrectionBoostsCatchupAndBrakesClosingSpeed()
    {
        var direction = -Vector3.UnitZ;
        var catchup = FormationControl.CruiseSpeedOffset(new Vector3(0, 0, -200),
            new Vector3(0, 0, -100), new Vector3(0, 0, -100), direction, 300);
        var braking = FormationControl.CruiseSpeedOffset(Vector3.Zero,
            new Vector3(0, 0, -140), new Vector3(0, 0, -100), direction, 300);

        Assert.True(catchup > 0);
        Assert.True(braking < 0);
        Assert.InRange(catchup, -60, 60);
        Assert.InRange(braking, -60, 60);
    }

    [Fact]
    public void SeparationPredictsClosestApproach()
    {
        FormationControl.Neighbor[] neighbors =
        [new(new Vector3(30, 0, 0), Vector3.Zero, 5, 2)];

        var separation = FormationControl.CalculateSeparation(Vector3.Zero, new Vector3(20, 0, 0), Vector3.UnitX,
            5, 1, neighbors);

        Assert.True(separation.Active);
        Assert.True(separation.Direction.X < 0);
        Assert.True(separation.Brake > 0);
    }

    [Fact]
    public void SeparationDoesNotBrakeForShipBehind()
    {
        FormationControl.Neighbor[] neighbors =
        [new(new Vector3(-10, 0, 0), Vector3.Zero, 5, 2)];

        var separation = FormationControl.CalculateSeparation(Vector3.Zero, Vector3.Zero, Vector3.UnitX,
            5, 1, neighbors);

        Assert.True(separation.Active);
        Assert.Equal(0, separation.Brake);
    }

    [Fact]
    public void SeparationIsStableForCoincidentShipsAndReleasesAfterHold()
    {
        FormationControl.Neighbor[] neighbors =
        [new(Vector3.Zero, Vector3.Zero, 5, 2)];
        var current = FormationControl.CalculateSeparation(Vector3.Zero, Vector3.Zero, Vector3.UnitZ,
            5, 1, neighbors);
        var held = Vector3.Zero;
        var heldNeighbor = 0;
        var timer = 0f;

        var active = FormationControl.ApplySeparationHysteresis(current, ref held, ref heldNeighbor, ref timer, 0);
        var stillHeld = FormationControl.ApplySeparationHysteresis(FormationControl.Separation.None,
            ref held, ref heldNeighbor, ref timer, FormationControl.SeparationHoldTime * 0.5f);
        var released = FormationControl.ApplySeparationHysteresis(FormationControl.Separation.None,
            ref held, ref heldNeighbor, ref timer, FormationControl.SeparationHoldTime);

        Assert.True(active.Active);
        Assert.True(stillHeld.Active);
        Assert.False(released.Active);
        Assert.Equal(Vector3.Zero, held);
    }

    private static void AssertClose(float expected, float actual) =>
        Assert.True(MathF.Abs(expected - actual) < Epsilon, $"Expected {expected}, got {actual}");
}
