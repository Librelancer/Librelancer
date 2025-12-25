// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Server.Ai.Trade;
using Xunit;

namespace LibreLancer.Tests.Trade;

/// <summary>
/// Unit tests for the AiTradeState trading system.
/// </summary>
public class TradeSystemTests
{
    #region TradeConstants Tests

    [Fact]
    public void TradeConstants_SpeedValues_ArePositive()
    {
        Assert.True(TradeConstants.NORMAL_SPEED > 0);
        Assert.True(TradeConstants.CRUISE_SPEED > 0);
        Assert.True(TradeConstants.TRADELANE_SPEED > 0);
    }

    [Fact]
    public void TradeConstants_SpeedHierarchy_IsCorrect()
    {
        // Tradelane should be fastest, then cruise, then normal
        Assert.True(TradeConstants.TRADELANE_SPEED > TradeConstants.CRUISE_SPEED);
        Assert.True(TradeConstants.CRUISE_SPEED > TradeConstants.NORMAL_SPEED);
    }

    [Fact]
    public void TradeConstants_SearchDistance_IsPositive()
    {
        Assert.True(TradeConstants.MAX_TRADELANE_SEARCH_DISTANCE > 0);
    }

    [Fact]
    public void TradeConstants_EfficiencyThreshold_IsValid()
    {
        // Threshold should be between 0 and 1 (or slightly above 1)
        Assert.True(TradeConstants.TRADELANE_EFFICIENCY_THRESHOLD > 0);
        Assert.True(TradeConstants.TRADELANE_EFFICIENCY_THRESHOLD <= 2.0f);
    }

    [Fact]
    public void TradeConstants_TradelaneSpeed_IsFasterThanCruise()
    {
        // Tradelanes should be significantly faster than cruise
        float ratio = TradeConstants.TRADELANE_SPEED / TradeConstants.CRUISE_SPEED;
        Assert.True(ratio > 2.0f, "Tradelane should be at least 2x faster than cruise");
    }

    #endregion

    #region RouteSegment Tests

    [Fact]
    public void RouteSegment_CreateSpaceTravel_CalculatesTimeCorrectly()
    {
        var start = new Vector3(0, 0, 0);
        var end = new Vector3(600, 0, 0); // 600 units distance

        var segment = RouteSegment.CreateSpaceTravel(start, end, useCruise: true);

        Assert.Equal(RouteSegmentType.SpaceTravel, segment.Type);
        Assert.Equal(start, segment.StartPosition);
        Assert.Equal(end, segment.EndPosition);

        // Time = distance / speed = 600 / 600 (CRUISE_SPEED) = 1.0 second
        Assert.Equal(1.0, segment.EstimatedTime, precision: 2);
    }

    [Fact]
    public void RouteSegment_CreateSpaceTravel_NoCruise_SlowerTime()
    {
        var start = new Vector3(0, 0, 0);
        var end = new Vector3(300, 0, 0); // 300 units distance

        var withCruise = RouteSegment.CreateSpaceTravel(start, end, useCruise: true);
        var noCruise = RouteSegment.CreateSpaceTravel(start, end, useCruise: false);

        // Without cruise should take longer
        Assert.True(noCruise.EstimatedTime > withCruise.EstimatedTime);
    }

    [Fact]
    public void RouteSegment_CreateSpaceTravel_ZeroDistance_ZeroTime()
    {
        var pos = new Vector3(100, 200, 300);
        var segment = RouteSegment.CreateSpaceTravel(pos, pos, useCruise: true);

        Assert.Equal(0.0, segment.EstimatedTime);
    }

    [Fact]
    public void RouteSegment_ToString_ReturnsReadableFormat()
    {
        var start = new Vector3(0, 0, 0);
        var end = new Vector3(1000, 0, 0);

        var segment = RouteSegment.CreateSpaceTravel(start, end, useCruise: true);
        var str = segment.ToString();

        Assert.Contains("Space", str);
        Assert.Contains("1000", str); // Distance
    }

    #endregion

    #region TradeRoute Tests

    [Fact]
    public void TradeRoute_EmptySegments_IsInvalid()
    {
        var route = new TradeRoute(null, null, null);

        Assert.False(route.IsValid);
        Assert.False(route.UsesTradelanes);
        Assert.Equal(0, route.TotalEstimatedTime);
    }

    [Fact]
    public void TradeRoute_WithSegments_CalculatesTotalTime()
    {
        var segments = new[]
        {
            RouteSegment.CreateSpaceTravel(Vector3.Zero, new Vector3(600, 0, 0), true), // 1s
            RouteSegment.CreateSpaceTravel(new Vector3(600, 0, 0), new Vector3(1200, 0, 0), true) // 1s
        };

        var route = new TradeRoute(null, null, segments);

        Assert.Equal(2.0, route.TotalEstimatedTime, precision: 2);
        Assert.Equal(2, route.Segments.Count);
    }

    [Fact]
    public void TradeRoute_NoTradelaneSegments_UsesTradelanes_False()
    {
        var segments = new[]
        {
            RouteSegment.CreateSpaceTravel(Vector3.Zero, new Vector3(600, 0, 0), true)
        };

        var route = new TradeRoute(null, null, segments);

        Assert.False(route.UsesTradelanes);
    }

    [Fact]
    public void TradeRoute_ToString_ContainsSegmentInfo()
    {
        var segments = new[]
        {
            RouteSegment.CreateSpaceTravel(Vector3.Zero, new Vector3(600, 0, 0), true)
        };

        var route = new TradeRoute(null, null, segments);
        var str = route.ToString();

        Assert.Contains("TradeRoute", str);
        Assert.Contains("1 segments", str);
    }

    #endregion

    #region RouteSegmentType Tests

    [Fact]
    public void RouteSegmentType_HasExpectedValues()
    {
        // Verify enum values exist
        Assert.True(Enum.IsDefined(typeof(RouteSegmentType), RouteSegmentType.SpaceTravel));
        Assert.True(Enum.IsDefined(typeof(RouteSegmentType), RouteSegmentType.Tradelane));
        Assert.True(Enum.IsDefined(typeof(RouteSegmentType), RouteSegmentType.Dock));
    }

    [Fact]
    public void RouteSegmentType_ValuesAreDistinct()
    {
        Assert.NotEqual(RouteSegmentType.SpaceTravel, RouteSegmentType.Tradelane);
        Assert.NotEqual(RouteSegmentType.Tradelane, RouteSegmentType.Dock);
        Assert.NotEqual(RouteSegmentType.SpaceTravel, RouteSegmentType.Dock);
    }

    #endregion

    #region Time Calculation Tests

    [Theory]
    [InlineData(0, 0, 0, 1000, 0, 0)]      // 1000 units X
    [InlineData(0, 0, 0, 0, 1000, 0)]      // 1000 units Y
    [InlineData(0, 0, 0, 0, 0, 1000)]      // 1000 units Z
    public void RouteSegment_CalculatesDistanceCorrectly(
        float x1, float y1, float z1,
        float x2, float y2, float z2)
    {
        var start = new Vector3(x1, y1, z1);
        var end = new Vector3(x2, y2, z2);
        float expectedDistance = Vector3.Distance(start, end);

        var segment = RouteSegment.CreateSpaceTravel(start, end, useCruise: true);

        // Time = Distance / Speed, so Distance = Time * Speed
        float actualDistance = (float)(segment.EstimatedTime * TradeConstants.CRUISE_SPEED);
        Assert.Equal(expectedDistance, actualDistance, precision: 1);
    }

    [Fact]
    public void TradelaneTime_IsFasterThanCruise()
    {
        // For same distance, tradelane should be much faster
        float distance = 10000f;
        double cruiseTime = distance / TradeConstants.CRUISE_SPEED;
        double tradelaneTime = distance / TradeConstants.TRADELANE_SPEED;

        Assert.True(tradelaneTime < cruiseTime);
        Assert.True(tradelaneTime < cruiseTime * 0.5); // Should be at least 2x faster
    }

    #endregion
}
