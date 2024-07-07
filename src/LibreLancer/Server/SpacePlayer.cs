using System.Numerics;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server;

public class SpacePlayer : ISpacePlayer
{
    public ServerWorld World => world;
    public Player Player => player;

    private ServerWorld world;
    private Player player;

    public SpacePlayer(ServerWorld world, Player player)
    {
        this.world = world;
        this.player = player;
    }

    public void Leave(bool exploded)
    {
        world.RemovePlayer(player, exploded);
    }

    public void ForceMove(Vector3 position, Quaternion? orientation = null)
    {
        world.EnqueueAction(() =>
        {
            var obj = World.Players[player];
            var rot = orientation ?? obj.LocalTransform.Orientation;
            obj.SetLocalTransform(new Transform3D(position, rot));
        });
    }

    public void RequestDock(ObjNetId id) => world.RequestDock(player, id);

    public void FireMissiles(MissileFireCmd[] missiles) => world.FireMissiles(missiles, player);

    public void EnterFormation(int ship)
    {
        world.EnqueueAction(() =>
        {
            var self = world.Players[player];
            var other = world.GameWorld.GetObject(  new ObjNetId() { Value = ship });
            if (other != null)
            {
                if (other.Formation != null) {
                    if(!other.Formation.Contains(self))
                        other.Formation.Add(self);
                }
                else {
                    other.Formation = new ShipFormation(other, self);
                }
                self.Formation = other.Formation;
                player.MissionRuntime?.PlayerManeuver("formation", other.Nickname);
            }
            else {
                FLLog.Warning("Server", $"Could not find object to join formation {ship}");
            }
        });
    }

    public void LeaveFormation()
    {
        world.EnqueueAction(() =>
        {
            var obj = world.Players[player];
            if(obj.Formation != null && obj.Formation.LeadShip != obj)
                obj.Formation.Remove(obj);
        });
    }

    public void UseRepairKits()
    {
        world.EnqueueAction(() =>
        {
            var obj = world.Players[player];
            obj.GetComponent<SHealthComponent>()?.UseRepairKits();
        });
    }

    public void UseShieldBatteries()
    {
        world.EnqueueAction(() =>
        {
            var obj = world.Players[player];
            obj.GetComponent<SHealthComponent>()?.UseShieldBatteries();
        });
    }
}
