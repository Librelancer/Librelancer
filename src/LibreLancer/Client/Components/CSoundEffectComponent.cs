using System.Numerics;
using LibreLancer.Physics;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CSoundEffectComponent : GameComponent
{
    private readonly GameObject parent;
    private readonly AttachedSound sound;

    public CSoundEffectComponent(GameObject obj, AttachedSound snd) : base(obj)
    {
        sound = snd;
        parent = obj;
    }

    public override void Update(double time)
    {
        var tr = parent.WorldTransform;
        var pos = tr.Position;
        var vel = Vector3.Zero;

        if (parent.PhysicsComponent != null)
        {
            vel = parent.PhysicsComponent.Body.LinearVelocity;
        }

        sound.Active = true;
        sound.Position = pos;
        sound.Velocity = vel;
        sound.Update();
    }

    public override void Unregister(PhysicsWorld physics) => sound.Kill();
}
