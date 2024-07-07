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

        public NetIDGenerator IdGenerator = new NetIDGenerator();

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
            GameWorld = new GameWorld(null, server.Resources, () => server.TotalTime);
            GameWorld.Server = this;
            GameWorld.LoadSystem(system, server.Resources, true);
            GameWorld.Physics.OnCollision += PhysicsOnCollision;
            NPCs = new NPCManager(this);
        }

        private void PhysicsOnCollision(PhysicsObject obja, PhysicsObject objb)
        {
            if (obja == null || objb == null) //Asteroid collision
                return;
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
            if ((obj.Flags & GameObjectFlags.Exists) == 0)
                return;
            var missile = obj.GetComponent<SMissileComponent>();
            var pos = obj.LocalTransform.Position;
            obj.Unregister(GameWorld.Physics);
            GameWorld.RemoveObject(obj);
            updatingObjects.Remove(obj);
            IdGenerator.Free(obj.NetID);
            foreach (var p in Players)
                p.Key.RpcClient.DestroyMissile(obj.NetID, true);
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
            var obj = new GameObject(player.Character.Ship, Server.Resources, false, true) { World = GameWorld };
            foreach(var item in player.Character.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                EquipmentObjectManager.InstantiateEquipment(obj, Server.Resources, null, EquipmentType.Server, item.Hardpoint, item.Equipment);
            obj.AddComponent(new SPlayerComponent(player, obj));
            obj.AddComponent(new WeaponControlComponent(obj));
            obj.AddComponent(new SHealthComponent(obj)
            {
                CurrentHealth = player.Character.Ship.Hitpoints,
                MaxHealth = player.Character.Ship.Hitpoints
            });
            obj.AddComponent(new SFuseRunnerComponent(obj) { DamageFuses = player.Character.Ship.Fuses });
            obj.AddComponent(new ShipPhysicsComponent(obj) { Ship = player.Character.Ship });
            if (player == Server.LocalPlayer) obj.Nickname = "Player"; //HACK: Set local player ID for mission script
            obj.NetID = player.ID;
            obj.Flags |= GameObjectFlags.Player;
            GameWorld.AddObject(obj);
            obj.Register(GameWorld.Physics);
            FLLog.Debug("Server", $"Spawning player with rotation {orientation}");
            obj.SetLocalTransform(new Transform3D(position, orientation));
            foreach (var p in Players)
            {
                player.SpawnPlayer(p.Key);
                p.Key.SpawnPlayer(player);
            }
            Players[player] = obj;
            player.SendSolars(SpawnedSolars);
            foreach(var npc in spawnedNPCs)
                SpawnNpcShip(npc, player);
            foreach(var o in withAnimations)
                UpdateAnimations(o, player);
            updatingObjects.Add(obj);
        }

        public void EffectSpawned(GameObject obj)
        {
            foreach (var p in Players)
            {
                p.Key.RpcClient.UpdateEffects(obj, obj.GetComponent<SFuseRunnerComponent>().Effects.ToArray());
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

        public void RequestDock(Player player, ObjNetId id)
        {
            actions.Enqueue(() =>
            {
                var obj = Players[player];
                FLLog.Info("Server", $"{player.Name} requested dock at {id}");
                var dock = GetObject(id);
                if(dock == null)
                    FLLog.Warning("Server", $"Dock object {id} does not exist.");
                else
                {
                    var component = dock.GetComponent<SDockableComponent>();
                    if(component == null)
                        FLLog.Warning("Server", $"object {dock.Nickname} is not dockable.");
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

        public void FireMissile(Transform3D transform, MissileEquip missile, float muzzleVelocity, GameObject owner, GameObject target)
        {
            actions.Enqueue(() =>
            {
                var go = new GameObject(missile.ModelFile.LoadFile(Server.Resources), Server.Resources, false, true);
                go.SetLocalTransform(transform);
                go.Kind = GameObjectKind.Missile;
                go.NetID = IdGenerator.Allocate();
                go.World = GameWorld;
                go.PhysicsComponent.Mass = 1;
                go.AddComponent(new SMissileComponent(go, missile)
                {
                    Target = target, Owner = owner,
                    Speed = owner.PhysicsComponent.Body.LinearVelocity.Length() + muzzleVelocity
                });
                go.Register(GameWorld.Physics);
                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                foreach (var p in Players) {
                    p.Key.RpcClient.SpawnMissile(go.NetID, p.Value != owner, missile.CRC, transform.Position, transform.Orientation);
                }
            });
        }

        public void FireProjectiles(ProjectileFireCommand projectiles, Player owner)
        {
            actions.Enqueue(() =>
            {
                var go = Players[owner];
                if (go.TryGetComponent<WeaponControlComponent>(out var wo))
                {
                    int tgtUnique = 0;
                    for (int i = 0; i < wo.NetOrderWeapons.Length; i++)
                    {
                        if ((projectiles.Guns & (1UL << i)) == 0)
                            continue;
                        var target = projectiles.Target;
                        if ((projectiles.Unique & (1UL << i)) != 0)
                            target = projectiles.OtherTargets[tgtUnique++];
                        if (!wo.NetOrderWeapons[i].Fire(target)) {
                            FLLog.Debug("Server", $"Request failed firing {wo.NetOrderWeapons[i].Parent.Attachment}");
                        }
                    }
                }
            });
        }

        public GameObject GetObject(ObjNetId id)
        {
            if (id.Value == 0) return null;
            return GameWorld.GetObject(id);
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
                        ml.Fire(Vector3.Zero, GetObject(m.Target));
                    }
                }
            });
        }

        public void ActivateLane(GameObject obj, bool left)
        {
            foreach (var p in Players)
            {
                p.Key.RpcClient.TradelaneActivate(obj.NicknameCRC, left);
            }
        }

        public void DeactivateLane(GameObject obj, bool left)
        {
            foreach (var p in Players)
            {
                p.Key.RpcClient.TradelaneDeactivate(obj.NicknameCRC, left);
            }
        }

        void UpdateAnimations(GameObject obj, Player player)
        {
            player.RpcClient.UpdateAnimations(obj, obj.AnimationComponent.Serialize().ToArray());
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

        public void RemovePlayer(Player player, bool exploded)
        {
            actions.Enqueue(() =>
            {
                RemoveObjectInternal(Players[player]);
                Players.Remove(player);
                foreach(var p in Players)
                {
                    p.Key.Despawn(player.ID, exploded);
                }
                Interlocked.Decrement(ref PlayerCount);
            });
        }

        public void RemoveNPC(GameObject obj, bool exploded)
        {
            actions.Enqueue(() =>
            {
                RemoveObjectInternal(obj);
                spawnedNPCs.Remove(obj);
                IdGenerator.Free(obj.NetID);
                foreach (var p in Players) p.Key.Despawn(obj.NetID, exploded);
            });
        }

        public void InputsUpdate(Player player, InputUpdatePacket input)
        {
            actions.Enqueue(() =>
            {
                if (Players.TryGetValue(player, out var p))
                {
                    var phys = p.GetComponent<SPlayerComponent>();
                    phys.QueueInput(input);
                }
            });
        }

        public Dictionary<string, GameObject> SpawnedSolars = new Dictionary<string, GameObject>();
        List<GameObject> updatingObjects = new List<GameObject>();
        List<GameObject> spawnedNPCs = new List<GameObject>();

        public void SpawnSolar(string nickname, string archetype, string loadout, Vector3 position, Quaternion orientation, int idsName = 0, string? dockWith = null)
        {
            actions.Enqueue(() =>
            {
                var arch = Server.GameData.GetSolarArchetype(archetype);
                var gameobj = new GameObject(arch, Server.Resources, false);
                gameobj.ArchetypeName = archetype;
                gameobj.NetID = IdGenerator.Allocate();
                if (idsName != 0)
                    gameobj.Name = new ObjectName(idsName);
                gameobj.SetLocalTransform(new Transform3D(position, orientation));
                gameobj.Nickname = nickname;
                gameobj.World = GameWorld;
                gameobj.Register(GameWorld.Physics);
                gameobj.CollisionGroups = arch.CollisionGroups;
                GameWorld.AddObject(gameobj);
                SpawnedSolars.Add(nickname, gameobj);
                if (!string.IsNullOrWhiteSpace(dockWith))
                {
                    var act = new DockAction() {Kind = DockKinds.Base, Target = dockWith};
                    gameobj.AddComponent(new SDockableComponent(gameobj, arch.DockSpheres.ToArray())
                    {
                        Action = act
                    });
                }
                foreach(Player p in Players.Keys)
                    p.SendSolars(SpawnedSolars);
            });
        }

        public void SpawnDebris(GameObjectKind kind, string archetype, string part, Transform3D transform, float mass, Vector3 initialForce)
        {
            actions.Enqueue(() =>
            {
                ModelResource src;
                if (kind == GameObjectKind.Ship)
                {
                    src = Server.GameData.Ships.Get(archetype).ModelFile.LoadFile(Server.Resources);
                }
                else
                {
                    src = Server.GameData.GetSolarArchetype(archetype).ModelFile.LoadFile(Server.Resources);
                }
                var collider = src.Collision;
                var mdl = ((IRigidModelFile) src.Drawable).CreateRigidModel(false, Server.Resources);
                var newpart = mdl.Parts[part].Clone();
                var newmodel = new RigidModel()
                {
                    Root = newpart,
                    AllParts = new[] { newpart },
                    MaterialAnims = mdl.MaterialAnims,
                    Path = mdl.Path,
                };
                var id = IdGenerator.Allocate();
                var go = new GameObject(newmodel, collider, Server.Resources, part, mass, false);
                go.NetID = id;
                go.SetLocalTransform(transform);
                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                go.Register(GameWorld.Physics);
                go.PhysicsComponent.Body.Impulse(initialForce);
                go.PhysicsComponent.Body.SetDamping(0.5f, 0.2f);
                //Spawn debris
                foreach (Player p in Players.Keys)
                {
                    p.SpawnDebris(go.NetID, kind, archetype, part, transform, mass);
                }
            });
        }

        public void OnNPCSpawn(GameObject obj)
        {
            obj.NetID = IdGenerator.Allocate();
            obj.World = GameWorld;
            obj.Register(GameWorld.Physics);
            GameWorld.AddObject(obj);
            updatingObjects.Add(obj);
            spawnedNPCs.Add(obj);
            foreach (Player p in Players.Keys)
            {
                SpawnNpcShip(obj, p);
            }
        }

        void SpawnNpcShip(GameObject obj, Player p)
        {
            var npcInfo = obj.GetComponent<SNPCComponent>();
            var spawnInfo = new ShipSpawnInfo()
            {
                Name = obj.Name,
                Position = obj.LocalTransform.Position,
                Orientation = obj.LocalTransform.Orientation,
                Affiliation = npcInfo.Faction?.CRC ?? 0,
                CommHead = npcInfo.CommHead?.CRC ?? 0,
                CommBody = npcInfo.CommBody?.CRC ?? 0,
                CommHelmet = npcInfo.CommHelmet?.CRC ?? 0,
                Loadout = npcInfo.Loadout,
            };
            p.RpcClient.SpawnShip(obj.NetID, spawnInfo);
        }

        public void PartDisabled(GameObject obj, string part)
        {
            foreach (Player p in Players.Keys)
                p.RpcClient.DestroyPart(obj, part);
        }

        public void LocalChatMessage(Player player, BinaryChatMessage message)
        {
            actions.Enqueue(() =>
            {
                var pObj = Players[player];
                player.RpcClient.ReceiveChatMessage(ChatCategory.Local, BinaryChatMessage.PlainText(player.Name), message);
                foreach (var obj in GameWorld.SpatialLookup.GetNearbyObjects(pObj,
                             pObj.LocalTransform.Position, 15000))
                {
                    if (obj.TryGetComponent<SPlayerComponent>(out var other)) {
                        other.Player.RpcClient.ReceiveChatMessage(ChatCategory.Local, BinaryChatMessage.PlainText(player.Name+": "), message);
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
                    p.Despawn(s.NetID, false);
            });
        }

        public uint CurrentTick { get; private set; }

        private double noPlayersTime;
        private double maxNoPlayers = 2.0;
        public bool Update(double delta, double totalTime, uint currentTick)
        {
            //Avoid locks during Update
            CurrentTick = currentTick;
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
                var queue = GameWorld.Projectiles.GetSpawnQueue();
                foreach(var p in Players)
                    p.Key.RpcClient.SpawnProjectiles(queue);
            }
            //Network update tick
            SendWorldUpdates(currentTick);
            UpdateDebugInfo();
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

        void UpdateDebugInfo()
        {
            if (Server.LocalPlayer != null &&
                Server.SendDebugInfo &&
                Players.TryGetValue(Server.LocalPlayer, out var go) &&
                go.Flags.HasFlag(GameObjectFlags.Exists))
            {
                var pc = go.GetComponent<SPlayerComponent>();
                if (pc.SelectedObject != null && pc.SelectedObject.TryGetComponent<SNPCComponent>(out var npc))
                {
                    Server.ReportDebugInfo(npc.GetDebugInfo());
                }
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
        void SendWorldUpdates(uint tick)
        {
            foreach(var player in Players)
            {
                var tr = player.Value.WorldTransform;
                player.Key.Position = tr.Position;
                player.Key.Orientation = tr.Orientation;
            }

            var toUpdate = GetUpdatingObjects().ToArray();

            foreach (var player in Players)
            {
                List<ObjectUpdate> ps = new List<ObjectUpdate>();
                var phealthcomponent = player.Value.GetComponent<SHealthComponent>();
                var phealth = phealthcomponent.CurrentHealth;
                var pshieldComponent = player.Value.GetFirstChildComponent<SShieldComponent>();
                float pshield = 0;
                if (pshieldComponent != null)
                    pshield = pshieldComponent.Health;
                var selfPlayer = player.Value.GetComponent<SPlayerComponent>();
                foreach (var obj in toUpdate)
                {
                    //Skip self
                    if (obj.TryGetComponent<SPlayerComponent>(out var pComp) &&
                        pComp.Player == player.Key)
                        continue;
                    //Update object
                    var update = new ObjectUpdate();
                    update.ID = new ObjNetId(obj.NetID);
                    var tr = obj.WorldTransform;
                    update.Position = tr.Position;
                    update.Orientation = tr.Orientation;
                    if (obj.PhysicsComponent != null)
                    {
                        update.SetVelocity(
                            obj.PhysicsComponent.Body.LinearVelocity,
                            obj.PhysicsComponent.Body.AngularVelocity
                            );
                    }

                    if (obj.TryGetComponent<SEngineComponent>(out var eng))
                        update.Throttle = eng.Speed;
                    if (obj.TryGetComponent<SRepComponent>(out var rep))
                    {
                        update.RepToPlayer = rep.GetRep(player.Value);
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
                        update.HullValue = (long)health.CurrentHealth;
                        var sh = obj.GetFirstChildComponent<SShieldComponent>();
                        if (sh != null)
                        {
                            update.ShieldValue = (long)sh.Health;
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
                        Tick = tick,
                        InputSequence = selfPlayer.LatestReceived,
                        PlayerState = state,
                        Updates = newUpdates,
                    });
                }
                else
                {
                    selfPlayer.GetAcknowledgedState(out var oldTick, out var oldState, out var oldUpdates);
                    var packet = new PackedUpdatePacket();
                    packet.Tick = tick;
                    packet.OldTick = oldTick;
                    packet.InputSequence = selfPlayer.LatestReceived;
                    selfPlayer.EnqueueState((uint)tick, state,  packet.SetUpdates(state, oldState, oldUpdates, newUpdates, player.Key.HpidWriter));
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
