// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.World;

namespace LibreLancer.Server.Ai.Trade
{
    /// <summary>
    /// Represents a segment of a trade route.
    /// </summary>
    public interface IRouteSegment
    {
        /// <summary>
        /// Type of travel for this segment.
        /// </summary>
        RouteSegmentType Type { get; }

        /// <summary>
        /// Starting position of this segment.
        /// </summary>
        Vector3 StartPosition { get; }

        /// <summary>
        /// Ending position of this segment.
        /// </summary>
        Vector3 EndPosition { get; }

        /// <summary>
        /// Estimated travel time in seconds.
        /// </summary>
        double EstimatedTime { get; }

        /// <summary>
        /// For tradelane segments: the entry ring object.
        /// </summary>
        GameObject TradelaneEntry { get; }

        /// <summary>
        /// For tradelane segments: the exit ring object.
        /// </summary>
        GameObject TradelaneExit { get; }

        /// <summary>
        /// For dock segments: the target base.
        /// </summary>
        GameObject DockTarget { get; }
    }

    /// <summary>
    /// Types of route segments.
    /// </summary>
    public enum RouteSegmentType
    {
        /// <summary>Flying through space without tradelane.</summary>
        SpaceTravel,
        /// <summary>Using a tradelane for fast transit.</summary>
        Tradelane,
        /// <summary>Docking at a base.</summary>
        Dock
    }

    /// <summary>
    /// Represents a complete trade route from origin to destination.
    /// </summary>
    public interface ITradeRoute
    {
        /// <summary>
        /// Origin base where the route starts.
        /// </summary>
        GameObject Origin { get; }

        /// <summary>
        /// Destination base where the route ends.
        /// </summary>
        GameObject Destination { get; }

        /// <summary>
        /// Ordered list of route segments.
        /// </summary>
        IReadOnlyList<IRouteSegment> Segments { get; }

        /// <summary>
        /// Total estimated travel time in seconds.
        /// </summary>
        double TotalEstimatedTime { get; }

        /// <summary>
        /// Whether the route uses any tradelanes.
        /// </summary>
        bool UsesTradelanes { get; }

        /// <summary>
        /// Whether the route is valid and traversable.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// Builds trade routes between bases.
    /// </summary>
    public interface ITradeRouteBuilder
    {
        /// <summary>
        /// Build a route from origin to destination.
        /// </summary>
        /// <param name="world">The game world containing objects.</param>
        /// <param name="origin">Starting base.</param>
        /// <param name="destination">Target base.</param>
        /// <returns>A trade route, or null if no route is possible.</returns>
        ITradeRoute BuildRoute(GameWorld world, GameObject origin, GameObject destination);

        /// <summary>
        /// Check if a direct route (no tradelanes) would be faster.
        /// </summary>
        /// <param name="world">The game world.</param>
        /// <param name="origin">Starting position.</param>
        /// <param name="destination">Target position.</param>
        /// <returns>True if direct flight is preferable.</returns>
        bool ShouldFlyDirect(GameWorld world, Vector3 origin, Vector3 destination);
    }
}
