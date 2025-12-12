using System;
using System.Numerics;
using BepuPhysics;

namespace LibreLancer.Physics;

internal class DynamicObject : PhysicsObject
{
    internal BodyReference BepuObject;
    private PhysicsWorld world;

    internal DynamicObject(int id, PhysicsWorld world, BodyReference bepuObject, Collider col) : base(id)
    {
        this.BepuObject = bepuObject;
        this.Collider = col;
        this.world = world;
    }

    private bool IsValid => BepuObject.Bodies.BodyExists(BepuObject.Handle);

    public override bool Collidable
    {
        get => IsValid && world.collidableObjects[BepuObject.Handle];
        set { if (IsValid) world.collidableObjects[BepuObject.Handle] = value; }
    }

    public override bool Static => false;
    public override bool Active => IsValid && BepuObject.Awake;

    public override Vector3 Position
    {
        get => IsValid ? BepuObject.Pose.Position : Vector3.Zero;
        protected set { if (IsValid) BepuObject.Pose.Position = value; }
    }

    public override Quaternion Orientation
    {
        get => IsValid ? BepuObject.Pose.Orientation : Quaternion.Identity;
        protected set { if (IsValid) BepuObject.Pose.Orientation = value; }
    }

    public override void SetTransform(Transform3D transform)
    {
        if (IsValid)
        {
            Position = transform.Position;
            Orientation = transform.Orientation;
            BepuObject.UpdateBounds();
        }
    }

    public override void SetOrientation(Quaternion orientation)
    {
        if (IsValid)
        {
            BepuObject.Pose.Orientation = orientation;
            BepuObject.UpdateBounds();
        }
    }

    public override Vector3 AngularVelocity
    {
        get => IsValid ? BepuObject.Velocity.Angular : Vector3.Zero;
        set
        {
            if (IsValid)
            {
                BepuObject.Velocity.Angular = value;
                if (value.LengthSquared() > 0)
                    BepuObject.Awake = true;
            }
        }
    }

    public override Vector3 LinearVelocity
    {
        get => IsValid ? BepuObject.Velocity.Linear : Vector3.Zero;
        set
        {
            if (IsValid)
            {
                BepuObject.Velocity.Linear = value;
                if (value.LengthSquared() > 0)
                    BepuObject.Awake = true;
            }
        }
    }

    public override BoundingBox GetBoundingBox()
    {
        if (!IsValid)
            throw new ObjectDisposedException(nameof(DynamicObject), "The physics object is no longer valid.");
        var bounds = BepuObject.BoundingBox;
        return new BoundingBox(bounds.Min, bounds.Max);
    }

    public override Vector3 RotateVector(Vector3 src)
    {
        return Vector3.Transform(src, BepuObject.Pose.Orientation);
    }

    public override void SetDamping(float linearDamping, float angularDamping)
    {
        if (IsValid)
            world.dampings[BepuObject.Handle] = new Vector2(linearDamping, angularDamping);
    }

    private const float ForceFactor = (1 / 60.0f);

    public override void AddForce(Vector3 force)
    {
        if (IsValid && force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force * ForceFactor, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void Activate()
    {
        if (IsValid)
            BepuObject.Awake = true;
    }

    public override void Impulse(Vector3 force)
    {
        if (IsValid && force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void AddTorque(Vector3 torque)
    {
        if (IsValid && torque.LengthSquared() > 0)
        {
            BepuObject.ApplyAngularImpulse(torque * ForceFactor);
            BepuObject.Awake = true;
        }
    }

    public override void PredictionStep(float timestep)
    {
        if (IsValid)
        {
            PoseIntegration.Integrate(BepuObject.Pose, BepuObject.Velocity, timestep, out BepuObject.Pose);
            UpdateProperties();
        }
    }

    internal override void UpdateProperties()
    {
    }

    public override void Dispose()
    {
    }
}
