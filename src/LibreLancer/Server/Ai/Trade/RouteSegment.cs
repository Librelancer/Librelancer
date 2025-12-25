// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.World;
using static LibreLancer.Server.Ai.Trade.TradeConstants;

namespace LibreLancer.Server.Ai.Trade
{
    /// <summary>
    /// Implementation of a trade route segment.
    /// </summary>
    public class RouteSegment : IRouteSegment
    {
        public RouteSegmentType Type { get; }
        public Vector3 StartPosition { get; }
        public Vector3 EndPosition { get; }
        public double EstimatedTime { get; }
        public GameObject TradelaneEntry { get; }
        public GameObject TradelaneExit { get; }
        public GameObject DockTarget { get; }

        private RouteSegment(
            RouteSegmentType type,
            Vector3 start,
            Vector3 end,
            double time,
            GameObject tlEntry = null,
            GameObject tlExit = null,
            GameObject dockTarget = null)
        {
            Type = type;
            StartPosition = start;
            EndPosition = end;
            EstimatedTime = time;
            TradelaneEntry = tlEntry;
            TradelaneExit = tlExit;
            DockTarget = dockTarget;
        }

        /// <summary>
        /// Create a space travel segment (no tradelane).
        /// </summary>
        public static RouteSegment CreateSpaceTravel(Vector3 start, Vector3 end, bool useCruise = true)
        {
            float distance = Vector3.Distance(start, end);
            float speed = useCruise ? CRUISE_SPEED : NORMAL_SPEED;
            double time = distance / speed;

            return new RouteSegment(
                RouteSegmentType.SpaceTravel,
                start,
                end,
                time);
        }

        /// <summary>
        /// Create a tradelane segment.
        /// </summary>
        public static RouteSegment CreateTradelane(GameObject entry, GameObject exit)
        {
            var start = entry.WorldTransform.Position;
            var end = exit.WorldTransform.Position;
            float distance = Vector3.Distance(start, end);
            double time = distance / TRADELANE_SPEED;

            return new RouteSegment(
                RouteSegmentType.Tradelane,
                start,
                end,
                time,
                entry,
                exit);
        }

        /// <summary>
        /// Create a dock segment.
        /// </summary>
        public static RouteSegment CreateDock(Vector3 start, GameObject target)
        {
            var end = target.WorldTransform.Position;
            float distance = Vector3.Distance(start, end);
            double time = distance / NORMAL_SPEED + 10.0; // Add docking time

            return new RouteSegment(
                RouteSegmentType.Dock,
                start,
                end,
                time,
                dockTarget: target);
        }

        public override string ToString()
        {
            return Type switch
            {
                RouteSegmentType.SpaceTravel => $"Space({Vector3.Distance(StartPosition, EndPosition):F0}m, {EstimatedTime:F1}s)",
                RouteSegmentType.Tradelane => $"TL({TradelaneEntry?.Nickname ?? "?"}->{TradelaneExit?.Nickname ?? "?"}, {EstimatedTime:F1}s)",
                RouteSegmentType.Dock => $"Dock({DockTarget?.Nickname ?? "?"}, {EstimatedTime:F1}s)",
                _ => $"Segment({Type})"
            };
        }
    }

    /// <summary>
    /// Implementation of a complete trade route.
    /// </summary>
    public class TradeRoute : ITradeRoute
    {
        public GameObject Origin { get; }
        public GameObject Destination { get; }
        public IReadOnlyList<IRouteSegment> Segments { get; }
        public double TotalEstimatedTime { get; }
        public bool UsesTradelanes { get; }
        public bool IsValid { get; }

        public TradeRoute(
            GameObject origin,
            GameObject destination,
            IEnumerable<IRouteSegment> segments)
        {
            Origin = origin;
            Destination = destination;
            Segments = segments?.ToList() ?? new List<IRouteSegment>();
            TotalEstimatedTime = Segments.Sum(s => s.EstimatedTime);
            UsesTradelanes = Segments.Any(s => s.Type == RouteSegmentType.Tradelane);
            IsValid = Origin != null &&
                      Destination != null &&
                      Segments.Count > 0;
        }

        /// <summary>
        /// Create a direct route with no tradelanes.
        /// </summary>
        public static TradeRoute CreateDirect(GameObject origin, GameObject destination)
        {
            var segments = new List<IRouteSegment>
            {
                RouteSegment.CreateSpaceTravel(
                    origin.WorldTransform.Position,
                    destination.WorldTransform.Position,
                    useCruise: true),
                RouteSegment.CreateDock(
                    destination.WorldTransform.Position,
                    destination)
            };

            return new TradeRoute(origin, destination, segments);
        }

        public override string ToString()
        {
            return $"TradeRoute({Origin?.Nickname ?? "?"} -> {Destination?.Nickname ?? "?"}, " +
                   $"{Segments.Count} segments, {TotalEstimatedTime:F0}s, TL={UsesTradelanes})";
        }
    }
}
