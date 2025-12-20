// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Data;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Physics;
using LibreLancer.Resources;
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
        private Random debrisRandom = new();
        object _idLock = new object();

        public NetIDGenerator IdGenerator = new NetIDGenerator();

        UpdatePacker packer = new UpdatePacker();

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
            GameWorld.LoadSystem(system, server.Resources, null, true);
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
                    if (msl?.Target == g2)
                    {
                        ExplodeMissile(g1);
                    }
                }
                else if (g2.Kind == GameObjectKind.Missile)
                {
                    var msl = g2.GetComponent<SMissileComponent>();
                    if (msl?.Target == g1)
                    {
                        ExplodeMissile(g2);
                    }
                }
            }
        }

        public void StartTractor(GameObject obj, GameObject target)
        {
            foreach (var p in Players)
            {
                p.Key.RpcClient.StartTractor(obj, target);
            }
        }

        public void PickupObject(GameObject obj, GameObject pickup)
        {
            if (!pickup.Flags.HasFlag(GameObjectFlags.Exists) ||
                !pickup.TryGetComponent<LootComponent>(out var loot))
            {
                return;
            }
            if (obj.TryGetComponent<AbstractCargoComponent>(out var cargo))
            {
                var newLoot = new List<BasicCargo>();
                int totalRemain = 0;
                int totalCount = 0;
                foreach (var c in loot.Cargo)
                {
                    var remaining = c.Count - cargo.TryAdd(c.Item, c.Count);
                    totalCount += c.Count;
                    totalRemain += remaining;
                    if (remaining > 0)
                    {
                        newLoot.Add(new BasicCargo(c.Item, remaining));
                    }
                }

                if (totalRemain == totalCount)
                {
                    if (obj.TryGetComponent<SPlayerComponent>(out var player))
                    {
                        player.Player.RpcClient.TractorFailed();
                    }
                }
                else if(totalRemain == 0)
                {
                    RemoveSpawnedObject(pickup, false);
                    // Notify mission system that loot has been acquired after removal
                    if (obj.TryGetComponent<SPlayerComponent>(out var playerComponent))
                    {
                        actions.Enqueue(() => Server.LocalPlayer?.MissionRuntime?.LootAcquired(pickup.Nickname, "Player"));
                    }
                }
                else
                {
                    loot.Cargo = newLoot;
                    foreach (var p in Players)
                    {
                        p.Key.RpcClient.UpdateLootObject(pickup,
                            loot.Cargo.Select(x => new NetBasicCargo(x.Item.CRC, x.Count)).ToArray());
                    }
                }
            }
        }


        public void EndTractor(GameObject obj, GameObject target)
        {
            foreach (var p in Players)
            {
                p.Key.RpcClient.EndTractor(obj, target);
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
                        health.Damage(missile.Missile.Explosion.HullDamage, missile.Missile.Explosion.EnergyDamage, missile.Owner);
                        health.OnProjectileHit(missile.Owner);
                    }
                }
            }
        }

        public void LaunchComplete(GameObject obj)
        {
            if (!string.IsNullOrWhiteSpace(obj.Nickname))
            {
                Server.LocalPlayer?.MissionRuntime?.LaunchComplete(obj.Nickname);
            }
        }

        public JumperNpc[] GatherJumpers()
        {
            var msn = Server.LocalPlayer?.MissionRuntime;
            if (msn == null)
                return [];
            var jumpers = new List<JumperNpc>();
            foreach (var npc in msn.Script.Ships.Values)
            {
                if (!npc.Jumper)
                    continue;
                var go = GameWorld.GetObject(npc.Nickname);
                if (go == null)
                    continue;
                jumpers.Add(JumperNpc.FromGameObject(go));
                RemoveSpawnedObject(go, false);
            }
            return jumpers.ToArray();
        }

        public bool TryScanCargo(GameObject obj, out NetLoadout ld)
        {
            if (obj.TryGetComponent<ShipComponent>(out var ship))
            {
                ld = new NetLoadout();
                ld.Items = new();
                ld.ArchetypeCrc = ship.Ship.CRC;
            }
            else
            {
                ld = null;
                return false;
            }
            int id = 1;
            foreach (var item in obj.GetComponents<EquipmentComponent>())
            {
                ld.Items.Add(item.GetDescription(id++));
            }
            foreach (var item in obj.GetChildComponents<EquipmentComponent>())
            {
                ld.Items.Add(item.GetDescription(id++));
            }
            if (obj.TryGetComponent<AbstractCargoComponent>(out var cargo))
            {
                ld.Items.AddRange(cargo.GetCargo(id));
            }
            return true;
        }

        ObjectSpawnInfo BuildSpawnInfo(GameObject obj, GameObject self)
        {
            var info = new ObjectSpawnInfo();
            info.ID = new ObjNetId(obj.NetID);
            info.Nickname = obj.Nickname;
            if (obj.Name is not LootName)
            {
                info.Name = obj.Name;
            }
            var tr = obj.WorldTransform;
            info.Position = tr.Position;
            info.Orientation = tr.Orientation;
            if (obj.TryGetComponent<SRepComponent>(out var rep))
            {
                var r = rep.GetRep(self);
                info.Affiliation = rep.Faction?.CRC ?? 0;
                if (r == RepAttitude.Friendly)
                {
                    info.Flags |= ObjectSpawnFlags.Friendly;
                }

                if (r == RepAttitude.Hostile)
                {
                    info.Flags |= ObjectSpawnFlags.Hostile;
                }
            }

            if (obj.TryGetComponent<SDockableComponent>(out var dock))
            {
                info.Dock = dock.Action;
            }

            info.DestroyedParts = obj.Model.DestroyedParts.ToArray();
            // Fuse effects
            info.Effects = [];
            if (obj.TryGetComponent<SFuseRunnerComponent>(out var fuse)
                && fuse.Effects.Count > 0)
            {
                info.Effects = fuse.Effects.ToArray();
            }

            // Set comm data
            if (obj.TryGetComponent<SNPCComponent>(out var npc))
            {
                info.CommHead = npc.CommHead?.CRC ?? 0;
                info.CommBody = npc.CommBody?.CRC ?? 0;
                info.CommHelmet = npc.CommHelmet?.CRC ?? 0;
            }

            //Actual loadout
            info.Loadout = new NetLoadout();
            info.Loadout.Items = new List<NetShipCargo>();
            if (obj.TryGetComponent<SDebrisComponent>(out var debris))
            {
                info.Flags |= ObjectSpawnFlags.Debris;
                if (debris.Solar)
                    info.Flags |= ObjectSpawnFlags.Solar;
                info.Loadout.ArchetypeCrc = debris.Archetype;
                info.DebrisPart = debris.Part;
            }
            else if (obj.TryGetComponent<ShipComponent>(out var ship))
            {
                info.Loadout.ArchetypeCrc = ship.Ship.CRC;
            }
            else if (obj.Kind == GameObjectKind.Solar)
            {
                info.Flags |= ObjectSpawnFlags.Solar;
                info.Loadout.ArchetypeCrc = FLHash.CreateID(obj.ArchetypeName);
            }
            else if (obj.Kind == GameObjectKind.Loot)
            {
                info.Flags |= ObjectSpawnFlags.Loot;
                info.Loadout.ArchetypeCrc = FLHash.CreateID(obj.ArchetypeName);
            }
            else
            {
                //Shouldn't occur
                throw new InvalidOperationException("BuildSpawnInfo called on non-archetype object");
            }

            if (obj.TryGetComponent<LootComponent>(out var l))
            {
                foreach (var item in l.Cargo)
                {
                    info.Loadout.Items.Add(new NetShipCargo(0, item.Item.CRC, null, 255, item.Count));
                }
            }

            if (obj.TryGetComponent<SHealthComponent>(out var health))
            {
                info.Loadout.Health = health.CurrentHealth;
            }

            foreach (var item in obj.GetComponents<EquipmentComponent>())
            {
                info.Loadout.Items.Add(item.GetDescription());
            }

            foreach (var item in obj.GetChildComponents<EquipmentComponent>())
            {
                info.Loadout.Items.Add(item.GetDescription());
            }

            return info;
        }

        public int PlayerCount;

        public GameObject SpawnPlayer(Player player, Vector3 position, Quaternion orientation)
        {
            player.VisitSystem(System);
            Interlocked.Increment(ref PlayerCount);
            var obj = new GameObject(player.Character.Ship, Server.Resources, false, true) { World = GameWorld };
            foreach (var item in player.Character.Items.Where(x => !string.IsNullOrEmpty(x.Hardpoint)))
                EquipmentObjectManager.InstantiateEquipment(obj, Server.Resources, null, EquipmentType.Server,
                    item.Hardpoint, item.Equipment);
            obj.AddComponent(new SPlayerComponent(player, obj));
            obj.AddComponent(new WeaponControlComponent(obj));
            obj.AddComponent(new SHealthComponent(obj)
            {
                CurrentHealth = player.Character.Ship.Hitpoints,
                MaxHealth = player.Character.Ship.Hitpoints
            });
            obj.AddComponent(new SFuseRunnerComponent(obj) { DamageFuses = player.Character.Ship.Fuses });
            obj.AddComponent(new ShipPhysicsComponent(obj) { Ship = player.Character.Ship });
            obj.AddComponent(new SDestroyableComponent(obj, this));
            if (player == Server.LocalPlayer) obj.Nickname = "Player"; //HACK: Set local player ID for mission script
            obj.NetID = player.ID;
            obj.Flags |= GameObjectFlags.Player;
            GameWorld.AddObject(obj);
            obj.Register(GameWorld.Physics);
            FLLog.Debug("Server", $"Spawning player with rotation {orientation}");
            obj.SetLocalTransform(new Transform3D(position, orientation));
            int objSpawn = 0;
            var allComplexSpawns = new ObjectSpawnInfo[Players.Count + spawnedObjects.Count];
            foreach (var p in Players)
            {
                allComplexSpawns[objSpawn++] = BuildSpawnInfo(p.Value, obj);
                p.Key.RpcClient.SpawnObjects([BuildSpawnInfo(obj, p.Value)]);
            }

            Players[player] = obj;
            foreach (var spawned in spawnedObjects)
            {
                allComplexSpawns[objSpawn++] = BuildSpawnInfo(spawned, obj);
            }

            player.RpcClient.SpawnObjects(allComplexSpawns);
            foreach (var o in withAnimations)
                UpdateAnimations(o, player);
            updatingObjects.Add(obj);
            return obj;
        }

        public void SpawnJumpers(string target, JumperNpc[] jumpers)
        {
            foreach (var j in jumpers)
            {
                NPCs.SpawnJumper(j, Server.LocalPlayer?.MissionRuntime, target);
            }
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
            if (obj.TryGetComponent<SHealthComponent>(out var health))
            {
                health.Damage(munition.Def.HullDamage, munition.Def.EnergyDamage, owner);
                health.OnProjectileHit(owner);
            }
        }

        public void RequestDock(Player player, ObjNetId id)
        {
            actions.Enqueue(() =>
            {
                var obj = Players[player];
                FLLog.Info("Server", $"{player.Name} requested dock at {id}");
                var dock = GetObject(id);
                if (dock == null)
                    FLLog.Warning("Server", $"Dock object {id} does not exist.");
                else
                {
                    var component = dock.GetComponent<SDockableComponent>();
                    if (component == null)
                        FLLog.Warning("Server", $"object {dock.Nickname} is not dockable.");
                    else
                    {
                        component.StartDock(obj, 0);
                    }
                }
            });
        }

        private ConcurrentQueue<(Action, double)> delayedActions = new();

        public void DelayAction(Action action, double delay)
        {
            delayedActions.Enqueue((action, Server.TotalTime + delay));
        }


        public void EnqueueAction(Action a)
        {
            actions.Enqueue(a);
        }

        public void FireMissile(Transform3D transform, MissileEquip missile, float muzzleVelocity, GameObject owner,
            GameObject target)
        {
            actions.Enqueue(() =>
            {
                var go = new GameObject(missile.ModelFile.LoadFile(Server.Resources), Server.Resources, false, true);
                go.SetLocalTransform(transform);
                go.Kind = GameObjectKind.Missile;
                go.NetID = IdGenerator.Allocate();
                go.PhysicsComponent.Mass = 1;
                go.AddComponent(new SMissileComponent(go, missile)
                {
                    Target = target, Owner = owner,
                    Speed = owner.PhysicsComponent.Body.LinearVelocity.Length() + muzzleVelocity
                });
                GameWorld.AddObject(go);
                go.Register(GameWorld.Physics);
                updatingObjects.Add(go);
                foreach (var p in Players)
                {
                    p.Key.RpcClient.SpawnMissile(go.NetID, p.Value != owner, missile.CRC, transform.Position,
                        transform.Orientation);
                }
            });
        }

        public void FireProjectiles(ProjectileFireCommand projectiles, Player owner)
        {
            actions.Enqueue(() =>
            {
                if (!Players.TryGetValue(owner, out var go))
                {
                    FLLog.Debug("Server", "Dead/unavailable player attempted fire");
                    return;
                }
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
                        if (!wo.NetOrderWeapons[i].Fire(target))
                        {
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
            if (!withAnimations.Contains(obj))
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
                foreach (var p in Players)
                {
                    p.Key.Despawn(player.ID, exploded);
                }

                Interlocked.Decrement(ref PlayerCount);
            });
        }

        public void RemoveSpawnedObject(GameObject obj, bool exploded)
        {
            actions.Enqueue(() =>
            {
                RemoveObjectInternal(obj);
                spawnedObjects.Remove(obj);
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

        List<GameObject> updatingObjects = new List<GameObject>();
        List<GameObject> spawnedObjects = new List<GameObject>();

        public GameObject SpawnSolar(string nickname, string archetype, string loadout, string rep, Vector3 position,
            Quaternion orientation, int idsName = 0, string? dockWith = null)
        {
            var arch = Server.GameData.Items.Archetypes.Get(archetype);
            var gameobj = new GameObject(arch, null, Server.Resources, false);
            gameobj.ArchetypeName = archetype;
            gameobj.NetID = IdGenerator.Allocate();
            if (idsName != 0)
                gameobj.Name = new ObjectName(idsName);
            gameobj.SetLocalTransform(new Transform3D(position, orientation));
            gameobj.Nickname = nickname;
            gameobj.World = GameWorld;
            var faction = Server.GameData.Items.Factions.Get(rep);
            gameobj.AddComponent(new SSolarComponent(gameobj) { Faction = faction });
            if (!string.IsNullOrWhiteSpace(dockWith))
            {
                var act = new DockAction() { Kind = DockKinds.Base, Target = dockWith };
                gameobj.AddComponent(new SDockableComponent(gameobj, act, arch.DockSpheres.ToArray()));
            }

            if (arch.Hitpoints > 0)
            {
                gameobj.AddComponent(new SHealthComponent(gameobj)
                    { CurrentHealth = arch.Hitpoints, MaxHealth = arch.Hitpoints });
                gameobj.AddComponent(new SDestroyableComponent(gameobj, this));
            }

            GameWorld.AddObject(gameobj);
            gameobj.Register(GameWorld.Physics);
            spawnedObjects.Add(gameobj);
            updatingObjects.Add(gameobj);

            foreach (var p in Players)
            {
                p.Key.RpcClient.SpawnObjects([BuildSpawnInfo(gameobj, p.Value)]);
            }

            return gameobj;
        }

        public void SpawnLoot(
            LootCrateEquipment crate,
            Equipment good,
            int count,
            Transform3D transform,
            string nickname = null)
        {
            actions.Enqueue(() =>
            {
                var model = crate.ModelFile.LoadFile(Server.Resources);
                var go = new GameObject(model, Server.Resources, false);
                go.Kind = GameObjectKind.Loot;
                go.PhysicsComponent.Mass = crate.Mass;
                go.NetID = IdGenerator.Allocate();
                go.ArchetypeName = crate.Nickname;
                go.Nickname = nickname ?? "";
                go.SetLocalTransform(transform);
                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                go.Register(GameWorld.Physics);
                go.PhysicsComponent.Body.SetDamping(0.5f, 0.2f);
                spawnedObjects.Add(go);
                go.AddComponent(new SHealthComponent(go) { MaxHealth = crate.Hitpoints, CurrentHealth = crate.Hitpoints });
                go.AddComponent(new SDestroyableComponent(go, this));
                var lt = new LootComponent(go);
                lt.Cargo.Add(new BasicCargo(good, count));
                go.AddComponent(lt);
                //Spawn debris
                foreach (var p in Players)
                {
                    p.Key.RpcClient.SpawnObjects([BuildSpawnInfo(go, p.Value)]);
                }
            });
        }

        public void SpawnDebris(
            GameObjectKind kind,
            string archetype,
            string part,
            Transform3D transform,
            GameObject[] children,
            uint[] destroyedParts,
            float mass,
            Vector3 initialForce
        )
        {
            actions.Enqueue(() =>
            {
                ModelResource src;
                List<SeparablePart> sep;
                if (kind == GameObjectKind.Ship)
                {
                    var ship = Server.GameData.Items.Ships.Get(archetype);
                    sep = ship.SeparableParts;
                    src = ship.ModelFile.LoadFile(Server.Resources);
                }
                else
                {
                    var solar = Server.GameData.Items.Archetypes.Get(archetype);
                    sep = solar.SeparableParts;
                    src = solar.ModelFile.LoadFile(Server.Resources);
                }

                var collider = src.Collision;
                var mdl = ((IRigidModelFile)src.Drawable).CreateRigidModel(false, Server.Resources);
                var newmodel = mdl.Parts[part].CloneAsRoot(mdl);
                var id = IdGenerator.Allocate();
                var go = new GameObject(newmodel, collider, Server.Resources, part, mass, false);
                go.Model.SeparableParts = sep;
                foreach (var p in destroyedParts)
                {
                    go.DisableCmpPart(p, Server.Resources, out _);
                }

                go.NetID = id;
                go.SetLocalTransform(transform);
                var sepInfo = sep.FirstOrDefault(x => x.Part.Equals(part, StringComparison.OrdinalIgnoreCase));
                var lifetime = debrisRandom.Next(
                    sepInfo?.DebrisType?.Lifetime ?? new ValueRange<float>(30, 30));
                FLLog.Debug("Server", $"Spawn debris of {archetype}:{part} with lifetime {lifetime}");
                go.AddComponent(new SDebrisComponent(
                    go,
                    kind == GameObjectKind.Solar,
                    FLHash.CreateID(archetype),
                    CrcTool.FLModelCrc(part),
                    lifetime));
                //re-parent children
                foreach (var c in children)
                {
                    if (go.Model.TryGetHardpoint(c.Attachment.Name, out var newHp))
                    {
                        c.Attachment = newHp;
                        c.Parent = go;
                        go.Children.Add(c);
                    }
                }

                GameWorld.AddObject(go);
                updatingObjects.Add(go);
                go.Register(GameWorld.Physics);
                go.PhysicsComponent.Body.Impulse(initialForce);
                go.PhysicsComponent.Body.SetDamping(0.5f, 0.2f);
                spawnedObjects.Add(go);
                //Spawn debris
                foreach (var p in Players)
                {
                    p.Key.RpcClient.SpawnObjects([BuildSpawnInfo(go, p.Value)]);
                }
            });
        }

        public void OnNPCSpawn(GameObject obj)
        {
            obj.NetID = IdGenerator.Allocate();
            obj.World = GameWorld;
            GameWorld.AddObject(obj);
            obj.Register(GameWorld.Physics);
            updatingObjects.Add(obj);
            spawnedObjects.Add(obj);
            foreach (var p in Players)
            {
                p.Key.RpcClient.SpawnObjects([BuildSpawnInfo(obj, p.Value)]);
            }
        }

        public void PartDisabled(GameObject obj, uint part)
        {
            foreach (Player p in Players.Keys)
                p.RpcClient.DestroyPart(obj, part);
        }

        public void LocalChatMessage(Player player, BinaryChatMessage message)
        {
            actions.Enqueue(() =>
            {
                var pObj = Players[player];
                player.RpcClient.ReceiveChatMessage(ChatCategory.Local, BinaryChatMessage.PlainText(player.Name),
                    message);
                foreach (var obj in GameWorld.SpatialLookup.GetNearbyObjects(pObj,
                             pObj.LocalTransform.Position, 15000))
                {
                    if (obj.TryGetComponent<SPlayerComponent>(out var other))
                    {
                        other.Player.RpcClient.ReceiveChatMessage(ChatCategory.Local,
                            BinaryChatMessage.PlainText(player.Name + ": "), message);
                    }
                }
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
            while (actions.Count > 0 && actions.TryDequeue(out act))
            {
                act();
            }

            while (delayedActions.Count > 0 && delayedActions.TryPeek(out var delayedAct)
                                            && delayedAct.Item2 <= Server.TotalTime)
            {
                if (delayedActions.TryDequeue(out delayedAct))
                {
                    delayedAct.Item1();
                }
            }

            //pause
            if (paused) return true;
            //Update
            NPCs.FrameStart();
            GameWorld.Update(delta);
            //projectiles
            if (GameWorld.Projectiles.HasQueued)
            {
                var queue = GameWorld.Projectiles.GetSpawnQueue();
                foreach (var p in Players)
                    p.Key.RpcClient.SpawnProjectiles(queue);
            }

            //Network update tick
            SendWorldUpdates(currentTick);
            UpdateDebugInfo();
            //Despawn after 2 seconds of nothing
            if (PlayerCount == 0)
            {
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

        record struct SortedUpdate(FetchedDelta Old, int Size, int Offset, GameObject Object, ObjectUpdate Update)
            : IComparable<SortedUpdate>
        {
            public int CompareTo(SortedUpdate other)
            {
                var x = ((ulong)other.Old.Priority) << 32 | (uint)other.Size;
                var y = ((ulong)Old.Priority) << 32 | (uint)Size;
                return x.CompareTo(y);
            }
        }

        class IdComparer : IComparer<SortedUpdate>
        {
            public static readonly IdComparer Instance = new IdComparer();

            private IdComparer()
            {
            }

            public int Compare(SortedUpdate x, SortedUpdate y) =>
                x.Update.ID.Value.CompareTo(y.Update.ID.Value);
        }

        //This could do with some work
        void SendWorldUpdates(uint tick)
        {
            // Update players
            foreach (var player in Players)
            {
                var tr = player.Value.WorldTransform;
                player.Key.Position = tr.Position;
                player.Key.Orientation = tr.Orientation;
            }

            // Fetch data
            var toUpdate = GetUpdatingObjects().ToArray();
            var allUpdates = new ObjectUpdate[toUpdate.Length];

            for (int i = 0; i < toUpdate.Length; i++)
            {
                //Get main object update fields
                var obj = toUpdate[i];
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
                if (obj.TryGetComponent<ShipPhysicsComponent>(out var objPhysics))
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

                allUpdates[i] = update;
            }

            // Send data to players
            var pk = packer.Begin(allUpdates, toUpdate);
            foreach (var player in Players)
            {
                var phealthcomponent = player.Value.GetComponent<SHealthComponent>();
                var phealth = phealthcomponent.CurrentHealth;
                var pshieldComponent = player.Value.GetFirstChildComponent<SShieldComponent>();
                float pshield = 0;
                if (pshieldComponent != null)
                    pshield = pshieldComponent.Health;
                var selfPlayer = player.Value.GetComponent<SPlayerComponent>();
                var phys = player.Value.GetComponent<ShipPhysicsComponent>();
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
                    var lst = new ObjectUpdate[allUpdates.Length - 1];
                    int j = 0;
                    for (int i = 0; i < allUpdates.Length; i++)
                    {
                        if (toUpdate[i] == player.Value)
                            continue;
                        lst[j++] = allUpdates[i];
                    }

                    player.Key.SendSPUpdate(new SPUpdatePacket()
                    {
                        Tick = tick,
                        InputSequence = selfPlayer.LatestReceived,
                        PlayerState = state,
                        Updates = lst
                    });
                }
                else
                {
#if DEBUG
                    int maxPacketSize = 500; //Min safe UDP packet size - 8
#else
                    int maxPacketSize = player.Key.Client.MaxSequencedSize;
#endif
                    player.Key.SendMPUpdate(pk.Pack(tick, state, selfPlayer, player.Value, maxPacketSize));
                }
            }
        }

        public void Finish()
        {
            GameWorld.Dispose();
        }
    }
}
