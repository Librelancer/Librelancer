using System;
using System.Numerics;
using LibreLancer.GameData.Items;

namespace LibreLancer;

public class SMissileComponent : GameComponent
{
    public MissileEquip Missile;
    public GameObject Target;
    public GameObject Owner;

    public float Speed = 0;
    
    PIDController pitchControl = new PIDController() { P = 1 };
    PIDController yawControl = new PIDController() { P = 1 };

    public SMissileComponent(GameObject parent, MissileEquip missile) : base(parent)
    {
        this.Missile = missile;
    }

    private double totalTime;
    private Quaternion guidedRotation;
    
    public override void Update(double time)
    {
        totalTime += time;
        if (Missile.Motor != null) DoMotor(time);
        
        var phys = Parent.PhysicsComponent;
        phys.Body.LinearVelocity = Parent.LocalTransform.GetForward() * Speed;

        if (Target != null && 
            !Target.Flags.HasFlag(GameObjectFlags.Exists))
        {
            Target = null;
        }
        
        if (Target != null)
        {
            TurnTowards(time, Vector3.Transform(Vector3.Zero, Target.LocalTransform));
        }
        
        if (Missile.Def.MaxAngularVelocity > 0 &&
            phys.Body.AngularVelocity.Length() > Missile.Def.MaxAngularVelocity)
        {
            phys.Body.AngularVelocity =
                phys.Body.AngularVelocity.Normalized() * Missile.Def.MaxAngularVelocity;
        }
        
        if (totalTime > Missile.Def.Lifetime) {
            Parent.World.Server.ExplodeMissile(Parent); //Todo: does this do damage?
        }
    }
    
    void TurnTowards(double dt, Vector3 targetPoint)
    {
        //Orientation
        var vec = Parent.InverseTransformPoint(targetPoint);
        //normalize it
        vec.Normalize();
        //
        float yaw = MathHelper.Clamp((float)yawControl.Update(0, vec.X, dt), -1, 1);
        float pitch = MathHelper.Clamp((float)pitchControl.Update(0, -vec.Y, dt), -1, 1);
        var steering = new Vector3(pitch, yaw, 0);
        steering = Parent.PhysicsComponent.Body.RotateVector(steering);
        steering = MathHelper.ApplyEpsilon(steering);
        var torque = new Vector3(50);
        var angularForce = steering * torque;
        angularForce += (Parent.PhysicsComponent.Body.AngularVelocity * -1);
        Parent.PhysicsComponent.Body.AddTorque(angularForce);
    }
    

    void DoMotor(double time)
    {
        if (totalTime > Missile.Motor.Lifetime + Missile.Motor.Delay ||
            totalTime < Missile.Motor.Delay)
            return;
        Speed += (float) (Missile.Motor.Accel * time);
    }
}