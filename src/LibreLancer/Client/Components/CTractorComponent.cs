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
public class CTractorComponent(TractorEquipment equipment, GameObject parent) : GameComponent(parent)
{
    public TractorEquipment Equipment = equipment;

    private readonly TractorBeamRenderer renderer = new();
    private static int _beamSeed = 0;

    public void AddBeam(GameObject target)
    {
        renderer.TractorBeams.Add(new(target, 0, Interlocked.Increment(ref _beamSeed)));
    }

    public void RemoveBeam(GameObject target)
    {
        for (var i = 0; i < renderer.TractorBeams.Count; i++)
        {
            if (renderer.TractorBeams[i].Target != target)
            {
                continue;
            }

            renderer.TractorBeams.RemoveAt(i);
            i--;
        }
    }

    private Vector3 GetBeamOrigin()
    {
        if (!Parent!.TryGetComponent<ShipComponent>(out var ship) || string.IsNullOrWhiteSpace(ship.Ship.TractorSource))
        {
            return Parent.WorldTransform.Position;
        }

        var hp = Parent.GetHardpoint(ship.Ship.TractorSource);
        return hp != null ? (hp.Transform * Parent.WorldTransform).Position : Parent.WorldTransform.Position;
    }

    public Vector3 WorldOrigin;
    public int BeamCount => renderer.TractorBeams.Count;

    public override void Update(double time, GameWorld world)
    {
        for (var i = 0; i < renderer.TractorBeams.Count; i++)
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

    public override void Register(GameWorld world)
    {
        Parent!.ExtraRenderers.Add(renderer);
    }

    public override void Unregister(GameWorld world)
    {
        Parent!.ExtraRenderers.Remove(renderer);
    }
}
