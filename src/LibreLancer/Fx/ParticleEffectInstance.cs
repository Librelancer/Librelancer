// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading;
using LibreLancer.Data.Schema.Effects;
using LibreLancer.Resources;

namespace LibreLancer.Fx
{
    public static class FxRandom
    {
        private static int seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> random = new(() => new Random(Interlocked.Increment(ref seed)));
        public static float NextFloat(float min, float max) => random.Value!.NextFloat(min, max);
    }

    public struct EmitterState
    {
        public double SpawnTimer;
        public int Count;
        public bool Initialized;
    }

    public class ParticleEffectInstance
    {
        private static int _id;
        private readonly int id;

        public override string ToString()
        {
            return $"Instance {id:X}";
        }

        public int DrawIndex; // needed to fix multiple fx spawned by fuse

        public ParticleEffectPool Pool = null!;
        public ResourceManager Resources = null!;
        public ParticleEffect Effect;

        public EmitterState[] Emitters;
        private double globaltime = 0;
        public double GlobalTime => globaltime;
        public ParticleBuffer Buffer;
        private readonly Dictionary<(int Appearance, int Particle), ParticleEffectInstance> childEffects = [];
        private int nextParticleId;

        public int CountAll()
        {
            int total = 0;
            for (var i = 0; i < Effect.Appearances.Count; i++)
            {
                total += Buffer.GetCount(i);
            }
            return total;
        }

        public bool IsFinished()
        {
            for (var i = 0; i < Emitters.Length; i++)
            {
                if (Emitters[i].Count > 0)
                {
                    return false;
                }
            }

            return Effect.Emitters.All(t => !t.Enabled || !(globaltime < t.Emitter.NodeLifeSpan));
        }

        public ParticleEffectInstance(ParticleEffect fx)
        {
            id = Interlocked.Increment(ref _id);
            Emitters = new EmitterState[fx.Emitters.Count];
            Buffer = new ParticleBuffer(fx.ParticleCounts);
            Effect = fx;
        }

        public void Reset()
        {
            globaltime = 0;
            Buffer.Reset();
            Array.Clear(Emitters);
            childEffects.Clear();
        }

        public ParticleEffectInstance GetOrCreateChildEffect(
            int appearanceIndex,
            int particleId,
            ParticleEffect effect)
        {
            var key = (appearanceIndex, particleId);
            if (!childEffects.TryGetValue(key, out var child) || child.Effect != effect)
            {
                child = new ParticleEffectInstance(effect);
                childEffects[key] = child;
            }

            child.Pool = Pool;
            child.Resources = Resources;
            child.DrawIndex = DrawIndex;
            return child;
        }

        public bool TryGetChildEffect(int appearanceIndex, int particleId,
            [MaybeNullWhen(false)] out ParticleEffectInstance effect) =>
            childEffects.TryGetValue((appearanceIndex, particleId), out effect);

        public void RemoveUnusedChildEffects(int appearanceIndex, HashSet<int> activeParticles)
        {
            foreach (var key in childEffects.Keys.Where(x =>
                         x.Appearance == appearanceIndex && !activeParticles.Contains(x.Particle)).ToArray())
                childEffects.Remove(key);
        }

        public int AllocateParticleId() => ++nextParticleId;

        public double LastTime => lasttime;

        private double lasttime = 0;
        public Vector3 Position = Vector3.Zero;

        public bool Culled = false;

        public void UpdateCull(ICamera camera)
        {
            if (!float.IsFinite(Effect.Radius))
            {
                return;
            }

            var sph = new BoundingSphere(Position, Effect.Radius);
            Culled = !camera.FrustumCheck(sph);
        }

        public void Update(double delta, Matrix4x4 transform, float sparam)
        {
            if ((ParticleEffectPool?)Pool == null)
            {
                return;
            }

            Position = Vector3.Transform(Vector3.Zero, transform);
            lasttime = globaltime;
            globaltime += delta;

            // Update particles
            for (var i = 0; i < Effect.Appearances.Count; i++)
            {
                var app = Effect.Appearances[i];
                for (int j = 0; j < app.Linked.Count; j++)
                {
                   app.Linked[j].Field.Update(this, app.Linked[j], i, transform, sparam, (float)delta);
                }
                var count = Buffer.GetCount(i);
                for (var j = 0; j < count; j++)
                {
                    ref var particle = ref Buffer[i, j];
                    particle.TimeAlive += (float) delta;
                    if (particle.TimeAlive >= particle.LifeSpan)
                    {
                        Emitters[particle.EmitterIndex].Count--;
                        Debug.Assert(Emitters[particle.EmitterIndex].Count >= 0);
                        Buffer.RemoveAt(i, j); // Usually a dequeue, can change with sparam
                        j--;
                        count--;
                    }
                    else
                    {
                        particle.Position += (float) delta * particle.Velocity;
                    }
                }
            }

            // Update emitters
            for (var i = 0; i < Effect.Emitters.Count; i++)
            {
                var r = Effect.Emitters[i];
                if(r.Enabled)
                {
                    r.Emitter.Update(r, i, this, delta, ref transform, sparam);
                }
            }

            for (var i = 0; i < Effect.Appearances.Count; i++)
            {
                var appearance = Effect.Appearances[i];
                if (appearance.Enabled)
                    appearance.Appearance.Update(this, appearance, i, transform, sparam, delta);
            }
        }

        public void Draw(Matrix4x4 transform, float sparam)
        {
            if ((ParticleEffectPool?)Pool == null)
            {
                return;
            }

            for (var i = 0; i < Effect.Appearances.Count; i++)
            {
                if (!Effect.Appearances[i].Enabled)
                {
                    continue;
                }

                Effect.Appearances[i].Appearance.Debug = Pool.Debug;
                Effect.Appearances[i].Appearance.DrawDebug = Pool.DrawDebug;
                Effect.Appearances[i].Appearance.Draw(this, Effect.Appearances[i], i, transform, sparam);
            }
        }
    }
}
