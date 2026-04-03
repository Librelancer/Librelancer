using System.Numerics;
using LibreLancer.World;

namespace MonkeyDemo;

public class MonkeyComponent(GameObject parent) : GameComponent(parent)
{
    public bool Move = false;
    public bool Rotate = false;
    public override void Update(double time, GameWorld world)
    {
        if (Move)
        {
            Parent.PhysicsComponent!.Body.AddForce(new Vector3(0, 0, 200));
        }

        if (Rotate)
        {
            Parent.PhysicsComponent!.Body.AddTorque(new Vector3(0, -30, 0));
        }

    }
}
