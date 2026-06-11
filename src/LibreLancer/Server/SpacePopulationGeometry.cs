using System;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.World;
using Zone = LibreLancer.Data.GameData.World.Zone;

namespace LibreLancer.Server;

public partial class SpacePopulationManager
{
    private bool TryFindSpawnPoint(
        Zone zone,
        GameObject[] players,
        float zoneCreationDistance,
        out Vector3 point)
    {
        point = Vector3.Zero;
        var maxDistance = zoneCreationDistance > 0 ? zoneCreationDistance : DefaultSpawnMaxDistance;
        var minDistance = Math.Min(DefaultSpawnMinDistance, maxDistance * 0.5f);

        for (int i = 0; i < 64; i++)
        {
            var player = players[random.Next(players.Length)];
            var distance = Lerp(minDistance, maxDistance, random.NextSingle());
            var candidate = player.WorldTransform.Position + RandomUnitVector() * distance;
            if (zone.ContainsPoint(candidate))
            {
                point = candidate;
                return true;
            }
        }

        for (int i = 0; i < 32; i++)
        {
            var candidate = SampleZonePoint(zone);
            var distance = DistanceToNearestPlayer(candidate, players);
            if (distance <= maxDistance * 1.5f)
            {
                point = candidate;
                return true;
            }
        }

        return false;
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
            Matrix4x4.CreateWorld(Vector3.Zero, -direction, up)));
    }
}
