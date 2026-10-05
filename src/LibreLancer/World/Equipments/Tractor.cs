using System.Numerics;
using System.Threading;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Server;
using LibreLancer.Server.Components;
using LibreLancer.World.Components;

namespace LibreLancer.World.Equipments;

public class Tractor : EquipmentObject<TractorEquipment>
{
    private record struct ActiveBeam(GameObject Other, float Distance, float Time);

    private RefList<ActiveBeam> beams = [];
    private readonly TractorBeamRenderer? renderer;
    private static int _beamSeed = 0;

    public Vector3 WorldOrigin;

    public int ClientBeamCount => renderer?.TractorBeams.Count ?? 0;


    public Tractor(GameObject parent, Hardpoint? attachment, TractorEquipment equipment, EquipmentType type)
        : base(parent, attachment, equipment, type, type == EquipmentType.Server,
            type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
    {
        if (type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
        {
            renderer = new();
        }
    }

    // Client-side


    public void AddBeam(GameObject target)
    {
        renderer?.TractorBeams.Add(new(target, 0, Interlocked.Increment(ref _beamSeed)));
    }

    public void RemoveBeam(GameObject target)
    {
        if (renderer != null)
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
    }

    // Server-side
    public void TryTractor(GameObject other, ServerWorld world)
    {
        if (other.Kind != GameObjectKind.Loot ||
            !other.Flags.HasFlag(GameObjectFlags.Exists))
        {
            return;
        }
        for (int i = 0; i < beams.Count; i++)
        {
            if (beams[i].Other == other)
                return;
        }

        beams.Add(new(other, 0, 0));
        world.StartTractor(Parent, other);
    }

    private Vector3 GetBeamOrigin()
    {
        if (Parent.TryGetComponent<ShipComponent>(out var ship) &&
            !string.IsNullOrWhiteSpace(ship.Ship.TractorSource))
        {
            var hp = Parent.GetHardpoint(ship.Ship.TractorSource);
            if (hp != null)
            {
                return (hp.Transform * Parent.Transform).Position;
            }
            else
            {
                return Parent.Transform.Position;
            }
        }
        return Parent.Transform.Position;
    }

    public override void OnUpdate(double time, GameWorld world)
    {
        var origin = GetBeamOrigin();
        WorldOrigin = origin;

        if (renderer != null)
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
            renderer.Origin = origin;
        }

        if (world.Server == null)
            return;
        for (int i = 0; i < beams.Count; i++)
        {
            var dist = Vector3.Distance(origin, beams[i].Other.Transform.Position);
            beams[i].Distance += (float)(time * Equipment.Def.ReachSpeed);
            if (beams[i].Distance >= dist)
            {
                beams[i].Distance = dist;
                beams[i].Time += (float)time;
            }
            if (dist > Equipment.Def.MaxLength)
            {
                world.Server!.EndTractor(Parent, beams[i].Other);
                if (Parent.TryGetComponent<SPlayerComponent>(out var player))
                {
                    player.Player.RpcClient.TractorFailed();
                }
                beams.RemoveAt(i);
                i--;
            }
            else if (!beams[i].Other.Flags.HasFlag(GameObjectFlags.Exists))
            {
                beams.RemoveAt(i);
                i--;
            }
            else if (beams[i].Time >= 1.0f)
            {
                world.Server!.PickupObject(Parent, beams[i].Other);
                world.Server!.EndTractor(Parent, beams[i].Other);
                beams.RemoveAt(i);
                i--;
            }
        }
    }

    public override void RenderUpdate(double delta)
    {
        renderer?.Update(delta, Parent.Transform.Position, Parent.Transform.Matrix());
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        renderer?.PrepareRender(camera, nr, sys, parentCull);
    }
}
