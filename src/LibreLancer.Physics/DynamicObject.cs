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

    public override bool Collidable
    {
        get => world.collidableObjects[BepuObject.Handle];
        set => world.collidableObjects[BepuObject.Handle] = value;
    }

    public override bool Static => false;
    public override bool Active => BepuObject.Awake;

    public override Vector3 Position
    {
        get => BepuObject.Pose.Position;
        protected set => BepuObject.Pose.Position = value;
    }

    public override Quaternion Orientation
    {
        get => BepuObject.Pose.Orientation;
        protected set => BepuObject.Pose.Orientation = value;
    }

    public override void SetTransform(Transform3D transform)
    {
        Position = transform.Position;
        Orientation = transform.Orientation;
        BepuObject.UpdateBounds();
    }

    public override void SetOrientation(Quaternion orientation)
    {
        BepuObject.Pose.Orientation = orientation;
        BepuObject.UpdateBounds();
    }

    public override Vector3 AngularVelocity
    {
        get => BepuObject.Velocity.Angular;
        set
        {
            BepuObject.Velocity.Angular = value;
            if (value.LengthSquared() > 0)
                BepuObject.Awake = true;
        }
    }

    public override Vector3 LinearVelocity
    {
        get => BepuObject.Velocity.Linear;
        set
        {
            BepuObject.Velocity.Linear = value;
            if (value.LengthSquared() > 0)
                BepuObject.Awake = true;
        }
    }

    public override BoundingBox GetBoundingBox()
    {
        var bounds = BepuObject.BoundingBox;
        return new BoundingBox(bounds.Min, bounds.Max);
    }

    public override Vector3 RotateVector(Vector3 src)
    {
        return Vector3.Transform(src, BepuObject.Pose.Orientation);
    }

    public override void SetDamping(float linearDamping, float angularDamping)
    {
        world.dampings[BepuObject.Handle] = new Vector2(linearDamping, angularDamping);
    }

    private const float ForceFactor = (1 / 60.0f);

    public override void AddForce(Vector3 force)
    {
        if (force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force * ForceFactor, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void Activate()
    {
        BepuObject.Awake = true;
    }

    public override void Impulse(Vector3 force)
    {
        if (force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void AddTorque(Vector3 torque)
    {
        if (torque.LengthSquared() > 0)
        {
            BepuObject.ApplyAngularImpulse(torque * ForceFactor);
            BepuObject.Awake = true;
        }
    }

    public override void PredictionStep(float timestep)
    {
        PoseIntegration.Integrate(BepuObject.Pose, BepuObject.Velocity, timestep, out BepuObject.Pose);
        UpdateProperties();
    }

    internal override void UpdateProperties()
    {
    }

    public override void Dispose()
    {
    }
}
