using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.Client;

public static class NavmapBestPathPlanner
{
    private const float TradelaneSpeed = 2500f;
    private const float JumpTransitionSeconds = 15f;
    private const float TradelanePreferenceMargin = 1f;

    private enum NodeKind
    {
        Origin,
        Destination,
        Jump,
        TradelaneEndpoint
    }

    private enum EdgeKind
    {
        Direct,
        Jump,
        TradelaneTraversal
    }

    private sealed class Node
    {
        public required int Id;
        public required StarSystem System;
        public required Vector3 Position;
        public required NodeKind Kind;
        public uint? ObjectHash;
    }

    private readonly record struct Edge(int To, float Cost, EdgeKind Kind);

    private sealed class JumpLink
    {
        public required SystemObject SourceObject;
        public required StarSystem SourceSystem;
        public required SystemObject TargetObject;
        public required StarSystem TargetSystem;
    }

    private sealed class TradelaneRoute
    {
        public required SystemObject SourceObject;
        public required SystemObject TargetObject;
    }

    public static List<UserWaypoint> Compute(
        IEnumerable<StarSystem> allSystems,
        StarSystem currentSystem,
        Vector3 currentPosition,
        StarSystem destinationSystem,
        Vector3 destinationPosition,
        Func<uint, bool> isVisited,
        float cruiseSpeed)
    {
        cruiseSpeed = MathF.Max(1f, cruiseSpeed);

        var systems = allSystems.ToDictionary(x => x.Nickname, StringComparer.OrdinalIgnoreCase);
        var systemObjects = systems.Values.ToDictionary(
            x => x.CRC,
            x => x.Objects.ToDictionary(o => o.Nickname, StringComparer.OrdinalIgnoreCase));
        var graph = new Dictionary<int, List<Edge>>();
        var nodes = new List<Node>();
        var systemNodes = new Dictionary<uint, List<int>>();
        var jumpNodes = new Dictionary<(uint SystemHash, uint ObjectHash), int>();
        var tradelaneNodes = new Dictionary<(uint SystemHash, uint ObjectHash), int>();

        int AddNode(StarSystem system, Vector3 position, NodeKind kind, uint? objectHash = null)
        {
            var id = nodes.Count;
            nodes.Add(new Node
            {
                Id = id,
                System = system,
                Position = position,
                Kind = kind,
                ObjectHash = objectHash
            });
            graph[id] = [];
            if (!systemNodes.TryGetValue(system.CRC, out var list))
            {
                list = [];
                systemNodes[system.CRC] = list;
            }
            list.Add(id);
            return id;
        }

        void AddEdge(int from, int to, float cost, EdgeKind kind, bool bidirectional = true)
        {
            graph[from].Add(new Edge(to, cost, kind));
            if (bidirectional)
                graph[to].Add(new Edge(from, cost, kind));
        }

        int GetOrAddJumpNode(StarSystem system, SystemObject obj)
        {
            var hash = FLHash.CreateID(obj.Nickname);
            var key = (system.CRC, hash);
            if (!jumpNodes.TryGetValue(key, out var id))
            {
                id = AddNode(system, obj.Position, NodeKind.Jump, hash);
                jumpNodes[key] = id;
            }
            return id;
        }

        int GetOrAddTradelaneNode(StarSystem system, SystemObject obj)
        {
            var hash = FLHash.CreateID(obj.Nickname);
            var key = (system.CRC, hash);
            if (!tradelaneNodes.TryGetValue(key, out var id))
            {
                id = AddNode(system, obj.Position, NodeKind.TradelaneEndpoint, hash);
                tradelaneNodes[key] = id;
            }
            return id;
        }

        var originId = AddNode(currentSystem, currentPosition, NodeKind.Origin);
        var destinationId = AddNode(destinationSystem, destinationPosition, NodeKind.Destination);

        foreach (var link in BuildJumpLinks(systems, systemObjects, isVisited))
        {
            var sourceId = GetOrAddJumpNode(link.SourceSystem, link.SourceObject);
            var targetId = GetOrAddJumpNode(link.TargetSystem, link.TargetObject);
            AddEdge(sourceId, targetId, JumpTransitionSeconds, EdgeKind.Jump, bidirectional: false);
        }

        foreach (var system in systems.Values.Where(x => isVisited(x.CRC) || x == currentSystem || x == destinationSystem))
        {
            foreach (var lane in BuildTradelaneRoutes(system, systemObjects[system.CRC]))
            {
                var startId = GetOrAddTradelaneNode(system, lane.SourceObject);
                var endId = GetOrAddTradelaneNode(system, lane.TargetObject);
                var travelTime = TravelTime(lane.SourceObject.Position, lane.TargetObject.Position, TradelaneSpeed) + TradelanePreferenceMargin;
                AddEdge(startId, endId, travelTime, EdgeKind.TradelaneTraversal, bidirectional: false);
            }
        }

        foreach (var ids in systemNodes.Values)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                for (var j = i + 1; j < ids.Count; j++)
                {
                    var a = nodes[ids[i]];
                    var b = nodes[ids[j]];
                    var time = TravelTime(a.Position, b.Position, cruiseSpeed);
                    AddEdge(a.Id, b.Id, time, EdgeKind.Direct);
                }
            }
        }

        if (!TryFindPath(graph, originId, destinationId, out var path, out var edgeKinds))
            return [];

        var waypoints = new List<UserWaypoint>();
        for (var i = 1; i < path.Count;)
        {
            var node = nodes[path[i]];
            var incoming = edgeKinds[i - 1];

            if (node.Kind == NodeKind.Destination)
            {
                waypoints.Add(new UserWaypoint(node.System.CRC, node.Position, UserWaypointKind.ManualDestination));
                i++;
                continue;
            }

            if (incoming == EdgeKind.Jump)
            {
                i++;
                continue;
            }

            if (node.Kind == NodeKind.Jump)
            {
                waypoints.Add(new UserWaypoint(
                    node.System.CRC,
                    node.Position,
                    UserWaypointKind.JumpEntry,
                    node.ObjectHash));
                i++;
                continue;
            }

            if (node.Kind == NodeKind.TradelaneEndpoint && incoming != EdgeKind.TradelaneTraversal)
            {
                waypoints.Add(new UserWaypoint(
                    node.System.CRC,
                    node.Position,
                    UserWaypointKind.TradelaneEntry,
                    node.ObjectHash));
                var exitIndex = i;
                while (exitIndex < edgeKinds.Count && edgeKinds[exitIndex] == EdgeKind.TradelaneTraversal)
                    exitIndex++;
                var exitNode = nodes[path[exitIndex]];
                if (exitNode.Kind == NodeKind.TradelaneEndpoint)
                {
                    waypoints.Add(new UserWaypoint(
                        exitNode.System.CRC,
                        exitNode.Position,
                        UserWaypointKind.TradelaneExit,
                        exitNode.ObjectHash));
                }
                i = exitIndex + 1;
                continue;
            }

            i++;
        }

        return waypoints;
    }

    private static float TravelTime(Vector3 a, Vector3 b, float speed) =>
        Vector3.Distance(a, b) / MathF.Max(1f, speed);

    private static bool TryFindPath(
        Dictionary<int, List<Edge>> graph,
        int origin,
        int destination,
        out List<int> nodePath,
        out List<EdgeKind> edgeKinds)
    {
        var queue = new PriorityQueue<int, float>();
        var distances = new Dictionary<int, float>();
        var previous = new Dictionary<int, (int Node, EdgeKind Kind)>();

        foreach (var node in graph.Keys)
            distances[node] = float.PositiveInfinity;
        distances[origin] = 0f;
        queue.Enqueue(origin, 0f);

        while (queue.TryDequeue(out var node, out var cost))
        {
            if (cost > distances[node])
                continue;
            if (node == destination)
                break;

            foreach (var edge in graph[node])
            {
                var next = cost + edge.Cost;
                if (next >= distances[edge.To])
                    continue;

                distances[edge.To] = next;
                previous[edge.To] = (node, edge.Kind);
                queue.Enqueue(edge.To, next);
            }
        }

        if (!previous.ContainsKey(destination) && origin != destination)
        {
            nodePath = [];
            edgeKinds = [];
            return false;
        }

        nodePath = [destination];
        edgeKinds = [];
        var current = destination;
        while (current != origin)
        {
            var prev = previous[current];
            edgeKinds.Add(prev.Kind);
            current = prev.Node;
            nodePath.Add(current);
        }

        nodePath.Reverse();
        edgeKinds.Reverse();
        return true;
    }

    private static IEnumerable<JumpLink> BuildJumpLinks(
        Dictionary<string, StarSystem> systems,
        Dictionary<uint, Dictionary<string, SystemObject>> systemObjects,
        Func<uint, bool> isVisited)
    {
        foreach (var system in systems.Values)
        {
            if (!isVisited(system.CRC))
                continue;

            foreach (var obj in system.Objects)
            {
                if (obj.Dock?.Kind != DockKinds.Jump || string.IsNullOrWhiteSpace(obj.Dock.Target) ||
                    string.IsNullOrWhiteSpace(obj.Dock.Exit))
                    continue;

                var sourceHash = FLHash.CreateID(obj.Nickname);
                if (!isVisited(sourceHash))
                    continue;

                if (!systems.TryGetValue(obj.Dock.Target, out var targetSystem) || !isVisited(targetSystem.CRC))
                    continue;

                if (!systemObjects[targetSystem.CRC].TryGetValue(obj.Dock.Exit, out var targetObject))
                    continue;

                var targetHash = FLHash.CreateID(targetObject.Nickname);
                if (!isVisited(targetHash))
                    continue;

                yield return new JumpLink
                {
                    SourceObject = obj,
                    SourceSystem = system,
                    TargetObject = targetObject,
                    TargetSystem = targetSystem
                };
            }
        }
    }

    private static IEnumerable<TradelaneRoute> BuildTradelaneRoutes(
        StarSystem system,
        Dictionary<string, SystemObject> systemObjects)
    {
        foreach (var obj in system.Objects)
        {
            if (obj.Dock is not { Kind: DockKinds.Tradelane })
                continue;

            if (!string.IsNullOrEmpty(obj.Dock.Target))
            {
                var exit = FollowTradelane(systemObjects, obj.Dock.Target, followLeft: false);
                if (exit != null && exit != obj)
                {
                    yield return new TradelaneRoute
                    {
                        SourceObject = obj,
                        TargetObject = exit
                    };
                }
            }

            if (!string.IsNullOrEmpty(obj.Dock.TargetLeft))
            {
                var exit = FollowTradelane(systemObjects, obj.Dock.TargetLeft, followLeft: true);
                if (exit != null && exit != obj)
                {
                    yield return new TradelaneRoute
                    {
                        SourceObject = obj,
                        TargetObject = exit
                    };
                }
            }
        }
    }

    private static SystemObject? FollowTradelane(
        Dictionary<string, SystemObject> systemObjects,
        string nextNickname,
        bool followLeft)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SystemObject? current = null;
        var nickname = nextNickname;
        while (!string.IsNullOrEmpty(nickname) && visited.Add(nickname))
        {
            systemObjects.TryGetValue(nickname, out current);
            if (current?.Dock is not { Kind: DockKinds.Tradelane } dock)
                break;
            nickname = followLeft ? dock.TargetLeft : dock.Target;
        }
        return current;
    }
}
