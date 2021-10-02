using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Data.Solar;

namespace LibreLancer
{
    public class NPCManager
    {
        public ServerWorld World;
        public NPCManager(ServerWorld world)
        {
            this.World = world;
        }

        public void Despawn(GameObject obj)
        {
            World.RemoveNPC(obj);
        }
        public void DockWith(int id, string obj)
        {
            World.EnqueueAction(() =>
            {
                var npc = World.GameWorld.Objects.FirstOrDefault(x => x.NetID == id);
                var tgt = World.GameWorld.Objects.FirstOrDefault(x =>
                    obj.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
                if (npc == null || tgt == null) return;
                if (npc.TryGetComponent<SNPCComponent>(out var n))
                    n.DockWith(tgt);
            });
        }

        public Task<int> SpawnNPC(Loadout loadout, Vector3 position)
        {
            var completionSource = new TaskCompletionSource<int>();
            World.EnqueueAction(() =>
            {
                NetShipLoadout netLoadout = new NetShipLoadout();
                netLoadout.Equipment = new List<NetShipEquip>();
                netLoadout.Cargo = new List<NetShipCargo>();
                var ship = World.Server.GameData.GetShip(loadout.Archetype);
                netLoadout.ShipCRC = ship.CRC;
                var obj = new GameObject(ship, World.Server.Resources, false, true);
                obj.Name = $"Bob NPC - {loadout.Nickname}";
                obj.SetLocalTransform(Matrix4x4.CreateTranslation(position));
                obj.Components.Add(new HealthComponent(obj)
                {
                    CurrentHealth = ship.Hitpoints,
                    MaxHealth = ship.Hitpoints
                });
                foreach (var equipped in loadout.Equip)
                {
                    var e = World.Server.GameData.GetEquipment(equipped.Nickname);
                    if (e == null) continue;
                    EquipmentObjectManager.InstantiateEquipment(obj, World.Server.Resources, EquipmentType.Server,
                        equipped.Hardpoint, e);
                    var hp = equipped.Hardpoint == null ? 0 : CrcTool.FLModelCrc(equipped.Hardpoint);
                    netLoadout.Equipment.Add(new NetShipEquip(hp, e.CRC, 255));
                }
                obj.Components.Add(new SNPCComponent(obj, this) {Loadout = netLoadout});
                obj.Components.Add(new ShipPhysicsComponent(obj) { Ship = ship });
                obj.Components.Add(new ShipInputComponent(obj));
                obj.Components.Add(new AutopilotComponent(obj));
                World.OnNPCSpawn(obj);
                completionSource.SetResult(obj.NetID);
            });
            return completionSource.Task;
        }
    }
}