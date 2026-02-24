// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using LibreLancer.Resources;

namespace LibreLancer.Fx
{
    public static class FxRandom
    {
        static int seed = Environment.TickCount;
        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        public static float NextFloat(float min, float max) => random.Value.NextFloat(min, max);
    }

    public struct EmitterState
    {
        public double SpawnTimer;
        public int Count;
    }

    public class ParticleEffectInstance
    {
        private static int _id;
        private int ID;

        public override string ToString()
        {
            return $"Instance {ID:X}";
        }

        public int DrawIndex; //needed to fix multiple fx spawned by fuse

        public ParticleEffectPool Pool;
        public ParticleEffect Effect;
        public ResourceManager Resources;

        public EmitterState[] Emitters;
        double globaltime = 0;
        public double GlobalTime => globaltime;

        public ParticleBuffer Buffer;

        public bool IsFinished()
        {
            for (int i = 0; i < Emitters.Length; i++)
            {
                if (Emitters[i].Count > 0) return false;
            }
            for (int i = 0; i < Effect.Emitters.Count; i++)
            {
                if (Effect.Emitters[i].Enabled &&
                    globaltime < Effect.Emitters[i].Emitter.NodeLifeSpan)
                    return false;
            }
            return true;
        }

        public ParticleEffectInstance(ParticleEffect fx)
        {
            ID = Interlocked.Increment(ref _id);
            Emitters = new EmitterState[fx.Emitters.Count];
            Buffer = new ParticleBuffer(fx.ParticleCounts);
            Effect = fx;
        }

        public void Reset()
        {
            globaltime = 0;
            Buffer.Reset();
            Array.Clear(Emitters);
        }

        public double LastTime => lasttime;

        double lasttime = 0;
        public Vector3 Position = Vector3.Zero;

        public bool Culled = false;

        public void UpdateCull(ICamera camera)
        {
            if (!float.IsFinite(Effect.Radius)) return;
            var sph = new BoundingSphere(Position, Effect.Radius);
            Culled = !camera.FrustumCheck(sph);
        }

        public void Update(double delta, Matrix4x4 transform, float sparam)
        {
            if (Pool == null) return;
            Position = Vector3.Transform(Vector3.Zero, transform);
            lasttime = globaltime;
            globaltime += delta;
            //Update particles
            for (int i = 0; i < Effect.Appearances.Count; i++)
            {
                int count = Buffer.GetCount(i);
                for (int j = 0; j < count; j++)
                {
                    ref var particle = ref Buffer[i, j];
                    particle.TimeAlive += (float) delta;
                    if (particle.TimeAlive >= particle.LifeSpan)
                    {
                        Emitters[particle.EmitterIndex].Count--;
                        Debug.Assert(Emitters[particle.EmitterIndex].Count >= 0);
                        Buffer.RemoveAt(i, j); //Usually a dequeue, can change with sparam
                        j--;
                        count--;
                    }
                    else
                    {
                        particle.Position += (float) delta * particle.Normal;
                    }
                }
            }
            //Update emitters
            for (int i = 0; i < Effect.Emitters.Count; i++)
            {
                var r = Effect.Emitters[i];
                if(r.Enabled)
                    r.Emitter.Update(r, i, this, delta, ref transform, sparam);
            }
        }

        public void Draw(Matrix4x4 transform, float sparam)
        {
            if (Pool == null) return;
            for (int i = 0; i < Effect.Appearances.Count; i++)
            {
                if (!Effect.Appearances[i].Enabled) continue;
                Effect.Appearances[i].Appearance.Draw(
                    this,
                    Effect.Appearances[i],
                    i,
                    transform,
                    sparam);
            }
        }
    }
}

