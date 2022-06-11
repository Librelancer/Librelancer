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

        
        int GenerateID()
        {
            lock (_idLock)
            {
                var retVal = mId--;
                if (mId < int.MinValue + 2) mId = -1;
                return retVal;
            }
        }

        public bool Paused => paused;
        
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
            foreach (var p in Players)
            {
                player.SpawnPlayer(p.Key);
                p.Key.SpawnPlayer(player);
            }
            player.SendSolars(SpawnedSolars);
            foreach(var npc in spawnedNPCs)
                SpawnShip(npc, player);
            var obj = new GameObject(player.Character.Ship, Server.Resources, false, true) { World = GameWorld };
            foreach(var item in player.Character.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                EquipmentObjectManager.InstantiateEquipment(obj, Server.Resources, EquipmentType.Server, item.Hardpoint, item.Equipment);
            obj.Components.Add(new SPlayerComponent(player, obj));
            obj.Components.Add(new SHealthComponent(obj)
            {
                CurrentHealth = player.Character.Ship.Hitpoints, 
                MaxHealth = player.Character.Ship.Hitpoints
            });
            obj.Components.Add(new ShipPhysicsComponent(obj) { Ship = player.Character.Ship });

            obj.NetID = player.ID;
            GameWorld.AddObject(obj);
            obj.Register(GameWorld.Physics);
            Players[player] = obj;
            Players[player].SetLocalTransform(Matrix4x4.CreateFromQuaternion(orientation) *
                                              Matrix4x4.CreateTranslation(position));
            updatingObjects.Add(obj);
        }

        public void EffectSpawned(GameObject obj)
        {
            foreach (var p in Players)
            {
                p.Key.RemoteClient.UpdateEffects(obj.NetID, obj.GetComponent<SFuseRunnerComponent>().Effects.ToArray());
            }
        }
        

        public void ProjectileHit(GameObject obj, GameObject owner, MunitionEquip munition)
        {
            if (obj.TryGetComponent<SHealthComponent>(out var health)) {
                health.Damage(munition.Def.HullDamage);
            }
            if (obj.TryGetComponent<SNPCComponent>(out var npc)) {
                npc.OnProjectileHit(owner);
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

            foreach (var p in Players)
            {
                p.Key.RemoteClient.StartAnimation(sysObj, id, script);
            }
        }

        public void RemovePlayer(Player player)
        {
            actions.Enqueue(() =>
            {
                Players[player].Unregister(GameWorld.Physics);
                GameWorld.RemoveObject(Players[player]);
                updatingObjects.Remove(Players[player]);
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

        public void InputsUpdate(Player player, InputUpdatePacket input)
        {
            actions.Enqueue(() =>
            {
                var phys = Players[player].GetComponent<SPlayerComponent>();
                phys.Inputs.Enqueue(input);
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

        public void SpawnDebris(GameObjectKind kind, string archetype, string part, Matrix4x4 transform, float mass, Vector3 initialForce)
        {
            actions.Enqueue(() =>
            {
                RigidModel mdl;
                if (kind == GameObjectKind.Ship)
                {
                    var ship = Server.GameData.GetShip(archetype);
                    mdl = ((IRigidModelFile) ship.ModelFile.LoadFile(Server.Resources)).CreateRigidModel(false);
                }
                else
                {
                    var arch = Server.GameData.GetSolarArchetype(archetype);
                    mdl = ((IRigidModelFile) arch.ModelFile.LoadFile(Server.Resources)).CreateRigidModel(false);
                }
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
                    p.SpawnDebris(go.NetID, kind, archetype, part, transform, mass);
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
        public bool Update(double delta, double totalTime)
        {
            //Avoid locks during Update
            Action act;
            while(actions.Count > 0 && actions.TryDequeue(out act)){ act(); }
            //pause
            if (paused) return true;
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
            SendPositionUpdates(totalTime);
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

        //This could do with some work
        void SendPositionUpdates(double tick)
        {
            tick *= 1000.0;
            while (tick > uint.MaxValue) tick -= uint.MaxValue;
            
            foreach(var player in Players)
            {
                var tr = player.Value.WorldTransform;
                player.Key.Position = Vector3.Transform(Vector3.Zero, tr);
                player.Key.Orientation = tr.ExtractRotation();
            }
           
            foreach (var player in Players)
            {
                List<PackedShipUpdate> ps = new List<PackedShipUpdate>();
                var phealthcomponent = player.Value.GetComponent<SHealthComponent>();
                var phealth = phealthcomponent.CurrentHealth;
                var pshield = phealthcomponent.ShieldHealth;
                
                foreach (var obj in updatingObjects)
                {
                    //Skip self
                    if (obj.TryGetComponent<SPlayerComponent>(out var pComp) &&
                        pComp.Player == player.Key)
                        continue;
                    //Update object
                    var update = new PackedShipUpdate();
                    update.ID = obj.NetID;
                    update.IsCRC = false;
                    update.HasPosition = true;
                    var tr = obj.WorldTransform;
                    update.Position = Vector3.Transform(Vector3.Zero, tr);
                    update.Orientation = tr.ExtractRotation();
                    if (obj.PhysicsComponent != null)
                    {
                        update.LinearVelocity = obj.PhysicsComponent.Body.LinearVelocity;
                        update.AngularVelocity = MathHelper.ApplyEpsilon(obj.PhysicsComponent.Body.AngularVelocity);
                    }
                    if(obj.TryGetComponent<SEngineComponent>(out var engine))
                    {
                        update.Throttle = engine.Speed;
                    }
                    if (obj.TryGetComponent<SHealthComponent>(out var health))
                    {
                        if (health.CurrentHealth < health.MaxHealth)
                        {
                            update.Hull = true;
                            update.HullValue = health.CurrentHealth;
                        }

                        if (health.ShieldHealth <= 0)
                            update.Shield = 0;
                        else if (health.ShieldHealth >= 1)
                            update.Shield = 1;
                        else {
                            update.Shield = 2;
                            update.ShieldValue = health.ShieldHealth;
                        }
                    }
                    if (obj.TryGetComponent<WeaponControlComponent>(out var weapons))
                    {
                        update.Guns = weapons.GetRotations();
                    }
                    ps.Add(update);
                }

                var state = new PlayerAuthState
                {
                    Health = phealth,
                    Shield = pshield,
                    Position = player.Key.Position,
                    Orientation = player.Key.Orientation,
                    LinearVelocity = player.Value.PhysicsComponent.Body.LinearVelocity,
                    AngularVelocity = MathHelper.ApplyEpsilon(player.Value.PhysicsComponent.Body.AngularVelocity)
                };
                player.Key.SendUpdate(new ObjectUpdatePacket()
                {
                    Tick = (uint)tick,
                    InputSequence = player.Value.GetComponent<SPlayerComponent>().SequenceApplied,
                    PlayerState = state,
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