// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
using LibreLancer.Fx;
using LibreLancer.Media;
using LibreLancer.Net;

namespace LibreLancer
{
    public class ProjectileManager
    {
        public Projectile[] Projectiles = new Projectile[16384];

        int projectilePtr = 0;

        GameWorld world;
        public ProjectileManager(GameWorld world)
        {
            this.world = world;
        }

        public void FixedUpdate(double time)
        {
            var tFloat = (float)time;
            for(int i = 0; i < Projectiles.Length; i++) {
                if (!Projectiles[i].Alive) continue;
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
                    if (po.Tag is GameObject go) 
                    {
                        world.Server?.ProjectileHit(go, Projectiles[i].Owner, Projectiles[i].Data.Munition);
                    }
                }
                Projectiles[i].Position += (Projectiles[i].Normal * tFloat);
                world.DrawDebug(Projectiles[i].Position);
                Projectiles[i].Time += tFloat;
                if(Projectiles[i].Time >= Projectiles[i].Data.Lifetime) {
                    Projectiles[i].Alive = false;
                    Projectiles[i].Effect = null;
                }
            }

            //collect sound instances
            List<ulong> toRemove = new List<ulong>();
            foreach (var kv in _instances) {
                if (!kv.Value.Playing)
                {
                    kv.Value.Dispose();
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var k in toRemove)
                _instances.Remove(k);
        }

        Dictionary<string, ProjectileData> datas = new Dictionary<string, ProjectileData>();
        public ProjectileData GetData(GameData.Items.GunEquipment gunDef)
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
                pdata.HitEffect = res.GetEffect(gunDef.Munition.Def.MunitionHitEffect)
                    .GetEffect(world.Renderer.ResourceManager);
               if(gunDef.Munition.Def.ConstEffect != null)
                pdata.TravelEffect = res.GetEffect(gunDef.Munition.Def.ConstEffect)?
                    .GetEffect(world.Renderer.ResourceManager);
            }
            datas.Add(gunDef.Nickname, pdata);
            return pdata;
        }

        private List<ProjectileSpawn> queued = new List<ProjectileSpawn>();
        public bool HasQueued => queued.Count > 0;

        public ProjectileSpawn[] GetQueue()
        {
            var x = queued.ToArray();
            queued.Clear();
            return x;
        }
        public void QueueProjectile(int owner, GameData.Items.GunEquipment gunDef, uint hardpoint, Vector3 position, Vector3 heading)
        {
            queued.Add(new ProjectileSpawn()
            {
                Owner = owner, Gun = gunDef.CRC, Hardpoint = hardpoint, Heading = heading, Start = position
            });
        }

        private Dictionary<ulong, SoundInstance> _instances = new Dictionary<ulong, SoundInstance>();
        public void SpawnProjectile(GameObject owner, uint hardpoint, ProjectileData projectile, Vector3 position, Vector3 heading)
        {
            if (projectilePtr == 16383) projectilePtr = 0;
            Projectiles[projectilePtr] = new Projectile() {
                Data = projectile,
                Owner = owner,
                Time = 0,
                Alive = true,
                Position = position,
                Start = position,
                Normal = heading * projectile.Velocity
            };
            SoundManager snd;
            if (world.Renderer != null && (snd = world.Renderer.Game.GetService<SoundManager>()) != null)
            {
                ulong soundID = ((ulong) owner.Unique << 32) | (ulong)hardpoint;
                if (!_instances.TryGetValue(soundID, out var inst))
                {
                    inst = snd.GetInstance(projectile.Munition.Def.OneShotSound, 0, -1, -1, position);
                    _instances[soundID] = inst;
                }

                if (inst != null) inst.Priority = -2;
                inst?.Set3D();
                inst?.SetPosition(position);
                inst?.Stop();
                inst?.Play();
            }
            if (world.Renderer != null && projectile.TravelEffect != null) {
                Projectiles[projectilePtr].Effect = new ParticleEffectInstance(projectile.TravelEffect);
            }
            projectilePtr++;
        }
    }
    public class ProjectileData
    {
        public GameData.Items.MunitionEquip Munition;
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
