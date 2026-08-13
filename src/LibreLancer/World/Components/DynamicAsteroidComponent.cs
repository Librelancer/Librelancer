using System.Numerics;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.World.Components;

public class DynamicAsteroidComponent(GameObject parent, float maxVelocity, float maxAngularVelocity,
    float despawnDistance, ulong spawnGroup, AsteroidFieldComponent? parentField, GameObject? refObject) : GameComponent(parent)
{
    public float MaxVelocity = maxVelocity;
    public float MaxAngularVelocity = maxAngularVelocity;
    public float SquareDespawnDistance = despawnDistance * despawnDistance;
    public ulong SpawnGroup = spawnGroup;
    public AsteroidFieldComponent? ParentField = parentField;
    public GameObject? RefObject = refObject;

    public override void Update(double time, GameWorld world)
    {
        // Server-side checks
        if (ParentField != null &&
            RefObject != null)
        {
            if ((RefObject.Flags & GameObjectFlags.Exists) == 0 ||
                Vector3.DistanceSquared(Parent.LocalTransform.Position, RefObject.LocalTransform.Position) >
                SquareDespawnDistance)
            {
                world.Server!.RemoveSpawnedObject(Parent, false);
            }
        }
        // Velocity limits
        if (Parent.PhysicsComponent!.Body.LinearVelocity.Length() > MaxVelocity)
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
