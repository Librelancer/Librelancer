namespace LibreLancer.World.Components;

public class DynamicAsteroidComponent(GameObject parent, float maxVelocity, float maxAngularVelocity) : GameComponent(parent)
{
    public float MaxVelocity = maxVelocity;
    public float MaxAngularVelocity = maxAngularVelocity;

    public override void Update(double time, GameWorld world)
    {
        if (Parent.PhysicsComponent.Body.LinearVelocity.Length() > MaxVelocity)
        {
            Parent.PhysicsComponent.Body.LinearVelocity =
                Parent.PhysicsComponent.Body.LinearVelocity.Normalized() * MaxVelocity;
        }
        if (Parent.PhysicsComponent.Body.AngularVelocity.Length() > MaxAngularVelocity)
        {
            Parent.PhysicsComponent.Body.AngularVelocity =
                Parent.PhysicsComponent.Body.AngularVelocity.Normalized() * MaxAngularVelocity;
        }
    }
}
