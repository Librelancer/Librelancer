using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Server.Ai;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components;

public class SAutoTurretComponent : GameComponent
{
    private const float FireAlignmentToleranceDegrees = 1f;
    private readonly Func<GunBlock?>? getGunSettings;
    private readonly Random random = new();
    private readonly List<GunComponent> turrets = [];
    private readonly List<GameObject> candidates = [];
    private readonly HashSet<GunComponent> mountWarnings = [];
    private readonly HashSet<GunComponent> fireWarnings = [];
    private bool inBurst;
    private bool wasReturningToRest;
    private float burstTimer;
    private float fireTimer;

    internal int TrackingCount { get; private set; }
    internal bool InBurst => inBurst;
    internal float FireTimer => fireTimer;

    public SAutoTurretComponent(GameObject parent, Func<GunBlock?>? getGunSettings = null) : base(parent)
    {
        this.getGunSettings = getGunSettings;
    }

    public static bool TryAdd(GameObject parent, Func<GunBlock?>? getGunSettings = null)
    {
        var hasAutoTurret = false;
        foreach (var gun in parent.GetChildComponents<GunComponent>())
        {
            if (gun.Object.Def.AutoTurret)
            {
                hasAutoTurret = true;
                break;
            }
        }

        if (!hasAutoTurret)
            return false;

        if (!parent.TryGetComponent<WeaponControlComponent>(out _))
            parent.AddComponent(new WeaponControlComponent(parent));
        if (!parent.TryGetComponent<SAutoTurretComponent>(out _))
            parent.AddComponent(new SAutoTurretComponent(parent, getGunSettings));
        return true;
    }

    private float ValueWithVariance(float value, float variance)
    {
        var v = variance != 0 ? random.NextFloat(-variance, variance) : 0;
        return value + value * v;
    }

    private bool RunFireTimer(float dt)
    {
        var settings = getGunSettings?.Invoke();
        if (inBurst)
        {
            burstTimer -= dt;
            if (burstTimer <= 0)
            {
                inBurst = false;
                burstTimer = MathF.Max(0, settings?.AutoTurretNoBurstIntervalTime ?? 0);
                return false;
            }

            fireTimer -= dt;
            if (fireTimer <= 0)
            {
                var interval = settings?.AutoTurretIntervalTime ?? 0.1f;
                fireTimer = interval > 0 ? interval : 0.1f;
                return true;
            }
        }
        else
        {
            burstTimer -= dt;
            if (burstTimer <= 0)
            {
                inBurst = true;
                var burstLength = settings?.AutoTurretBurstIntervalTime ?? 1f;
                if (burstLength <= 0)
                    burstLength = 1f;
                burstTimer = ValueWithVariance(burstLength,
                    settings?.AutoTurretBurstIntervalVariancePercent ?? 0);
                fireTimer = 0;
            }
        }

        return false;
    }

    private bool IsHostileTarget(SRepComponent reputation, GameObject other)
    {
        if (reputation.IsHostileTo(other))
            return true;

        return other.TryGetComponent<SRepComponent>(out var otherReputation) &&
               otherReputation.IsHostileTo(Parent);
    }

    private void WarnOnce(HashSet<GunComponent> warnings, GunComponent turret, string message)
    {
        if (!warnings.Add(turret))
            return;

        var owner = Parent.Nickname ?? Parent.ArchetypeName ?? Parent.ToString();
        var mount = turret.Parent.Attachment?.Name ?? "(no hardpoint)";
        FLLog.Warning("AutoTurret", $"{owner}: auto turret `{turret.Object.Nickname}` on {mount} {message}");
    }

    private bool CanFireTurret(GunComponent turret)
    {
        if (turret.Parent.Model == null)
        {
            WarnOnce(fireWarnings, turret, "has no model, so it cannot find HpFire hardpoints.");
            return false;
        }

        if (turret.TryGetMuzzleTransform(out _))
            return true;

        WarnOnce(fireWarnings, turret, "has no HpFire hardpoints, so it cannot spawn projectiles.");
        return false;
    }

    internal GameObject? FindTarget(GunComponent turret, IEnumerable<GameObject> possibleTargets)
    {
        if (!turret.Object.Def.AutoTurret ||
            !Parent.TryGetComponent<SRepComponent>(out var reputation))
        {
            return null;
        }

        if (turret.Parent.Attachment == null)
        {
            WarnOnce(mountWarnings, turret, "is not mounted on a valid parent hardpoint.");
            return null;
        }

        var turretPosition = turret.Parent.WorldTransform.Position;
        var maxRangeSquared = turret.MaxRange * turret.MaxRange;
        var closestDistanceSquared = float.MaxValue;
        GameObject? closest = null;

        foreach (var other in possibleTargets)
        {
            if (other == Parent ||
                !other.Flags.HasFlag(GameObjectFlags.Exists) ||
                other.Flags.HasFlag(GameObjectFlags.Cloaked) ||
                other.TryGetComponent<STradelaneMoveComponent>(out _) ||
                !IsHostileTarget(reputation, other))
            {
                continue;
            }

            var targetPosition = other.WorldTransform.Position;
            var distanceSquared = Vector3.DistanceSquared(turretPosition, targetPosition);
            if (distanceSquared > maxRangeSquared ||
                distanceSquared >= closestDistanceSquared ||
                !turret.CanAimAt(targetPosition))
            {
                continue;
            }

            closest = other;
            closestDistanceSquared = distanceSquared;
        }

        return closest;
    }

    internal Vector3 GetAimPosition(GameObject target, GunComponent turret)
    {
        var myPosition = turret.TryGetMuzzleTransform(out var muzzle)
            ? muzzle.Position
            : turret.Parent.WorldTransform.Position;
        var myVelocity = Parent.PhysicsComponent?.Body.LinearVelocity ?? Vector3.Zero;
        var targetPosition = target.PhysicsComponent?.Body.Position ?? target.WorldTransform.Position;
        var targetVelocity = target.PhysicsComponent?.Body.LinearVelocity ?? Vector3.Zero;

        if (turret.Object.Def.MuzzleVelocity > float.Epsilon &&
            Aiming.GetTargetLeading(targetPosition - myPosition, targetVelocity - myVelocity,
                turret.Object.Def.MuzzleVelocity, out var time) &&
            float.IsFinite(time) && time >= 0)
        {
            return targetPosition + targetVelocity * time;
        }

        return targetPosition;
    }

    public override void Update(double time, GameWorld world)
    {
        turrets.Clear();
        var maxRange = 0f;
        foreach (var gun in Parent.GetChildComponents<GunComponent>())
        {
            if (gun.Object.Def.AutoTurret)
            {
                turrets.Add(gun);
                maxRange = MathF.Max(maxRange, gun.MaxRange);
            }
        }

        if (turrets.Count == 0 ||
            !Parent.TryGetComponent<SRepComponent>(out _) ||
            !Parent.TryGetComponent<WeaponControlComponent>(out var weapons))
        {
            TrackingCount = 0;
            SetSolarTrackingState(false);
            return;
        }

        candidates.Clear();
        foreach (var candidate in world.SpatialLookup
                     .GetNearbyObjects(Parent, Parent.WorldTransform.Position, maxRange))
        {
            candidates.Add(candidate);
        }

        var shouldFire = RunFireTimer((float)time);
        var canFire = !shouldFire || weapons.CanFireWeapons(world);
        TrackingCount = 0;
        var movingToRest = false;
        foreach (var turret in turrets)
        {
            var target = FindTarget(turret, candidates);
            if (target == null)
            {
                if (!turret.ReturnToRest(time))
                    movingToRest = true;
                continue;
            }

            TrackingCount++;
            var aimPoint = GetAimPosition(target, turret);
            turret.AimTowards(aimPoint, time);
            if (shouldFire && canFire && CanFireTurret(turret) &&
                turret.IsMuzzleAligned(aimPoint, FireAlignmentToleranceDegrees))
                turret.Fire(aimPoint, world, target);
        }

        SetSolarTrackingState(TrackingCount > 0 || movingToRest || wasReturningToRest);
        wasReturningToRest = movingToRest;
    }

    private void SetSolarTrackingState(bool tracking)
    {
        if (Parent.TryGetComponent<SSolarComponent>(out var solar))
            solar.SendAutoTurretUpdate = tracking;
    }
}
