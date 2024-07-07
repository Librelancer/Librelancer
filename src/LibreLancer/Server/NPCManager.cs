using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.Pilots;
using LibreLancer.Data.Solar;
using LibreLancer.GameData.World;
using LibreLancer.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server
{
    public class NPCManager
    {
        private NPCWattleScripting scripting;
        public ServerWorld World;
        public NPCManager(ServerWorld world)
        {
            this.World = world;
            scripting = new NPCWattleScripting(this);
        }

        public Task<string> RunScript(string src)
        {
            TaskCompletionSource<string> source = new TaskCompletionSource<string>();
            World.EnqueueAction(() =>
            {
                source.SetResult(scripting.Run(src));
            });
            return source.Task;
        }

        public int AttackingPlayer = 0;

        public void FrameStart()
        {
            AttackingPlayer = 0;
        }

        public void Despawn(GameObject obj, bool exploded)
        {
            World.RemoveNPC(obj, exploded);
        }

        private Dictionary<string, GameObject> missionNPCs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public void NpcDoAction(string nickname, Action<GameObject> act)
        {
            World.EnqueueAction(() =>
            {
                if (!missionNPCs.TryGetValue(nickname, out var npc))
                {
                    FLLog.Error("Mission", $"Could not find spawned npc {npc}");
                    return;
                }
                act(missionNPCs[nickname]);
            });
        }

        // Should be replaced with Faction class creating a random def
        public ObjectName RandomName(string affiliation)
        {
            var fac = World.Server.GameData.Factions.Get(affiliation);
            if (fac == null) return new ObjectName("NULL");
            var rand = new Random();
            ValueRange<int>? firstName = null;
            if (fac.Properties.FirstNameMale != null &&
                fac.Properties.FirstNameFemale != null)
            {
                firstName = rand.Next(0, 2) == 1 ? fac.Properties.FirstNameMale : fac.Properties.FirstNameFemale;
            }
            else if (fac.Properties.FirstNameFemale != null)
            {
                firstName = fac.Properties.FirstNameFemale;
            }
            else if (fac.Properties.FirstNameMale != null)
            {
                firstName = fac.Properties.FirstNameMale;
            }
            return new ObjectName(firstName != null ? rand.Next(firstName.Value) : 0, rand.Next(fac.Properties.LastName));
        }

        public GameObject DoSpawn(
            ObjectName name,
            string nickname,
            string affiliation,
            string stateGraph,
            string head,
            string body,
            string helmet,
            ObjectLoadout loadout,
            GameData.Pilot pilot,
            Vector3 position,
            Quaternion orient,
            MissionRuntime msn = null
            )
        {
            NetShipLoadout netLoadout = new NetShipLoadout();
            netLoadout.Items = new List<NetShipCargo>();
            var ship = World.Server.GameData.Ships.Get(loadout.Archetype);
            netLoadout.ShipCRC = ship.CRC;
            var obj = new GameObject(ship, World.Server.Resources, false, true);
            obj.Name = name;
            obj.Nickname = nickname;
            obj.SetLocalTransform(new Transform3D(position, orient));
            obj.AddComponent(new SHealthComponent(obj)
            {
                CurrentHealth = ship.Hitpoints,
                MaxHealth = ship.Hitpoints
            });
            obj.AddComponent(new SFuseRunnerComponent(obj) { DamageFuses = ship.Fuses });
            foreach (var equipped in loadout.Items)
            {
                EquipmentObjectManager.InstantiateEquipment(obj, World.Server.Resources, null, EquipmentType.Server,
                    equipped.Hardpoint, equipped.Equipment);
                netLoadout.Items.Add(new NetShipCargo(0, equipped.Equipment.CRC, equipped.Hardpoint ?? "internal", 255, 1));
            }
            var cargo = new SNPCCargoComponent(obj);
            cargo.Cargo.AddRange(loadout.Cargo);
            obj.AddComponent(cargo);
            var stateDescription = new StateGraphDescription(stateGraph.ToUpperInvariant(), "LEADER");
            World.Server.GameData.Ini.StateGraphDb.Tables.TryGetValue(stateDescription, out var stateTable);
            var npcComponent = new SNPCComponent(obj, this, stateTable) {Loadout = netLoadout, MissionRuntime = msn, Faction = World.Server.GameData.Factions.Get(affiliation)};
            npcComponent.SetPilot(pilot);
            npcComponent.CommHead = World.Server.GameData.Bodyparts.Get(head);
            npcComponent.CommBody = World.Server.GameData.Bodyparts.Get(body);
            npcComponent.CommHelmet = World.Server.GameData.Accessories.Get(helmet);
            obj.AddComponent(new SelectedTargetComponent(obj));
            obj.AddComponent(npcComponent);
            obj.AddComponent(new AutopilotComponent(obj));
            obj.AddComponent(new ShipSteeringComponent(obj));
            obj.AddComponent(new ShipPhysicsComponent(obj) { Ship = ship });
            obj.AddComponent(new WeaponControlComponent(obj));
            World.OnNPCSpawn(obj);
            if (nickname != null) missionNPCs[nickname] = obj;
            return obj;
        }
    }
}
