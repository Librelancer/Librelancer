// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using LibreLancer.GameData.Items;
using LibreLancer.Net;

namespace LibreLancer
{
    public class ServerWorld
    {
        public Dictionary<Player, GameObject> Players = new Dictionary<Player, GameObject>();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        public GameWorld GameWorld;
        public GameServer Server;
        public GameData.StarSystem System;
        public NPCManager NPCs;
        private int mId = -1;
        object _idLock = new object();

        
        public double TotalTime { get; private set; }
        int GenerateID()
        {
            lock (_idLock)
            {
                var retVal = mId--;
                if (mId < int.MinValue + 2) mId = -1;
                return retVal;
            }
        }

        private bool paused = false;

        public void Pause()
        {
            EnqueueAction(() => paused = true);
        }

        public void Resume()
        {
            EnqueueAction(() => paused = false);
        }
        
        public ServerWorld(GameData.StarSystem system, GameServer server)
        {
            Server = server;
            System = system;
            GameWorld = new GameWorld(null);
            GameWorld.Server = this;
            GameWorld.LoadSystem(system, server.Resources, true);
            NPCs = new NPCManager(this);
        }

        public int PlayerCount;

        public void SpawnPlayer(Player player, Vector3 position, Quaternion orientation)
        {
            Interlocked.Increment(ref PlayerCount);
            actions.Enqueue(() =>
            {
                foreach (var p in Players)
                {
                    player.SpawnPlayer(p.Key);
                    p.Key.SpawnPlayer(player);
                }
                player.SendSolars(SpawnedSolars);
                foreach(var npc in spawnedNPCs)
                    SpawnShip(npc, player);
                var obj = new GameObject(player.Character.Ship, Server.Resources, false, true) { World = GameWorld };
                obj.Components.Add(new SPlayerComponent(player, obj));
                obj.Components.Add(new HealthComponent(obj)
                {
                    CurrentHealth = player.Character.Ship.Hitpoints,
                    MaxHealth = player.Character.Ship.Hitpoints
                });
                obj.Components.Add(new SEngineComponent(obj));
                obj.NetID = player.ID;
                GameWorld.AddObject(obj);
                obj.Register(GameWorld.Physics);
                Players[player] = obj;
                Players[player].SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                                  Matrix4x4.CreateTranslation(position));
            });
        }

        public void ProjectileHit(GameObject obj, MunitionEquip munition)
        {
            if (obj.TryGetComponent<HealthComponent>(out var health)) {
                health.Damage(munition.Def.HullDamage);
            }
        }

        public void RequestDock(Player player, string nickname)
        {
            actions.Enqueue(() =>
            {
                var obj = Players[player];
                FLLog.Info("Server", $"{player.Name} requested dock at {nickname}");
                var dock = GameWorld.Objects.FirstOrDefault(x =>
                    x.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
                if(dock == null)
                    FLLog.Warning("Server", $"Dock object {nickname} does not exist.");
                else
                {
                    var component = dock.GetComponent<SDockableComponent>();
                    if(component == null)
                        FLLog.Warning("Server", $"object {nickname} is not dockable.");
                    else {
                        component.StartDock(obj, 0);
                    }
                }
            });
        }

        public void EnqueueAction(Action a)
        {
            actions.Enqueue(a);
        }

        public void FireProjectiles(ProjectileSpawn[] projectiles, Player owner)
        {
            actions.Enqueue(() =>
            {
                for(int i = 0; i < projectiles.Length; i++)
                    projectiles[i].Owner = owner.ID;
                foreach (var p in Players.Keys)
                {
                    if (p == owner) continue;
                    p.RemoteClient.SpawnProjectiles(projectiles);
                }
                foreach (var p in projectiles)
                {
                    var pdata = GameWorld.Projectiles.GetData(Server.GameData.GetEquipment(p.Gun) as GunEquipment);
                    GameWorld.Projectiles.SpawnProjectile(Players[owner], p.Hardpoint, pdata, p.Start, p.Heading);
                }
            });
        }
        
        public void StartAnimation(GameObject obj, string script)
        {
            int id = 0;
            bool sysObj = false;
            if (!string.IsNullOrEmpty(obj.Nickname))
            {
                id = (int) obj.NicknameCRC;
                sysObj = true;
            }
            else
            {
                id = obj.NetID;
            }
            foreach (var p in Players) {
                p.Key.RemoteClient.StartAnimation(sysObj, id, script);
            }
        }
        

        public void RemovePlayer(Player player)
        {
            actions.Enqueue(() =>
            {
                Players[player].Unregister(GameWorld.Physics);
                GameWorld.RemoveObject(Players[player]);
                Players.Remove(player);
                foreach(var p in Players)
                {
                    p.Key.Despawn(player.ID);
                }
                Interlocked.Decrement(ref PlayerCount);
            });
        }

        public void RemoveNPC(GameObject obj)
        {
            actions.Enqueue(() =>
            {
                obj.Unregister(GameWorld.Physics);
                GameWorld.RemoveObject(obj);
                spawnedNPCs.Remove(obj);
                foreach (var p in Players) p.Key.Despawn(obj.NetID);
            });
        }

        public void PositionUpdate(Player player, Vector3 position, Quaternion orientation, float speed)
        {
            actions.Enqueue(() =>
            {
                Players[player].SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                                  Matrix4x4.CreateTranslation(position));
                Players[player].GetComponent<SEngineComponent>().Speed = speed;
            });
        }

        public Dictionary<string, GameObject> SpawnedSolars = new Dictionary<string, GameObject>();
        List<GameObject> updatingObjects = new List<GameObject>();
        List<GameObject> spawnedNPCs = new List<GameObject>();

        public void SpawnSolar(string nickname, string archetype, string loadout, Vector3 position, Quaternion orientation)
        {
            actions.Enqueue(() =>
            {
                var arch = Server.GameData.GetSolarArchetype(archetype);
                var gameobj = new GameObject(arch, Server.Resources, false);
                gameobj.ArchetypeName = archetype;
                gameobj.NetID = GenerateID();
                gameobj.SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                          Matrix4x4.CreateTranslation(position));
                gameobj.Nickname = nickname;
                gameobj.World = GameWorld;
                gameobj.Register(GameWorld.Physics);
                gameobj.CollisionGroups = arch.CollisionGroups;
                GameWorld.AddObject(gameobj);
                SpawnedSolars.Add(nickname, gameobj);
                foreach(Player p in Players.Keys)
                    p.SendSolars(SpawnedSolars);
            });
        }

        public void SpawnDebris(string archetype, string part, Matrix4x4 transform, float mass, Vector3 initialForce)
        {
            actions.Enqueue(() =>
            {
                var arch = Server.GameData.GetSolarArchetype(archetype);
                var mdl = ((IRigidModelFile) arch.ModelFile.LoadFile(Server.Resources)).CreateRigidModel(false);
                var newpart = mdl.Parts[part].Clone();
                var newmodel = new RigidModel()
                {
                    Root = newpart,
                    AllParts = new[] { newpart },
                    MaterialAnims = mdl.MaterialAnims,
                    Path = mdl.Path,
                };
                var id = GenerateID();
                var go = new GameObject($"debris{id}", newmodel, Server.Resources, part, mass, false);
                go.NetID = id;
                go.SetLocalTransform(transform);
                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                go.Register(GameWorld.Physics);
                go.PhysicsComponent.Body.Impulse(initialForce);
                //Spawn debris
                foreach (Player p in Players.Keys)
                {
                    p.SpawnDebris(go.NetID, archetype, part, transform, mass);
                }
            });
        }

        public void OnNPCSpawn(GameObject obj)
        {
            obj.NetID = GenerateID();
            obj.World = GameWorld;
            obj.Register(GameWorld.Physics);
            GameWorld.AddObject(obj);
            updatingObjects.Add(obj);
            spawnedNPCs.Add(obj);
            foreach (Player p in Players.Keys)
            {
                SpawnShip(obj, p);
            }
        }

        void SpawnShip(GameObject obj, Player p)
        {
            var pos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
            var orient = obj.LocalTransform.ExtractRotation();
            p.RemoteClient.SpawnObject(obj.NetID, obj.Name, pos, orient, obj.GetComponent<SNPCComponent>().Loadout);
        }

        public void PartDisabled(GameObject obj, string part)
        {
            foreach (Player p in Players.Keys)
                p.SendDestroyPart(obj.NetID, part);
        }

        public void DeleteSolar(string nickname)
        {
            actions.Enqueue(() =>
            {
                var s = SpawnedSolars[nickname];
                SpawnedSolars.Remove(nickname);
                GameWorld.RemoveObject(s);
                foreach (Player p in Players.Keys)
                    p.Despawn(s.NetID);
            });
        }

        private double noPlayersTime;
        private double maxNoPlayers = 2.0;
        public bool Update(double delta)
        {
            //Avoid locks during Update
            Action act;
            while(actions.Count > 0 && actions.TryDequeue(out act)){ act(); }
            //pause
            if (paused) return true;
            TotalTime += delta;
            //Update
            GameWorld.Update(delta);
            //projectiles
            if (GameWorld.Projectiles.HasQueued)
            {
                var queue = GameWorld.Projectiles.GetQueue();
                foreach(var p in Players)
                    p.Key.RemoteClient.SpawnProjectiles(queue);
            }
            //Network update tick
            current += delta;
            tickTime += delta;
            if (tickTime > (LNetConst.MAX_TICK_MS / 1000.0))
                tickTime -= (LNetConst.MAX_TICK_MS / 1000.0);
            var tick = (uint) (tickTime * 1000.0);
            if (current >= UPDATE_RATE) {
                current -= UPDATE_RATE;
                //Send multiplayer updates (less)
                SendPositionUpdates(true, tick);
            }
            SendPositionUpdates(false, tick);
            //Despawn after 2 seconds of nothing
            if (PlayerCount == 0) {
                noPlayersTime += delta;
                return (noPlayersTime < maxNoPlayers);
            }
            else
            {
                noPlayersTime = 0;
                return true;
            }
        }

        const double UPDATE_RATE = 1 / 25.0;
        double current = 0;
        private double tickTime = 0;

        //This could do with some work
        void SendPositionUpdates(bool mp, uint tick)
        {
            foreach(var player in Players)
            {
                var tr = player.Value.WorldTransform;
                player.Key.Position = Vector3.Transform(Vector3.Zero, tr);
                player.Key.Orientation = tr.ExtractRotation();
            }
            IEnumerable<KeyValuePair<Player,GameObject>> targets;
            if (mp) {
                targets = Players.Where(x => x.Key.Client is RemotePacketClient);
            }
            else {
                targets = Players.Where(x => x.Key.Client is LocalPacketClient);
            }
            foreach (var player in targets)
            {
                List<PackedShipUpdate> ps = new List<PackedShipUpdate>();
                var phealth = player.Value.GetComponent<HealthComponent>().CurrentHealth;
                foreach (var otherPlayer in Players)
                {
                    if (otherPlayer.Key == player.Key) continue;
                    var update = new PackedShipUpdate();
                    update.ID = otherPlayer.Key.ID;
                    update.HasPosition = true;
                    update.Position = otherPlayer.Key.Position;
                    update.EngineThrottlePct = (byte) (otherPlayer.Value.GetComponent<SEngineComponent>().Speed * 255f);
                    update.HasOrientation = true;
                    update.Orientation = otherPlayer.Key.Orientation;
                    update.HasHealth = true;
                    update.HasHull = true;
                    update.HullHp = (int) (otherPlayer.Value.GetComponent<HealthComponent>().CurrentHealth);
                    ps.Add(update);
                }
                foreach (var obj in updatingObjects)
                {
                    var update = new PackedShipUpdate();
                    update.ID = obj.NetID;
                    update.HasPosition = true;
                    var tr = obj.WorldTransform;
                    update.Position = Vector3.Transform(Vector3.Zero, tr);
                    if (obj.TryGetComponent<SEngineComponent>(out var engine))
                    {
                        update.EngineThrottlePct = (byte) (engine.Speed * 255f);
                    }
                    update.HasOrientation = true;
                    update.Orientation = tr.ExtractRotation();
                    if (obj.TryGetComponent<HealthComponent>(out var health))
                    {
                        update.HasHealth = true;
                        update.HasHull = true;
                        update.HullHp = (int) health.CurrentHealth;
                    }
                    ps.Add(update);
                }
                player.Key.SendUpdate(new ObjectUpdatePacket()
                {
                    Tick = tick,
                    PlayerHealth = phealth,
                    Updates = ps.ToArray()
                });
            }
        }

        public void Finish()
        {
            GameWorld.Dispose();
        }
    }
}
