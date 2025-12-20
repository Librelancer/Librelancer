using System;
using System.Numerics;
using System.Threading;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Client.Components;

public record struct VisibleBeam(GameObject Target, float Distance, int Seed);
public class CTractorComponent : GameComponent
{
    public TractorEquipment Equipment;

    private TractorBeamRenderer renderer = new();

    private static int beamSeed = 0;


    public CTractorComponent(TractorEquipment equipment, GameObject parent) : base(parent)
    {
        Equipment = equipment;
    }

    public void AddBeam(GameObject target)
    {
        renderer.TractorBeams.Add(new(target, 0, Interlocked.Increment(ref beamSeed)));
    }

    public void RemoveBeam(GameObject target)
    {
        for (int i = 0; i < renderer.TractorBeams.Count; i++)
        {
            if (renderer.TractorBeams[i].Target == target)
            {
                renderer.TractorBeams.RemoveAt(i);
                i--;
            }
        }
    }

    Vector3 GetBeamOrigin()
    {
        if (Parent.TryGetComponent<ShipComponent>(out var ship) &&
            !string.IsNullOrWhiteSpace(ship.Ship.TractorSource))
        {
            var hp = Parent.GetHardpoint(ship.Ship.TractorSource);
            if (hp != null)
            {
                return (hp.Transform * Parent.WorldTransform).Position;
            }
            else
            {
                return Parent.WorldTransform.Position;
            }
        }
        return Parent.WorldTransform.Position;
    }

    public Vector3 WorldOrigin;
    public int BeamCount => renderer.TractorBeams.Count;

    public override void Update(double time)
    {
        for (int i = 0; i < renderer.TractorBeams.Count; i++)
        {
            if (!renderer.TractorBeams[i].Target.Flags.HasFlag(GameObjectFlags.Exists))
            {
                renderer.TractorBeams.RemoveAt(i);
                i--;
                continue;
            }
            renderer.TractorBeams[i].Distance += (float)(time * Equipment.Def.ReachSpeed);
        }
        renderer.Color = Equipment.Def.Color;
        WorldOrigin = renderer.Origin = GetBeamOrigin();
    }

    public override void Register(PhysicsWorld physics)
    {
        Parent.ExtraRenderers.Add(renderer);
    }

    public override void Unregister(PhysicsWorld physics)
    {
        Parent.ExtraRenderers.Remove(renderer);
    }
}
