using System;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

public sealed class SDeployableComponent : GameComponent
{
    public const double LaunchCollisionSafeTime = 1.0;

    public MunitionEquip Munition { get; }
    public GameObject Owner { get; }

    private double totalTime;

    public SDeployableComponent(GameObject parent, MunitionEquip munition, GameObject owner) : base(parent)
    {
        Munition = munition;
        Owner = owner;
    }

    private Mine? Mine => Munition.Def as Mine;

    public bool IsOwnerSafe => totalTime < Munition.Def switch
    {
        Countermeasure cm => cm.OwnerSafeTime,
        Mine mine => mine.OwnerSafeTime,
        _ => 0
    };

    public bool IsCollisionSafe => totalTime < LaunchCollisionSafeTime || Mine?.PhantomPhysics == true;

    public bool IsOwner(GameObject obj) => ReferenceEquals(obj, Owner);

    public override void Update(double time, GameWorld world)
    {
        totalTime += time;

        var physics = Parent.PhysicsComponent;
        if (physics?.Body == null)
        {
            return;
        }

        // Give the newly launched object one second to clear the owner's
        // collision volume, then restore normal physics collisions.
        physics.Collidable = !IsCollisionSafe;
        physics.Body.Collidable = !IsCollisionSafe;

        var lifetime = Munition.Def.Lifetime;
        if (lifetime > 0 && totalTime >= lifetime)
        {
            world.Server?.ExplodeMissile(Parent, false);
            return;
        }

        physics.Body.SetDamping(GetLinearDrag(), 0);

        if (Mine == null)
        {
            return;
        }

        var position = physics.Body.Position;
        if (HasDetonationTarget(world, position))
        {
            world.Server?.ExplodeMissile(Parent, true);
            return;
        }

        var target = FindNearestShip(world);

        if (target != null)
        {
            var targetPosition = GetNearestCollisionPoint(world, target, position);
            var direction = targetPosition - position;
            var distance = direction.Length();

            if (distance > 0.001f)
            {
                direction /= distance;
                var currentSpeed = physics.Body.LinearVelocity.Length();
                var speed = MathF.Min(GetTopSpeed(), currentSpeed + GetAcceleration() * (float)time);
                physics.Body.LinearVelocity = direction * speed;
                physics.Body.SetOrientation(QuaternionEx.LookAt(position, targetPosition));
            }
        }
    }

    private GameObject? FindNearestShip(GameWorld world)
    {
        var maxDistance = GetSeekDistance();
        var body = Parent.PhysicsComponent?.Body;
        if (maxDistance <= 0 || body == null)
        {
            return null;
        }

        var maxDistanceSquared = maxDistance * maxDistance;
        var origin = body.Position;
        GameObject? closest = null;
        var closestDistanceSquared = maxDistanceSquared;

        foreach (var candidate in world.SpatialLookup.GetNearbyObjects(Parent, origin, maxDistance))
        {
            if (candidate.Kind != GameObjectKind.Ship ||
                !candidate.Flags.HasFlag(GameObjectFlags.Exists) ||
                ReferenceEquals(candidate, Owner) ||
                candidate.Flags.HasFlag(GameObjectFlags.Player) ||
                candidate.PhysicsComponent?.Body == null)
            {
                continue;
            }

            var distanceSquared = Vector3.DistanceSquared(origin, candidate.PhysicsComponent.Body.Position);
            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closest = candidate;
            }
        }

        return closest;
    }

    private bool HasDetonationTarget(GameWorld world, Vector3 position)
    {
        var distance = GetDetonationDistance();
        if (distance < 0 || world.Physics == null)
        {
            return false;
        }

        foreach (var hit in world.Physics.SphereTest(position, distance))
        {
            if (hit?.Tag is not GameObject candidate ||
                candidate.Kind != GameObjectKind.Ship ||
                !candidate.Flags.HasFlag(GameObjectFlags.Exists) ||
                (ReferenceEquals(candidate, Owner) && IsOwnerSafe))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private Vector3 GetNearestCollisionPoint(GameWorld world, GameObject target, Vector3 position)
    {
        var body = target.PhysicsComponent?.Body;
        if (body == null)
        {
            return target.WorldTransform.Position;
        }

        var toCenter = body.Position - position;
        var centerDistance = toCenter.Length();
        if (centerDistance > 0.001f && world.Physics != null &&
            world.Physics.PointRaycast(Parent.PhysicsComponent?.Body, position,
                toCenter / centerDistance, centerDistance, true, out var contactPoint,
                out var hit, out _) && ReferenceEquals(hit, body))
        {
            return contactPoint;
        }

        var bounds = body.GetBoundingBox();
        return Vector3.Clamp(position, bounds.Min, bounds.Max);
    }

    private float GetLinearDrag() => Munition.Def switch
    {
        Countermeasure countermeasure => countermeasure.LinearDrag,
        Mine mine => mine.LinearDrag,
        _ => 0
    };

    private float GetSeekDistance() => Mine?.SeekDist ?? 0;
    private float GetTopSpeed() => Mine?.TopSpeed ?? 0;
    private float GetAcceleration() => Mine?.Acceleration ?? 0;
    private float GetDetonationDistance() => Mine?.DetonationDist ?? 0;
}
