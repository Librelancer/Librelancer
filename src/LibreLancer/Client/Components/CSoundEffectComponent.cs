using System.Numerics;
using LibreLancer.Physics;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CSoundEffectComponent : GameComponent
{
    private readonly GameObject parent;
    private readonly AttachedSound sound;

    public CSoundEffectComponent(GameObject obj, SoundManager snd, string soundName) : base(obj)
    {
        parent = obj;
        if (snd != null)
        {
            sound = new AttachedSound(snd);
            sound.Sound = soundName;
        }
    }

    public override void Update(double time)
    {
        if (sound == null)
            return;

        var tr = parent.WorldTransform;
        var pos = tr.Position;
        var vel = Vector3.Zero;

        if (parent.PhysicsComponent != null)
        {
            vel = parent.PhysicsComponent.Body.LinearVelocity;
        }

        sound.Position = pos;
        sound.Velocity = vel;
        sound.PlayIfInactive(true);
        sound.Update();
    }

    public override void Unregister(PhysicsWorld physics) => sound?.Stop();
}
