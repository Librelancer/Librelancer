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

    private void CheckValid()
    {
        if (!IsValid)
            throw new ObjectDisposedException(nameof(DynamicObject), "The physics object is no longer valid.");
    }

    public override bool Collidable
    {
        get { CheckValid(); return world.collidableObjects[BepuObject.Handle]; }
        set { CheckValid(); world.collidableObjects[BepuObject.Handle] = value; }
    }

    public override bool Static => false;
    public override bool Active { get { CheckValid(); return BepuObject.Awake; } }

    public override Vector3 Position
    {
        get { CheckValid(); return BepuObject.Pose.Position; }
        protected set { CheckValid(); BepuObject.Pose.Position = value; }
    }

    public override Quaternion Orientation
    {
        get { CheckValid(); return BepuObject.Pose.Orientation; }
        protected set { CheckValid(); BepuObject.Pose.Orientation = value; }
    }

    public override void SetTransform(Transform3D transform)
    {
        CheckValid();
        Position = transform.Position;
        Orientation = transform.Orientation;
        BepuObject.UpdateBounds();
    }

    public override void SetOrientation(Quaternion orientation)
    {
        CheckValid();
        BepuObject.Pose.Orientation = orientation;
        BepuObject.UpdateBounds();
    }

    public override Vector3 AngularVelocity
    {
        get { CheckValid(); return BepuObject.Velocity.Angular; }
        set
        {
            CheckValid();
            BepuObject.Velocity.Angular = value;
            if (value.LengthSquared() > 0)
                BepuObject.Awake = true;
        }
    }

    public override Vector3 LinearVelocity
    {
        get { CheckValid(); return BepuObject.Velocity.Linear; }
        set
        {
            CheckValid();
            BepuObject.Velocity.Linear = value;
            if (value.LengthSquared() > 0)
                BepuObject.Awake = true;
        }
    }

    public override BoundingBox GetBoundingBox()
    {
        CheckValid();
        var bounds = BepuObject.BoundingBox;
        return new BoundingBox(bounds.Min, bounds.Max);
    }

    public override Vector3 RotateVector(Vector3 src)
    {
        CheckValid();
        return Vector3.Transform(src, BepuObject.Pose.Orientation);
    }

    public override void SetDamping(float linearDamping, float angularDamping)
    {
        CheckValid();
        world.dampings[BepuObject.Handle] = new Vector2(linearDamping, angularDamping);
    }

    private const float ForceFactor = (1 / 60.0f);

    public override void AddForce(Vector3 force)
    {
        CheckValid();
        if (force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force * ForceFactor, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void Activate()
    {
        CheckValid();
        BepuObject.Awake = true;
    }

    public override void Impulse(Vector3 force)
    {
        CheckValid();
        if (force.LengthSquared() > 0)
        {
            BepuObject.ApplyImpulse(force, Vector3.Zero);
            BepuObject.Awake = true;
        }
    }

    public override void AddTorque(Vector3 torque)
    {
        CheckValid();
        if (torque.LengthSquared() > 0)
        {
            BepuObject.ApplyAngularImpulse(torque * ForceFactor);
            BepuObject.Awake = true;
        }
    }

    public override void PredictionStep(float timestep)
    {
        CheckValid();
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
