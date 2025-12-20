using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Pilots;
using LibreLancer.Data.Schema.Solar;
using LibreLancer.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server
{
    public class NPCManager
    {
        private NPCWattleScripting scripting;
        public ServerWorld World;
        private Random rand = new();
        public NPCManager(ServerWorld world)
        {
            this.World = world;
            scripting = new NPCWattleScripting(this, world.Server.GameData);
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
            World.RemoveSpawnedObject(obj, exploded);
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
            var fac = World.Server.GameData.Items.Factions.Get(affiliation);
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

        public GameObject SpawnJumper(JumperNpc jumper, MissionRuntime msn, string jumpObject)
        {
            var jumpPoint = World.GameWorld.GetObject(jumpObject);
            var pos = jumpPoint.WorldTransform.Position;
            var orient = jumpPoint.WorldTransform.Orientation;
            pos = Vector3.Transform(new Vector3(rand.Next(-50, 50), rand.Next(-50, 50), rand.Next(-300, -100)),
                orient) + pos;
            var newObj = DoSpawn(
                jumper.Name,
                jumper.Nickname,
                jumper.Faction?.Nickname,
                jumper.StateGraph?.Description?.Name,
                jumper.CommHead?.Nickname,
                jumper.CommBody?.Nickname,
                jumper.CommHelmet?.Nickname,
                jumper.Loadout,
                jumper.Pilot,
                pos,
                orient,
                null, 0,
                msn);
            msn.SystemEnter(World.System.Nickname, jumper.Nickname);
            return newObj;
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
            Pilot pilot,
            Vector3 position,
            Quaternion orient,
            string arrivalObj,
            int arrivalIndex,
            MissionRuntime msn = null
            )
        {
            var ship = World.Server.GameData.Items.Ships.Get(loadout.Archetype);
            GameObject spawnPoint = World.GameWorld.GetObject(arrivalObj);
            SDockableComponent sdock = null;
            if (spawnPoint?.TryGetComponent<SDockableComponent>(out sdock) ?? false)
            {
                if (arrivalIndex == 0)
                    arrivalIndex = sdock.GetUndockIndex();
                var p = sdock.GetSpawnPoint(arrivalIndex);
                position = p.Position;
                orient = p.Orientation;
            }
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
            }
            var cargo = new SNPCCargoComponent(obj);
            cargo.Cargo.AddRange(loadout.Cargo);
            obj.AddComponent(cargo);
            var stateDescription = new StateGraphDescription(stateGraph.ToUpperInvariant(), "LEADER");
            World.Server.GameData.Items.Ini.StateGraphDb.Tables.TryGetValue(stateDescription, out var stateTable);
            var npcComponent = new SNPCComponent(obj, this, stateTable) { MissionRuntime = msn, Faction = World.Server.GameData.Items.Factions.Get(affiliation)};
            npcComponent.SetPilot(pilot);
            npcComponent.CommHead = World.Server.GameData.Items.Bodyparts.Get(head);
            npcComponent.CommBody = World.Server.GameData.Items.Bodyparts.Get(body);
            npcComponent.CommHelmet = World.Server.GameData.Items.Accessories.Get(helmet);
            obj.AddComponent(new SelectedTargetComponent(obj));
            obj.AddComponent(npcComponent);
            obj.AddComponent(new AutopilotComponent(obj));
            obj.AddComponent(new ShipSteeringComponent(obj));
            obj.AddComponent(new ShipPhysicsComponent(obj) { Ship = ship });
            obj.AddComponent(new WeaponControlComponent(obj));
            obj.AddComponent(new SDestroyableComponent(obj, World));
            obj.AddComponent(new DirectiveRunnerComponent(obj));
            World.OnNPCSpawn(obj);
            if (sdock != null)
            {
                sdock.UndockShip(obj, arrivalIndex);
                obj.GetComponent<AutopilotComponent>().Undock(spawnPoint, arrivalIndex);
            }
            if (nickname != null) missionNPCs[nickname] = obj;
            return obj;
        }
    }
}
