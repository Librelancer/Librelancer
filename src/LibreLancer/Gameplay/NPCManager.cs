using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.AI;
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

        public void Attack(int id, string obj)
        {
            World.EnqueueAction(() =>
            {
                var npc = World.GameWorld.Objects.FirstOrDefault(x => x.NetID == id);
                var tgt = World.GameWorld.Objects.FirstOrDefault(x =>
                    obj.Equals(x.Nickname, StringComparison.OrdinalIgnoreCase));
                if (npc == null || tgt == null) return;
                if (npc.TryGetComponent<SNPCComponent>(out var n))
                    n.Attack(tgt);
            });
        }

        private Dictionary<string, GameObject> missionNPCs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public void NpcDoAction(string nickname, Action<GameObject> act)
        {
            World.EnqueueAction(() =>
            {
                act(missionNPCs[nickname]);
            });
        }
        public GameObject DoSpawn(string nickname, Loadout loadout, GameData.Pilot pilot, Vector3 position, Quaternion orient)
        {
            NetShipLoadout netLoadout = new NetShipLoadout();
            netLoadout.Items = new List<NetShipCargo>();
            var ship = World.Server.GameData.GetShip(loadout.Archetype);
            netLoadout.ShipCRC = ship.CRC;
            var obj = new GameObject(ship, World.Server.Resources, false, true);
            obj.Name = $"Bob NPC - {loadout.Nickname}";
            obj.Nickname = nickname;
            obj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orient) * Matrix4x4.CreateTranslation(position));
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
                var hp = equipped.Hardpoint == null ? NetShipCargo.InternalCrc : CrcTool.FLModelCrc(equipped.Hardpoint);
                netLoadout.Items.Add(new NetShipCargo(0, e.CRC, hp, 255, 1));
            }
            obj.Components.Add(new SNPCComponent(obj, this) {Loadout = netLoadout, Pilot = pilot});
            obj.Components.Add(new ShipPhysicsComponent(obj) { Ship = ship });
            obj.Components.Add(new ShipInputComponent(obj));
            obj.Components.Add(new AutopilotComponent(obj));
            obj.Components.Add(new WeaponControlComponent(obj));
            World.OnNPCSpawn(obj);
            if (nickname != null) missionNPCs[nickname] = obj;
            return obj;
        }

        public Task<int> SpawnNPC(Loadout loadout, Vector3 position)
        {
            var completionSource = new TaskCompletionSource<int>();
            World.EnqueueAction(() =>
            {
                var obj = DoSpawn(null, loadout, null, position, Quaternion.Identity);
                completionSource.SetResult(obj.NetID);
            });
            return completionSource.Task;
        }
    }
}