// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Fx;
using LibreLancer.Media;
using LibreLancer.Net;
using LibreLancer.Net.Protocol;
using LibreLancer.Resources;
using LibreLancer.Sounds;
using LibreLancer.World.Components;

namespace LibreLancer.World
{
    public class ProjectileManager
    {
        public Projectile[] Projectiles = new Projectile[16384];
        public IdPool Ids = new IdPool(16384 / 32, false);

        GameWorld world;
        public ProjectileManager(GameWorld world)
        {
            this.world = world;
        }

        public void Update(double time)
        {
            var tFloat = (float)time;
            foreach(var i in Ids.GetAllocated()) {
                var length = Projectiles[i].Normal.Length() * tFloat;
                var dir = Projectiles[i].Normal.Normalized();
                if (world.Physics.PointRaycast(
                    Projectiles[i].Owner?.PhysicsComponent?.Body,
                    Projectiles[i].Position,
                    dir,
                    length,
                    out var contactPoint,
                    out var po))
                {
                    Projectiles[i].Alive = false;
                    Projectiles[i].Effect = null;
                    world.Renderer?.SpawnTempFx(Projectiles[i].Data.HitEffect, contactPoint);
                    if (po?.Tag is GameObject go)
                    {
                        world.Server?.ProjectileHit(go, Projectiles[i].Owner, Projectiles[i].Data.Munition);
                    }
                    Ids.Free(i);
                }
                Projectiles[i].Position += (Projectiles[i].Normal * tFloat);
                world.DrawDebug(Projectiles[i].Position);
                Projectiles[i].Time += tFloat;
                if(Projectiles[i].Time >= Projectiles[i].Data.Lifetime) {
                    Projectiles[i].Alive = false;
                    Projectiles[i].Effect = null;
                    Ids.Free(i);
                }
            }

            //collect sound instances
            List<ulong> toRemove = new List<ulong>();
            foreach (var kv in _instances) {
                if (!kv.Value.Playing)
                {
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove)
                _instances.Remove(k);
        }

        Dictionary<string, ProjectileData> datas = new Dictionary<string, ProjectileData>();
        public ProjectileData GetData(GunEquipment gunDef)
        {
            ProjectileData pdata;
            if (datas.TryGetValue(gunDef.Nickname, out pdata)) return pdata;
            pdata = new ProjectileData();
            pdata.Munition = gunDef.Munition;
            pdata.Lifetime = gunDef.Munition.Def.Lifetime;
            pdata.Velocity = gunDef.Def.MuzzleVelocity;
            if (world.Renderer != null)
            {
                var res = world.Renderer.Game.GetService<GameDataManager>();
                if(gunDef.Munition.Def.MunitionHitEffect != null)
                pdata.HitEffect = res.Items.Effects.Get(gunDef.Munition.Def.MunitionHitEffect)
                    .GetEffect(world.Renderer.ResourceManager);
               if(gunDef.Munition.Def.ConstEffect != null)
                pdata.TravelEffect = res.Items.Effects.Get(gunDef.Munition.Def.ConstEffect)?
                    .GetEffect(world.Renderer.ResourceManager);
            }
            datas.Add(gunDef.Nickname, pdata);
            return pdata;
        }

        private List<(ObjNetId NetId, int Index, Vector3 Target)> spawns =
            new List<(ObjNetId NetId, int Index, Vector3 Target)>();
        private List<(int Index, Vector3 Target)> requests = new List<(int, Vector3)>();

        private List<MissileFireCmd> missiles = new List<MissileFireCmd>();
        public bool HasQueued => spawns.Count > 0;
        public bool HasMissilesQueued => missiles.Count > 0;

        public MissileFireCmd[] GetMissileQueue()
        {
            var x = missiles.ToArray();
            missiles.Clear();
            return x;
        }

        public void QueueMissile(string hardpoint, GameObject target)
        {
            if (target == null)
            {
                missiles.Add(new MissileFireCmd() { Hardpoint = hardpoint });
            }
            else
            {
                missiles.Add(new MissileFireCmd()
                {
                    Hardpoint = hardpoint,
                    Target = target
                });
            }
        }

        public ProjectileSpawn[] GetSpawnQueue()
        {
            var result = spawns.GroupBy(x => x.NetId)
                .Select(x =>
                {
                    var spawn = new ProjectileSpawn();
                    bool first = true;
                    List<Vector3> targets = null;
                    foreach (var v in x) {
                        if (first) {
                            spawn.Owner = v.NetId;
                            spawn.Target = v.Target;
                            first = false;
                        } else {
                            if (spawn.Target != v.Target) {
                                targets ??= new List<Vector3>();
                                targets.Add(spawn.Target);
                                spawn.Unique |= (1UL << v.Index);
                            }
                        }
                        spawn.Guns |= (1UL << v.Index);
                    }
                    spawn.OtherTargets = targets == null
                        ? Array.Empty<Vector3>() :
                        targets.ToArray();
                    return spawn;
                }).ToArray();
            spawns.Clear();
            return result;
        }



        public ProjectileFireCommand? GetQueuedRequest()
        {
            if(requests.Count <= 0)
                return null;
            requests.Sort((x,y) => x.Index.CompareTo(y.Index));
            var fireRequest = new ProjectileFireCommand()
            {
                Target = requests[0].Target
            };
            List<Vector3> otherTargets = new List<Vector3>();
            for (int i = 0; i < requests.Count; i++)
            {
                fireRequest.Guns |= (1UL << requests[i].Index);
                if (requests[i].Target != fireRequest.Target) {
                    fireRequest.Unique |= (1UL << requests[i].Index);
                    otherTargets.Add(requests[i].Target);
                }
            }
            if (otherTargets.Count > 0)
                fireRequest.OtherTargets = otherTargets.ToArray();
            requests.Clear();
            return fireRequest;
        }

        public GameObject Player;

        public void QueueFire(GameObject owner, WeaponComponent component, Vector3 target)
        {
            if (!owner.TryGetComponent<WeaponControlComponent>(out var wc))
                return;
            int wpIdx = Array.IndexOf(wc.NetOrderWeapons, component);
            if (wpIdx == -1)
                return;
            if (owner == Player)
                requests.Add((wpIdx, target));
            else
                spawns.Add((owner, wpIdx, target));
        }

        private Dictionary<ulong, SoundInstance> _instances = new Dictionary<ulong, SoundInstance>();

        public void PlayProjectileSound(GameObject owner, string soundName, Vector3 position, string hardpoint)
        {
            SoundManager snd;
            if (!string.IsNullOrWhiteSpace(soundName) &&
                world.Renderer != null && (snd = world.Renderer.Game.GetService<SoundManager>()) != null)
            {
                ulong soundID = ((ulong) owner.Unique << 32) | (ulong)CrcTool.HardpointCrc(hardpoint);
                if (!_instances.TryGetValue(soundID, out var inst))
                {
                    inst = snd.GetInstance(soundName, 0, -1, -1, position);
                    _instances[soundID] = inst;
                }
                if (inst != null)
                {
                    if (owner.NetID > 0)
                        inst.Priority = -1;
                    else
                        inst.Priority = -2;
                }
                inst?.Set3D();
                inst?.SetPosition(position);
                inst?.Stop();
                inst?.Play();
            }
        }

        public void SpawnProjectile(GameObject owner, string hardpoint, ProjectileData projectile, Vector3 position, Vector3 heading)
        {
            if (!Ids.TryAllocate(out int ptr))
                throw new Exception("Projectile overflow");
            Projectiles[ptr] = new Projectile() {
                Data = projectile,
                Owner = owner,
                Time = 0,
                Alive = true,
                Position = position,
                Start = position,
                Normal = heading * projectile.Velocity
            };
            PlayProjectileSound(owner, projectile.Munition.Def.OneShotSound, position, hardpoint);
            if (world.Renderer != null && projectile.TravelEffect != null) {
                Projectiles[ptr].Effect = new ParticleEffectInstance(projectile.TravelEffect);
            }
        }
    }
    public class ProjectileData
    {
        public MunitionEquip Munition;
        public float Velocity;
        public float Lifetime;
        public ParticleEffect HitEffect;
        public ParticleEffect TravelEffect;
    }

    public struct Projectile
    {
        public ProjectileData Data;
        public ParticleEffectInstance Effect;
        public GameObject Owner;
        public bool Alive;
        public float Time;
        public Vector3 Position;
        public Vector3 Start;
        public Vector3 Normal;
    }
}
