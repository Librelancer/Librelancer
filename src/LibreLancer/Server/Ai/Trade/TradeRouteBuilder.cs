// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Components;
using LibreLancer.World;
using static LibreLancer.Server.Ai.Trade.TradeConstants;

namespace LibreLancer.Server.Ai.Trade
{
    /// <summary>
    /// Builds trade routes between bases using BFS through tradelane network.
    /// </summary>
    public class TradeRouteBuilder : ITradeRouteBuilder
    {

        /// <summary>
        /// Represents a tradelane ring for graph building.
        /// </summary>
        private class TradelaneNode
        {
            public GameObject Ring { get; set; }
            public string Nickname { get; set; }
            public Vector3 Position { get; set; }
            public List<TradelaneNode> Connections { get; } = new();
        }

        public ITradeRoute BuildRoute(GameWorld world, GameObject origin, GameObject destination)
        {
            if (world == null || origin == null || destination == null)
            {
                FLLog.Warning("TradeRouteBuilder", "Invalid parameters for route building");
                return null;
            }

            var startPos = origin.WorldTransform.Position;
            var endPos = destination.WorldTransform.Position;

            // Find all tradelane rings
            var tradelanes = FindTradelaneRings(world);
            if (tradelanes.Count == 0)
            {
                FLLog.Debug("TradeRouteBuilder", "No tradelanes in system, using direct route");
                return TradeRoute.CreateDirect(origin, destination);
            }

            // Build tradelane graph
            var graph = BuildTradelaneGraph(tradelanes);

            // Find entry and exit points
            var entryNode = FindNearestNode(graph, startPos, MAX_TRADELANE_SEARCH_DISTANCE);
            var exitNode = FindNearestNode(graph, endPos, MAX_TRADELANE_SEARCH_DISTANCE);

            if (entryNode == null || exitNode == null)
            {
                FLLog.Debug("TradeRouteBuilder", "No suitable tradelane entry/exit points found");
                return TradeRoute.CreateDirect(origin, destination);
            }

            // Check if tradelane route is worth it
            if (ShouldFlyDirect(world, startPos, endPos))
            {
                FLLog.Debug("TradeRouteBuilder", "Direct flight is faster than tradelane route");
                return TradeRoute.CreateDirect(origin, destination);
            }

            // BFS to find path through tradelane network
            var path = FindPath(entryNode, exitNode);
            if (path == null || path.Count < 2)
            {
                FLLog.Debug("TradeRouteBuilder", "No path found through tradelane network");
                return TradeRoute.CreateDirect(origin, destination);
            }

            // Build route segments
            var segments = BuildSegments(startPos, endPos, path, destination);

            var route = new TradeRoute(origin, destination, segments);
            FLLog.Info("TradeRouteBuilder", $"Built route: {route}");
            return route;
        }

        public bool ShouldFlyDirect(GameWorld world, Vector3 origin, Vector3 destination)
        {
            float directDistance = Vector3.Distance(origin, destination);
            float directTime = directDistance / CRUISE_SPEED;

            // Find nearest tradelanes using manual loop to avoid LINQ allocations
            var tradelanes = FindTradelaneRings(world);
            if (tradelanes.Count == 0)
                return true;

            // Find nearest to origin
            GameObject nearestToOrigin = null;
            float distToEntry = float.MaxValue;
            foreach (var tl in tradelanes)
            {
                float dist = Vector3.Distance(origin, tl.WorldTransform.Position);
                if (dist < distToEntry)
                {
                    distToEntry = dist;
                    nearestToOrigin = tl;
                }
            }

            // Find nearest to destination
            GameObject nearestToDest = null;
            float distFromExit = float.MaxValue;
            foreach (var tl in tradelanes)
            {
                float dist = Vector3.Distance(destination, tl.WorldTransform.Position);
                if (dist < distFromExit)
                {
                    distFromExit = dist;
                    nearestToDest = tl;
                }
            }

            if (nearestToOrigin == null || nearestToDest == null)
                return true;

            // If entry/exit are too far, fly direct
            if (distToEntry > MAX_TRADELANE_SEARCH_DISTANCE || distFromExit > MAX_TRADELANE_SEARCH_DISTANCE)
                return true;

            // Rough estimate: time to entry + TL time + time from exit
            float tlDistance = Vector3.Distance(nearestToOrigin.WorldTransform.Position,
                                                 nearestToDest.WorldTransform.Position);
            float tlTime = distToEntry / NORMAL_SPEED +
                          tlDistance / TRADELANE_SPEED +
                          distFromExit / NORMAL_SPEED;

            // Use TL only if significantly faster
            return tlTime > directTime * TRADELANE_EFFICIENCY_THRESHOLD;
        }

        /// <summary>
        /// Find all tradelane rings in the world.
        /// </summary>
        public static List<GameObject> FindTradelaneRings(GameWorld world)
        {
            var rings = new List<GameObject>();

            foreach (var obj in world.Objects)
            {
                if (obj.TryGetComponent<SDockableComponent>(out var dock) &&
                    dock.Action.Kind == DockKinds.Tradelane)
                {
                    rings.Add(obj);
                }
            }

            return rings;
        }

        /// <summary>
        /// Build a graph of tradelane connections.
        /// </summary>
        private List<TradelaneNode> BuildTradelaneGraph(List<GameObject> rings)
        {
            var nodes = new Dictionary<string, TradelaneNode>(StringComparer.OrdinalIgnoreCase);

            // Create nodes for each ring
            foreach (var ring in rings)
            {
                var nickname = ring.Nickname ?? ring.GetHashCode().ToString();
                nodes[nickname] = new TradelaneNode
                {
                    Ring = ring,
                    Nickname = nickname,
                    Position = ring.WorldTransform.Position
                };
            }

            // Connect rings that are part of the same tradelane
            // Tradelanes typically have rings named like "li01_trade_lane_ring_1", "li01_trade_lane_ring_2", etc.
            // We need to detect which rings are connected by looking at dock targets
            foreach (var ring in rings)
            {
                if (!ring.TryGetComponent<SDockableComponent>(out var dock))
                    continue;

                var nickname = ring.Nickname ?? ring.GetHashCode().ToString();
                if (!nodes.TryGetValue(nickname, out var node))
                    continue;

                // Check if dock has a target (next ring in lane)
                if (!string.IsNullOrEmpty(dock.Action.Target))
                {
                    if (nodes.TryGetValue(dock.Action.Target, out var targetNode))
                    {
                        // Bidirectional connection
                        if (!node.Connections.Contains(targetNode))
                            node.Connections.Add(targetNode);
                        if (!targetNode.Connections.Contains(node))
                            targetNode.Connections.Add(node);
                    }
                }
            }

            return nodes.Values.ToList();
        }

        /// <summary>
        /// Find the nearest tradelane node within the maximum distance.
        /// </summary>
        private TradelaneNode FindNearestNode(List<TradelaneNode> nodes, Vector3 position, float maxDistance)
        {
            TradelaneNode nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var node in nodes)
            {
                float dist = Vector3.Distance(position, node.Position);
                if (dist < nearestDist && dist <= maxDistance)
                {
                    nearestDist = dist;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// BFS to find shortest path through tradelane network.
        /// </summary>
        private List<TradelaneNode> FindPath(TradelaneNode start, TradelaneNode end)
        {
            if (start == end)
                return new List<TradelaneNode> { start };

            var queue = new Queue<TradelaneNode>();
            var visited = new HashSet<TradelaneNode>();
            var parent = new Dictionary<TradelaneNode, TradelaneNode>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                {
                    // Reconstruct path - use Add() + Reverse() for O(n) instead of Insert(0) which is O(n²)
                    var path = new List<TradelaneNode>();
                    var node = end;
                    while (node != null)
                    {
                        path.Add(node);
                        parent.TryGetValue(node, out node);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in current.Connections)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return null; // No path found
        }

        /// <summary>
        /// Build route segments from path.
        /// </summary>
        private List<IRouteSegment> BuildSegments(
            Vector3 startPos,
            Vector3 endPos,
            List<TradelaneNode> path,
            GameObject destination)
        {
            var segments = new List<IRouteSegment>();

            // Segment 1: Travel to first tradelane
            if (path.Count > 0)
            {
                segments.Add(RouteSegment.CreateSpaceTravel(startPos, path[0].Position, useCruise: false));
            }

            // Segment 2+: Tradelane segments
            for (int i = 0; i < path.Count - 1; i++)
            {
                segments.Add(RouteSegment.CreateTradelane(path[i].Ring, path[i + 1].Ring));
            }

            // Segment N-1: Travel from last tradelane to near destination
            if (path.Count > 0)
            {
                var lastPos = path[^1].Position;
                segments.Add(RouteSegment.CreateSpaceTravel(lastPos, endPos, useCruise: true));
            }

            // Segment N: Dock at destination
            segments.Add(RouteSegment.CreateDock(endPos, destination));

            return segments;
        }
    }
}
