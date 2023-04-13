// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.GameData.Items;
using LibreLancer.GameData.World;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Physics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server
{
    public class ServerWorld
    {
        public Dictionary<Player, GameObject> Players = new Dictionary<Player, GameObject>();
        ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        public GameWorld GameWorld;
        public GameServer Server;
        public StarSystem System;
        public NPCManager NPCs;
        object _idLock = new object();

        private NetIDGenerator idGen = new NetIDGenerator();

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
        
        public ServerWorld(StarSystem system, GameServer server)
        {
            Server = server;
            System = system;
            GameWorld = new GameWorld(null, () => server.TotalTime);
            GameWorld.Server = this;
            GameWorld.LoadSystem(system, server.Resources, true);
            GameWorld.Physics.OnCollision += PhysicsOnCollision;
            NPCs = new NPCManager(this);
        }

        private void PhysicsOnCollision(PhysicsObject obja, PhysicsObject objb)
        {
            if (obja.Tag is GameObject g1 && 
                objb.Tag is GameObject g2)
            {
                if (g1.Kind == GameObjectKind.Missile)
                {
                    var msl = g1.GetComponent<SMissileComponent>();
                    if (msl?.Target == g2) {
                        ExplodeMissile(g1);
                    }
                }
                else if (g2.Kind == GameObjectKind.Missile)
                {
                    var msl = g2.GetComponent<SMissileComponent>();
                    if (msl?.Target == g1) {
                        ExplodeMissile(g2);
                    }
                }
            }
        }

        public void ExplodeMissile(GameObject obj)
        {
            var missile = obj.GetComponent<SMissileComponent>();
            var pos = Vector3.Transform(Vector3.Zero, obj.LocalTransform);
            
            obj.Unregister(GameWorld.Physics);
            GameWorld.RemoveObject(obj);
            updatingObjects.Remove(obj);
            idGen.Free(obj.NetID);
            foreach (var p in Players) 
                p.Key.RemoteClient.DestroyMissile(obj.NetID, true);
            if (missile.Missile.Explosion != null)
            {
                foreach (var other in GameWorld.Physics.SphereTest(pos, missile.Missile.Explosion.Radius))
                {
                    if (other.Tag is GameObject g &&
                        g.TryGetComponent<SHealthComponent>(out var health))
                    {
                        health.Damage(missile.Missile.Explosion.HullDamage, missile.Missile.Explosion.EnergyDamage);
                        if(missile.Owner != null && 
                           g.TryGetComponent<SNPCComponent>(out var npc))
                            npc.OnProjectileHit(missile.Owner);
                    }
                }
            }
            
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
            foreach(var o in withAnimations)
                UpdateAnimations(o, player);
            var obj = new GameObject(player.Character.Ship, Server.Resources, false, true) { World = GameWorld };
            foreach(var item in player.Character.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                EquipmentObjectManager.InstantiateEquipment(obj, Server.Resources, null, EquipmentType.Server, item.Hardpoint, item.Equipment);
            obj.Components.Add(new SPlayerComponent(player, obj));
            obj.Components.Add(new SHealthComponent(obj)
            {
                CurrentHealth = player.Character.Ship.Hitpoints, 
                MaxHealth = player.Character.Ship.Hitpoints
            });
            obj.Components.Add(new ShipPhysicsComponent(obj) { Ship = player.Character.Ship });
            if (player == Server.LocalPlayer) obj.Nickname = "Player"; //HACK: Set local player ID for mission script
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
                health.Damage(munition.Def.HullDamage, munition.Def.EnergyDamage);
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

        public void FireMissile(Matrix4x4 transform, MissileEquip missile, float muzzleVelocity, GameObject owner, GameObject target)
        {
            actions.Enqueue(() =>
            {
                var go = new GameObject(missile.ModelFile.LoadFile(Server.Resources), Server.Resources, false, true);
                go.SetLocalTransform(transform);
                go.Kind = GameObjectKind.Missile;
                go.NetID = idGen.Allocate();
                go.World = GameWorld;
                go.PhysicsComponent.Mass = 1;
                go.Components.Add(new SMissileComponent(go, missile)
                {
                    Target = target, Owner = owner,
                    Speed = owner.PhysicsComponent.Body.LinearVelocity.Length() + muzzleVelocity
                });
                go.Register(GameWorld.Physics);
                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                foreach (var p in Players) {
                    p.Key.RemoteClient.SpawnMissile(go.NetID, p.Value != owner, missile.CRC, Vector3.Transform(Vector3.Zero, transform), transform.ExtractRotation());
                }
            });
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
                    var pdata = GameWorld.Projectiles.GetData(Server.GameData.Equipment.Get(p.Gun) as GunEquipment);
                    GameWorld.Projectiles.SpawnProjectile(Players[owner], p.Hardpoint, pdata, p.Start, p.Heading);
                }
            });
        }

        GameObject GetObject(bool crc, int id)
        {
            if (id == 0) return null;
            if (crc)
            {
                return GameWorld.GetObject((uint)id);
            }
            else
            {
                return GameWorld.GetFromNetID(id);
            }
        }

        public void FireMissiles(MissileFireCmd[] missiles, Player owner)
        {
            actions.Enqueue(() =>
            {
                var go = Players[owner];
                foreach (var m in missiles)
                {
                    var x = go.Children.FirstOrDefault(
                        c => m.Hardpoint.Equals(c.Attachment?.Name, StringComparison.OrdinalIgnoreCase));
                    if (x?.TryGetComponent<MissileLauncherComponent>(out var ml) ?? false)
                    {
                        ml.Fire(Vector3.Zero, GetObject(m.TargetIsCrc, m.Target));
                    }
                }
            });
        }

        public void ActivateLane(GameObject obj, bool left)
        {
            foreach (var p in Players)
            {
                p.Key.RemoteClient.TradelaneActivate(obj.NicknameCRC, left);
            }
        }
        
        public void DeactivateLane(GameObject obj, bool left)
        {
            foreach (var p in Players)
            {
                p.Key.RemoteClient.TradelaneDeactivate(obj.NicknameCRC, left);
            }
        }

        void UpdateAnimations(GameObject obj, Player player)
        {
            int id = 0;
            bool sysObj = false;
            if (!string.IsNullOrEmpty(obj.Nickname)) {
                id = (int) obj.NicknameCRC;
                sysObj = true;
            }
            else {
                id = obj.NetID;
            }
            player.RemoteClient.UpdateAnimations(sysObj, id, obj.AnimationComponent.Serialize().ToArray());
        }

        private List<GameObject> withAnimations = new List<GameObject>();
        
        public void StartAnimation(GameObject obj)
        {
            if(!withAnimations.Contains(obj))
                withAnimations.Add(obj);
            foreach (var p in Players)
                UpdateAnimations(obj, p.Key);
        }
        
        void RemoveObjectInternal(GameObject obj)
        {
            obj.Unregister(GameWorld.Physics);
            GameWorld.RemoveObject(obj);
            withAnimations.Remove(obj);
            updatingObjects.Remove(obj);
        }

        public void RemovePlayer(Player player)
        {
            actions.Enqueue(() =>
            {
                RemoveObjectInternal(Players[player]);
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
                RemoveObjectInternal(obj);
                spawnedNPCs.Remove(obj);
                idGen.Free(obj.NetID);
                foreach (var p in Players) p.Key.Despawn(obj.NetID);
            });
        }

        public void InputsUpdate(Player player, InputUpdatePacket input)
        {
            actions.Enqueue(() =>
            {
                var phys = Players[player].GetComponent<SPlayerComponent>();
                phys.QueueInput(input);
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
                gameobj.NetID = idGen.Allocate();
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
                    var ship = Server.GameData.Ships.Get(archetype);
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
                var id = idGen.Allocate();
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
            obj.NetID = idGen.Allocate();
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
            string affiliation = null;
            if (obj.TryGetComponent<SNPCComponent>(out var npc))
                affiliation = npc.Faction?.Nickname;
            p.RemoteClient.SpawnObject(obj.NetID, obj.Name, affiliation, pos, orient, obj.GetComponent<SNPCComponent>().Loadout);
        }

        public void PartDisabled(GameObject obj, string part)
        {
            foreach (Player p in Players.Keys)
                p.SendDestroyPart(obj.NetID, part);
        }

        public void LocalChatMessage(Player player, string message)
        {
            actions.Enqueue(() =>
            {
                var pObj = Players[player];
                player.RemoteClient.ReceiveChatMessage(ChatCategory.Local, player.Name, message);
                foreach (var obj in GameWorld.SpatialLookup.GetNearbyObjects(pObj,
                             Vector3.Transform(Vector3.Zero, pObj.LocalTransform), 15000))
                {
                    if (obj.TryGetComponent<SPlayerComponent>(out var other)) {
                        other.Player.RemoteClient.ReceiveChatMessage(ChatCategory.Local, player.Name, message);
                    }
                }
            });
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
            NPCs.FrameStart();
            GameWorld.Update(delta);
            //projectiles
            if (GameWorld.Projectiles.HasQueued)
            {
                var queue = GameWorld.Projectiles.GetQueue();
                foreach(var p in Players)
                    p.Key.RemoteClient.SpawnProjectiles(queue);
            }
            //Network update tick
            SendWorldUpdates(totalTime);
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

        IEnumerable<GameObject> GetUpdatingObjects()
        {
            foreach (var obj in updatingObjects) yield return obj;
            foreach (var obj in GameWorld.Objects)
            {
                if (obj.SystemObject == null) continue;
                if (obj.TryGetComponent<SSolarComponent>(out var docking) &&
                    docking.SendSolarUpdate)
                    yield return obj;
            }
        }

        //This could do with some work
        void SendWorldUpdates(double tick)
        {
            tick *= 1000.0;
            while (tick > uint.MaxValue) tick -= uint.MaxValue;
            
            foreach(var player in Players)
            {
                var tr = player.Value.WorldTransform;
                player.Key.Position = Vector3.Transform(Vector3.Zero, tr);
                player.Key.Orientation = tr.ExtractRotation();
            }

            var toUpdate = GetUpdatingObjects().ToArray();

            foreach (var player in Players)
            {
                List<ObjectUpdate> ps = new List<ObjectUpdate>();
                var phealthcomponent = player.Value.GetComponent<SHealthComponent>();
                var phealth = phealthcomponent.CurrentHealth;
                var pshieldComponent = player.Value.GetChildComponents<SShieldComponent>().FirstOrDefault();
                float pshield = 0;
                if (pshieldComponent != null)
                    pshield = pshieldComponent.Health / pshieldComponent.Equip.Def.MaxCapacity;
                var selfPlayer = player.Value.GetComponent<SPlayerComponent>();
                foreach (var obj in toUpdate)
                {
                    //Skip self
                    if (obj.TryGetComponent<SPlayerComponent>(out var pComp) &&
                        pComp.Player == player.Key)
                        continue;
                    //Update object
                    var update = new ObjectUpdate();
                    if (obj.SystemObject == null)
                    {
                        update.ID = obj.NetID;
                        update.IsCRC = false;
                    }
                    else
                    {
                        //Static Solar
                        update.ID = (int) obj.NicknameCRC;
                        update.IsCRC = true;
                    }
                    
                    var tr = obj.WorldTransform;
                    update.Position = Vector3.Transform(Vector3.Zero, tr);
                    update.Orientation = tr.ExtractRotation();
                    if (obj.PhysicsComponent != null)
                    {
                        update.SetVelocity(
                            obj.PhysicsComponent.Body.LinearVelocity,
                            obj.PhysicsComponent.Body.AngularVelocity
                            );
                    }

                    if (obj.TryGetComponent<SEngineComponent>(out var eng))
                        update.Throttle = eng.Speed;
                    if (obj.TryGetComponent<SNPCComponent>(out var npc))
                    {
                        if (npc.HostileNPCs.Contains(player.Value)) {
                            update.RepToPlayer = RepAttitude.Hostile;
                        }
                        else {
                            update.RepToPlayer = RepAttitude.Neutral;
                        }
                    }
                    if(obj.TryGetComponent<ShipPhysicsComponent>(out var objPhysics))
                    {
                        switch (objPhysics.EngineState)
                        {
                            case EngineStates.CruiseCharging:
                                update.CruiseThrust = CruiseThrustState.CruiseCharging;
                                break;
                            case EngineStates.Cruise:
                                update.CruiseThrust = CruiseThrustState.Cruising;
                                break;
                            case EngineStates.Standard when objPhysics.ThrustEnabled:
                                update.CruiseThrust = CruiseThrustState.Thrusting;
                                break;
                        }
                    }
                    if (obj.TryGetComponent<SHealthComponent>(out var health))
                    {
                        update.HullValue = health.CurrentHealth;
                        var sh = obj.GetChildComponents<SShieldComponent>().FirstOrDefault();
                        if (sh != null)
                        {
                            update.ShieldValue = sh.Health;
                        }
                    }
                    if (obj.TryGetComponent<WeaponControlComponent>(out var weapons))
                    {
                        update.Guns = weapons.GetRotations();
                    }
                    ps.Add(update);
                }

                var phys = player.Value.GetComponent<ShipPhysicsComponent>();
                var newUpdates = ps.ToArray();
                var state = new PlayerAuthState
                {
                    Health = phealth,
                    Shield = pshield,
                    Position = player.Key.Position,
                    Orientation = player.Key.Orientation,
                    LinearVelocity = player.Value.PhysicsComponent.Body.LinearVelocity,
                    AngularVelocity = MathHelper.ApplyEpsilon(player.Value.PhysicsComponent.Body.AngularVelocity),
                    CruiseAccelPct = phys.CruiseAccelPct,
                    CruiseChargePct = phys.ChargePercent
                };
                if (player.Key.SinglePlayer)
                {
                    player.Key.SendSPUpdate(new SPUpdatePacket()
                    {
                        Tick = (uint)tick,
                        InputSequence = selfPlayer.SequenceApplied,
                        PlayerState = state,
                        Updates = newUpdates,
                    });
                }
                else
                {
                    selfPlayer.GetAcknowledgedState(out var oldTick, out var oldState, out var oldUpdates);
                    var packet = new PackedUpdatePacket();
                    packet.Tick = (uint) tick;
                    packet.OldTick = oldTick;
                    packet.InputSequence = selfPlayer.SequenceApplied;
                    selfPlayer.EnqueueState((uint)tick, state,  packet.SetUpdates(state, oldState, oldUpdates, newUpdates, player.Key.HpidWriter)); ;
                    player.Key.SendMPUpdate(packet);
                }
            }
        }

        public void Finish()
        {
            GameWorld.Dispose();
        }
    }
}