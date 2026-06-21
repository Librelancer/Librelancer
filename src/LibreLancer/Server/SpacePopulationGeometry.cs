using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private const float SpawnVerticalBand = 200f;
    private const float PatrolPathSpawnMinDistance = 2500f;
    private const float PatrolPathSpawnMaxDistance = 10000f;
    private const float PatrolPathSpawnDistanceScale = 14f;
    private const float PatrolPathPersistBuffer = 500f;
    private const float PatrolPathLineDistanceTolerance = 500f;

    private readonly record struct PatrolPathSpawnCandidate(
        Vector3 Start,
        Vector3 End,
        int PathIndex,
        Vector3 PlayerPosition,
        float StartT,
        float EndT,
        float Length,
        float LineDistance);

    private bool TryFindPatrolPathSpawnLocation(
        ZoneState state,
        GameObject[] players,
        float zoneCreationDistance,
        out SpawnLocation spawn)
    {
        spawn = default;
        var path = state.Path;
        if (path == null || path.Count < 2 || players.Length == 0)
            return false;

        var maxDistance = zoneCreationDistance > 0
            ? zoneCreationDistance * PatrolPathSpawnDistanceScale
            : PatrolPathSpawnMaxDistance;
        if (maxDistance <= PatrolPathSpawnMinDistance)
            maxDistance = PatrolPathSpawnMaxDistance;

        var candidates = new List<PatrolPathSpawnCandidate>();
        AddPatrolPathSpawnCandidates(state, players, maxDistance, candidates);

        if (!ChoosePatrolPathSpawnCandidate(candidates, out var candidateInfo))
        {
            spawn = default;
            return false;
        }

        var candidate = SamplePatrolPathSpawnPoint(candidateInfo, players, maxDistance);
        var approachTarget = ClosestPointOnSegment(
            candidateInfo.Start,
            candidateInfo.End,
            candidateInfo.PlayerPosition);
        spawn = new SpawnLocation(
            candidate,
            Quaternion.Identity,
            null,
            0,
            candidateInfo.PathIndex,
            approachTarget,
            maxDistance + PatrolPathPersistBuffer);
        return true;
    }

    private void AddPatrolPathSpawnCandidates(
        ZoneState state,
        GameObject[] players,
        float maxDistance,
        List<PatrolPathSpawnCandidate> candidates)
    {
        var path = state.Path;
        if (path is { Count: > 0 })
        {
            var count = candidates.Count;
            for (int i = 0; i < path.Count; i++)
                AddPatrolZoneSpawnCandidates(players, candidates, path, i, maxDistance);

            if (candidates.Count > count)
                return;

            for (int i = 0; i + 1 < path.Count; i++)
                AddPatrolPathSpawnCandidates(players, candidates, path[i].Zone.Position, path[i + 1].Zone.Position, i, maxDistance);
            return;
        }

        AddPatrolZoneSpawnCandidates(players, candidates, state.Zone, state.PathIndex, maxDistance);
    }

    private void AddPatrolZoneSpawnCandidates(
        GameObject[] players,
        List<PatrolPathSpawnCandidate> candidates,
        List<PatrolPathSegment> path,
        int pathIndex,
        float maxDistance)
    {
        if (!TryGetPatrolPathLine(path, pathIndex, out var start, out var end))
            return;

        AddPatrolPathSpawnCandidates(players, candidates, start, end, pathIndex, maxDistance);
    }

    private void AddPatrolZoneSpawnCandidates(
        GameObject[] players,
        List<PatrolPathSpawnCandidate> candidates,
        Zone zone,
        int pathIndex,
        float maxDistance)
    {
        if (!TryGetPatrolZoneLine(zone, out var start, out var end))
            return;

        AddPatrolPathSpawnCandidates(players, candidates, start, end, pathIndex, maxDistance);
    }

    private void AddPatrolPathSpawnCandidates(
        GameObject[] players,
        List<PatrolPathSpawnCandidate> candidates,
        Vector3 start,
        Vector3 end,
        int pathIndex,
        float maxDistance)
    {
        foreach (var player in players)
        {
            AddPatrolPathSpawnCandidates(
                candidates,
                start,
                end,
                pathIndex,
                player.WorldTransform.Position,
                PatrolPathSpawnMinDistance,
                maxDistance);
        }
    }

    private void AddPatrolPathSpawnCandidates(
        List<PatrolPathSpawnCandidate> candidates,
        Vector3 a,
        Vector3 b,
        int pathIndex,
        Vector3 playerPosition,
        float minDistance,
        float maxDistance)
    {
        var segment = b - a;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 1)
            return;

        var length = MathF.Sqrt(lengthSquared);
        var closestT = Math.Clamp(Vector3.Dot(playerPosition - a, segment) / lengthSquared, 0, 1);
        var closestPoint = a + segment * closestT;
        var perpendicular = Vector3.Distance(playerPosition, closestPoint);
        if (perpendicular > maxDistance)
            return;

        var minAlong = MathF.Sqrt(MathF.Max(0, minDistance * minDistance - perpendicular * perpendicular)) / length;
        var maxAlong = MathF.Sqrt(MathF.Max(0, maxDistance * maxDistance - perpendicular * perpendicular)) / length;
        if (maxAlong <= 0)
            return;

        AddPatrolPathSpawnCandidate(candidates, a, b, pathIndex, playerPosition, closestT - maxAlong, closestT - minAlong, length, perpendicular);
    }

    private static void AddPatrolPathSpawnCandidate(
        List<PatrolPathSpawnCandidate> candidates,
        Vector3 start,
        Vector3 end,
        int pathIndex,
        Vector3 playerPosition,
        float startT,
        float endT,
        float segmentLength,
        float lineDistance)
    {
        startT = Math.Clamp(startT, 0, 1);
        endT = Math.Clamp(endT, 0, 1);
        if (endT <= startT)
            return;

        candidates.Add(new PatrolPathSpawnCandidate(
            start,
            end,
            pathIndex,
            playerPosition,
            startT,
            endT,
            (endT - startT) * segmentLength,
            lineDistance));
    }

    private bool ChoosePatrolPathSpawnCandidate(
        List<PatrolPathSpawnCandidate> candidates,
        out PatrolPathSpawnCandidate selected)
    {
        selected = default;
        var closestLine = float.MaxValue;
        foreach (var candidate in candidates)
            closestLine = MathF.Min(closestLine, candidate.LineDistance);
        if (closestLine == float.MaxValue)
            return false;

        var maxLineDistance = closestLine + PatrolPathLineDistanceTolerance;

        var totalLength = 0f;
        foreach (var candidate in candidates)
        {
            if (candidate.LineDistance <= maxLineDistance &&
                candidate.Length > 0)
            {
                totalLength += candidate.Length;
            }
        }
        if (totalLength <= 0)
            return false;

        PatrolPathSpawnCandidate? fallback = null;
        var roll = random.NextSingle() * totalLength;
        foreach (var candidate in candidates)
        {
            if (candidate.LineDistance > maxLineDistance ||
                candidate.Length <= 0)
            {
                continue;
            }

            fallback ??= candidate;
            roll -= candidate.Length;
            if (roll <= 0)
            {
                selected = candidate;
                return true;
            }
        }

        selected = fallback.GetValueOrDefault();
        return fallback.HasValue;
    }

    private Vector3 SamplePatrolPathSpawnPoint(
        PatrolPathSpawnCandidate candidate,
        GameObject[] players,
        float maxDistance)
    {
        var t = Lerp(candidate.StartT, candidate.EndT, random.NextSingle());
        var point = Vector3.Lerp(candidate.Start, candidate.End, t);
        for (int i = 0; i < 4; i++)
        {
            var playerPosition = GetNearestPlayer(point, players).WorldTransform.Position;
            var distance = Vector3.Distance(point, playerPosition);
            if (distance + 1 >= PatrolPathSpawnMinDistance && distance <= maxDistance)
                return point;

            point = MovePatrolSpawnIntoRange(
                candidate,
                point,
                playerPosition,
                distance < PatrolPathSpawnMinDistance ? PatrolPathSpawnMinDistance : maxDistance);
        }
        return point;
    }

    private Vector3 MovePatrolSpawnIntoRange(
        PatrolPathSpawnCandidate candidate,
        Vector3 point,
        Vector3 playerPosition,
        float targetDistance)
    {
        var segment = candidate.End - candidate.Start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 1)
            return candidate.Start;

        var length = MathF.Sqrt(lengthSquared);
        var closestT = Math.Clamp(Vector3.Dot(playerPosition - candidate.Start, segment) / lengthSquared, 0, 1);
        var currentT = Math.Clamp(Vector3.Dot(point - candidate.Start, segment) / lengthSquared, 0, 1);
        var closestPoint = candidate.Start + segment * closestT;
        var perpendicular = Vector3.Distance(playerPosition, closestPoint);
        if (perpendicular >= targetDistance)
            return closestPoint;

        var offset = MathF.Sqrt(MathF.Max(0, targetDistance * targetDistance - perpendicular * perpendicular)) / length;
        var sign = currentT >= closestT ? 1 : -1;
        if (MathF.Abs(currentT - closestT) < 0.0001f)
            sign = -1;

        var targetT = closestT + sign * offset;
        targetT = Math.Clamp(targetT, candidate.StartT, candidate.EndT);

        return Vector3.Lerp(candidate.Start, candidate.End, Math.Clamp(targetT, 0, 1));
    }

    private bool TryFindSpawnPoint(
        Zone zone,
        GameObject[] players,
        float zoneCreationDistance,
        bool allowCloseSpawn,
        out Vector3 point)
    {
        point = Vector3.Zero;
        var maxDistance = Math.Max(
            zoneCreationDistance > 0 ? zoneCreationDistance : DefaultSpawnMaxDistance,
            DefaultSpawnMaxDistance);
        var minDistance = allowCloseSpawn
            ? Math.Min(DefaultSpawnMinDistance, maxDistance * 0.5f)
            : DefaultSpawnMaxDistance;

        for (int i = 0; i < 64; i++)
        {
            var player = players[random.Next(players.Length)];
            var distance = Lerp(minDistance, maxDistance, random.NextSingle());
            var candidate = player.WorldTransform.Position + RandomUnitVector() * distance;
            candidate = ClampSpawnHeight(candidate, player.WorldTransform.Position);
            if (zone.ContainsPoint(candidate))
            {
                point = candidate;
                return true;
            }
        }

        for (int i = 0; i < 32; i++)
        {
            var sampled = SampleZonePoint(zone);
            var player = GetNearestPlayer(sampled, players);
            var candidate = ClampSpawnHeight(sampled, player.WorldTransform.Position);
            var distance = DistanceToNearestPlayer(candidate, players);
            if (distance >= minDistance &&
                distance <= maxDistance * 1.5f &&
                zone.ContainsPoint(candidate))
            {
                point = candidate;
                return true;
            }
        }

        return false;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        var segment = b - a;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared < 1)
            return a;

        var t = Math.Clamp(Vector3.Dot(point - a, segment) / lengthSquared, 0, 1);
        return a + segment * t;
    }

    private float DistanceToNearestPlayer(Vector3 point, GameObject[] players)
    {
        var nearest = float.MaxValue;
        foreach (var player in players)
        {
            var distance = Vector3.Distance(point, player.WorldTransform.Position);
            if (distance < nearest)
                nearest = distance;
        }
        return nearest;
    }

    private GameObject GetNearestPlayer(Vector3 point, GameObject[] players)
    {
        var nearest = players[0];
        var nearestDistance = Vector3.DistanceSquared(point, nearest.WorldTransform.Position);
        for (int i = 1; i < players.Length; i++)
        {
            var distance = Vector3.DistanceSquared(point, players[i].WorldTransform.Position);
            if (distance < nearestDistance)
            {
                nearest = players[i];
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private static Vector3 ClampSpawnHeight(Vector3 candidate, Vector3 playerPosition)
    {
        candidate.Y = playerPosition.Y + Math.Clamp(candidate.Y - playerPosition.Y, -SpawnVerticalBand, SpawnVerticalBand);
        return candidate;
    }

    private Vector3 SampleZonePoint(Zone zone)
    {
        for (int i = 0; i < 16; i++)
        {
            var candidate = SampleZonePointUnchecked(zone);
            if (zone.ContainsPoint(candidate))
                return candidate;
        }
        return zone.Position;
    }

    private Vector3 SampleZonePointUnchecked(Zone zone)
    {
        return zone.Shape switch
        {
            ShapeKind.Sphere => zone.Position + RandomUnitVector() * (zone.Size.X * MathF.Cbrt(random.NextSingle())),
            ShapeKind.Ellipsoid => TransformZoneLocal(zone, RandomUnitVector() * MathF.Cbrt(random.NextSingle()) * zone.Size),
            ShapeKind.Box => TransformZoneLocal(zone, new Vector3(
                (random.NextSingle() - 0.5f) * zone.Size.X,
                (random.NextSingle() - 0.5f) * zone.Size.Y,
                (random.NextSingle() - 0.5f) * zone.Size.Z)),
            ShapeKind.Cylinder => SampleCylinder(zone, 0),
            ShapeKind.Ring => SampleCylinder(zone, zone.Size.Z),
            _ => zone.Position
        };
    }

    private Vector3 SampleCylinder(Zone zone, float innerRadius)
    {
        var angle = random.NextSingle() * MathF.PI * 2f;
        var radius = MathF.Sqrt(Lerp(innerRadius * innerRadius, zone.Size.X * zone.Size.X, random.NextSingle()));
        var local = new Vector3(
            MathF.Cos(angle) * radius,
            (random.NextSingle() - 0.5f) * zone.Size.Y,
            MathF.Sin(angle) * radius);
        return TransformZoneLocal(zone, local);
    }

    private static Vector3 TransformZoneLocal(Zone zone, Vector3 local) =>
        zone.Position + Vector3.Transform(local, zone.RotationMatrix);

    private static bool TryGetPatrolZoneLine(Zone zone, out Vector3 start, out Vector3 end)
    {
        if (zone.Shape is ShapeKind.Cylinder or ShapeKind.Ring)
        {
            start = TransformZoneLocal(zone, new Vector3(0, zone.Size.Y * -0.5f, 0));
            end = TransformZoneLocal(zone, new Vector3(0, zone.Size.Y * 0.5f, 0));
            return true;
        }

        start = Vector3.Zero;
        end = Vector3.Zero;
        return false;
    }

    private static bool TryGetPatrolPathLine(List<PatrolPathSegment> path, int index, out Vector3 start, out Vector3 end)
    {
        start = Vector3.Zero;
        end = Vector3.Zero;
        if (index < 0 || index >= path.Count)
            return false;

        if (!TryGetPatrolZoneLine(path[index].Zone, out start, out end))
        {
            if (index + 1 < path.Count)
            {
                start = path[index].Zone.Position;
                end = path[index + 1].Zone.Position;
                return true;
            }
            if (index > 0)
            {
                start = path[index - 1].Zone.Position;
                end = path[index].Zone.Position;
                return true;
            }
            return false;
        }

        OrientPatrolPathLine(path, index, ref start, ref end);
        return true;
    }

    private static void OrientPatrolPathLine(List<PatrolPathSegment> path, int index, ref Vector3 start, ref Vector3 end)
    {
        if (!TryGetPatrolConnectionScore(path, index, start, end, out var forwardScore) ||
            !TryGetPatrolConnectionScore(path, index, end, start, out var reverseScore) ||
            forwardScore <= reverseScore)
        {
            return;
        }

        (start, end) = (end, start);
    }

    private static bool TryGetPatrolConnectionScore(
        List<PatrolPathSegment> path,
        int index,
        Vector3 start,
        Vector3 end,
        out float score)
    {
        score = 0;
        var scored = false;

        if (TryGetPatrolPrevious(path, index, out var previous))
        {
            score += DistanceToPatrolConnection(start, path[previous].Zone);
            scored = true;
        }

        if (TryGetPatrolNext(path, index, out var next))
        {
            score += DistanceToPatrolConnection(end, path[next].Zone);
            scored = true;
        }

        return scored;
    }

    private static bool TryGetPatrolPrevious(List<PatrolPathSegment> path, int index, out int previous)
    {
        previous = index - 1;
        if (previous >= 0)
            return true;

        if (!IsClosedPath(path))
            return false;

        previous = path.Count - 1;
        return previous != index;
    }

    private static bool TryGetPatrolNext(List<PatrolPathSegment> path, int index, out int next)
    {
        next = index + 1;
        if (next < path.Count)
            return true;

        if (!IsClosedPath(path))
            return false;

        next = 0;
        return next != index;
    }

    private static float DistanceToPatrolConnection(Vector3 point, Zone zone)
    {
        if (!TryGetPatrolZoneLine(zone, out var start, out var end))
            return Vector3.DistanceSquared(point, zone.Position);

        return MathF.Min(
            Vector3.DistanceSquared(point, start),
            Vector3.DistanceSquared(point, end));
    }

    private Vector3 RandomUnitVector()
    {
        var z = random.NextSingle() * 2f - 1f;
        var angle = random.NextSingle() * MathF.PI * 2f;
        var radius = MathF.Sqrt(MathF.Max(0, 1f - z * z));
        return new Vector3(MathF.Cos(angle) * radius, z, MathF.Sin(angle) * radius);
    }

    private static Quaternion LookRotation(Vector3 direction)
    {
        if (direction.LengthSquared() < 0.0001f)
            return Quaternion.Identity;

        direction = Vector3.Normalize(direction);
        var up = Math.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.95f
            ? Vector3.UnitZ
            : Vector3.UnitY;
        return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(
            Matrix4x4.CreateWorld(Vector3.Zero, direction, up)));
    }
}
