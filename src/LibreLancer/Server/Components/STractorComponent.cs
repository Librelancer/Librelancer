using System.Numerics;
using LibreLancer.Data.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components;

public class STractorComponent : GameComponent
{
    public TractorEquipment Equipment;

    record struct ActiveBeam(GameObject Other, float Distance, float Time);

    private RefList<ActiveBeam> beams = new();

    public STractorComponent(TractorEquipment equipment, GameObject parent) : base(parent)
    {
        Equipment = equipment;
    }

    public void TryTractor(GameObject other)
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
        Parent.GetWorld().Server.StartTractor(Parent, other);
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

    public override void Update(double time)
    {
        var origin = GetBeamOrigin();
        for (int i = 0; i < beams.Count; i++)
        {
            var dist = Vector3.Distance(origin, beams[i].Other.WorldTransform.Position);
            beams[i].Distance += (float)(time * Equipment.Def.ReachSpeed);
            if (beams[i].Distance >= dist)
            {
                beams[i].Distance = dist;
                beams[i].Time += (float)time;
            }
            if (dist > Equipment.Def.MaxLength)
            {
                Parent.GetWorld().Server.EndTractor(Parent, beams[i].Other);
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
                Parent.GetWorld().Server.PickupObject(Parent, beams[i].Other);
                Parent.GetWorld().Server.EndTractor(Parent, beams[i].Other);
                beams.RemoveAt(i);
                i--;
            }
        }
    }
}
