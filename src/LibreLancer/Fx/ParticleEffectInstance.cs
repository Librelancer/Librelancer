// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Threading;

namespace LibreLancer.Fx
{
    public static class FxRandom
    {
        static int seed = Environment.TickCount;
        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));
        public static float NextFloat(float min, float max) => random.Value.NextFloat(min, max);
    }

    public class BeamParticles
    {
        public NodeReference Node;
        public FLBeamAppearance BeamApp => (FLBeamAppearance)Node.Node;
        public const int MAX_PARTICLES = 768;
        public int[] ParticleIndices = new int[MAX_PARTICLES];
        public int ParticleCount = 0;
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

        public double[] SpawnTimers;
        public int[] ParticleCounts;
        public int[] ParticleIndex;
        public int FrameNumber;
        public BeamParticles[] Beams;
        bool[] enableStates;
        double globaltime = 0;
        public double GlobalTime => globaltime;

        public bool IsFinished() 
        {
            for (int i = 0; i < ParticleCounts.Length; i++)
            {
                if (ParticleCounts[i] > 0) return false;
            }
            for (int i = 0; i < Effect.References.Count; i++)
            {
                var r = Effect.References[i];
                if (NodeEnabled(r) && (r.Node is FxEmitter emit))
                {
                    if (globaltime < emit.NodeLifeSpan)
                        return false;
                }
            }
            return true;
        }

        public ParticleEffectInstance(ParticleEffect fx)
        {
            ID = Interlocked.Increment(ref _id);
            SpawnTimers = new double[fx.EmitterCount];
            ParticleCounts = new int[fx.EmitterCount];
            enableStates = new bool[fx.References.Count];
            ParticleIndex = new int[fx.References.Count];
            if(fx.BeamCount > 0)
            {
                Beams = new BeamParticles[fx.BeamCount];
                for (int i = 0; i < Beams.Length; i++) Beams[i] = new BeamParticles();
            }
            for (int i = 0; i < fx.References.Count; i++) enableStates[i] = true;
            Effect = fx;
            foreach (var node in fx.References)
            {
                if (node.Node is FLBeamAppearance) Beams[node.BeamIndex].Node = node;
            }
        }

        public void Reset()
        {
            globaltime = 0;
            Pool.KillAll(this);
            for (int i = 0; i < SpawnTimers.Length; i++) SpawnTimers[i] = 0;
            for (int i = 0; i < ParticleCounts.Length; i++) ParticleCounts[i] = 0;
        }

        public double LastTime => lasttime;

        double lasttime = 0;
        public Vector3 Position = Vector3.Zero;
        public void Update(double delta, Matrix4x4 transform, float sparam)
        {
            if (Pool == null) return;
            Position = Vector3.Transform(Vector3.Zero, transform);
            lasttime = globaltime;
            globaltime += delta;
            //Update Emitters
            for (int i = 0; i < Effect.References.Count; i++)
            {
                var r = Effect.References[i];
                if (NodeEnabled(r) && (r.Node is FxEmitter))
                {
                    ((FxEmitter)r.Node).Update(r, this, delta, ref transform, sparam);
                }
            }
        }
        
        public int GetNextFreeParticle() => Pool.GetFreeParticle();

        public bool NodeEnabled(NodeReference node) => enableStates[node.Index];
        public void SetNodeEnabled(NodeReference node, bool enabled) => enableStates[node.Index] = enabled;

        public void Draw(Matrix4x4 transform, float sparam)
        {
            DrawTransform = transform;
            DrawSParam = sparam; 
        }
        
        public Matrix4x4 DrawTransform;
        public float DrawSParam;
        public void DrawBeams(PolylineRender polyline, PhysicsDebugRenderer debug, Matrix4x4 transform, float sparam)
        {
            if (Beams != null)
            {
                foreach (var kv in Beams)
                {
                    if (NodeEnabled(kv.Node))
                    {
                        kv.BeamApp.DrawBeamApp(polyline, (float)globaltime, kv.Node, this, ref transform, sparam);
                    }
                }
            }
        }
    }
}

