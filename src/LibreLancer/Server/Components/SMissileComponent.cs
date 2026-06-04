using System;
using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.World;

namespace LibreLancer.Server.Components;

public class SMissileComponent : GameComponent
{
    private const double LaunchCollisionDelay = 0.20;

    public MissileEquip Missile;
    public GameObject? Target;
    public GameObject Owner;

    public float Speed = 0;

    private PIDController pitchControl = new() { P = 1 };
    private PIDController yawControl = new() { P = 1 };

    public SMissileComponent(GameObject parent, MissileEquip missile, GameObject? target, GameObject owner,
        float speed) : base(parent)
    {
        Missile = missile;
        Target = target;
        Owner = owner;
        Speed = speed;
    }

    private double totalTime;
    private bool seekerLocked;

    public void SuppressLaunchCollision()
    {
        SetCollidable(false);
    }

    public override void Update(double time, GameWorld world)
    {
        totalTime += time;
        if (Missile.Motor != null)
        {
            DoMotor(time);
        }

        var phys = Parent.PhysicsComponent!;
        SetCollidable(totalTime >= LaunchCollisionDelay);
        phys.Body.LinearVelocity = Vector3.Transform(-Vector3.UnitZ, Parent.LocalTransform.Orientation) * Speed;

        if (Target != null &&
            !Target.Flags.HasFlag(GameObjectFlags.Exists))
        {
            Target = null;
            seekerLocked = false;
        }

        if (Target != null)
        {
            if (ShouldDetonate(Target))
            {
                world.Server!.ExplodeMissile(Parent);
                return;
            }

            if (CanTrack(Target))
            {
                TurnTowards(time, Target.WorldTransform.Position);
            }
            else
            {
                StabilizeRotation();
            }
        }
        else
        {
            StabilizeRotation();
        }

        if (Missile.Def.MaxAngularVelocity > 0 &&
            phys.Body.AngularVelocity.Length() > Missile.Def.MaxAngularVelocity)
        {
            phys.Body.AngularVelocity =
                phys.Body.AngularVelocity.Normalized() * Missile.Def.MaxAngularVelocity;
        }

        if (totalTime > Missile.Def.Lifetime)
        {
            world.Server!.ExplodeMissile(Parent); // Todo: does this do damage?
        }
    }

    private bool HasSeeker() =>
        !string.IsNullOrWhiteSpace(Missile.Def.Seeker) &&
        !Missile.Def.Seeker.Equals("dumb", StringComparison.OrdinalIgnoreCase) &&
        Missile.Def.MaxAngularVelocity > 0 &&
        Missile.Def.SeekerRange > 0 &&
        Missile.Def.SeekerFovDeg > 0;

    private bool ShouldDetonate(GameObject target)
    {
        if (Missile.Def.DetonationDist <= 0)
        {
            return false;
        }

        var missilePos = Parent.WorldTransform.Position;
        var targetPos = target.WorldTransform.Position;
        var targetRadius = target.PhysicsComponent?.Body.Collider.Radius ?? 0;
        return Vector3.DistanceSquared(missilePos, targetPos) <=
               MathF.Pow(Missile.Def.DetonationDist + targetRadius, 2);
    }

    private bool CanTrack(GameObject target)
    {
        if (!HasSeeker() || totalTime < Missile.Def.TimeToLock)
        {
            return false;
        }

        var toTarget = target.WorldTransform.Position - Parent.WorldTransform.Position;
        var distanceSquared = toTarget.LengthSquared();

        if (distanceSquared <= float.Epsilon)
        {
            return true;
        }

        if (distanceSquared > Missile.Def.SeekerRange * Missile.Def.SeekerRange)
        {
            seekerLocked = false;
            return false;
        }

        if (seekerLocked)
        {
            return true;
        }

        var forward = Vector3.Transform(-Vector3.UnitZ, Parent.WorldTransform.Orientation);
        var direction = Vector3.Normalize(toTarget);
        var dot = MathHelper.Clamp(Vector3.Dot(forward, direction), -1, 1);
        var angle = MathF.Acos(dot);
        seekerLocked = angle <= MathHelper.DegreesToRadians(Missile.Def.SeekerFovDeg);
        return seekerLocked;
    }

    private void StabilizeRotation()
    {
        Parent.PhysicsComponent!.Body.AddTorque(Parent.PhysicsComponent.Body.AngularVelocity * -1);
    }

    private void SetCollidable(bool collidable)
    {
        var phys = Parent.PhysicsComponent;
        if (phys == null)
        {
            return;
        }

        phys.Collidable = collidable;
        if (phys.Body != null)
        {
            phys.Body.Collidable = collidable;
        }
    }

    private void TurnTowards(double dt, Vector3 targetPoint)
    {
        // Orientation
        var vec = Parent.InverseTransformPoint(targetPoint);

        // normalize it
        if (vec.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        vec.Normalize();

        var yaw = MathHelper.Clamp((float) yawControl.Update(0, vec.X, dt), -1, 1);
        var pitch = MathHelper.Clamp((float) pitchControl.Update(0, -vec.Y, dt), -1, 1);
        var steering = new Vector3(pitch, yaw, 0);
        steering = Parent.PhysicsComponent!.Body.RotateVector(steering);
        steering = MathHelper.ApplyEpsilon(steering);
        var torque = new Vector3(50);
        var angularForce = steering * torque;
        angularForce += (Parent.PhysicsComponent.Body.AngularVelocity * -1);
        Parent.PhysicsComponent.Body.AddTorque(angularForce);
    }

    private void DoMotor(double time)
    {
        if (totalTime > Missile.Motor!.Lifetime + Missile.Motor.Delay ||
            totalTime < Missile.Motor.Delay)
        {
            return;
        }

        Speed += (float) (Missile.Motor.Accel * time);
    }
}
