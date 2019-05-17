// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
namespace LibreLancer.Fx
{
    public class ParticleEffectInstance
    {
        public ParticleEffectPool Pool;
        public Random Random = new Random();
        public class EmitterState
        {
            public int ParticleCount = 0;
            public double SpawnTimer = 0f;
        }

        public const int PARTICLES_PER_EMITTER = 1024;
        public ParticleEffect Effect;
        public ResourceManager Resources;
        public Dictionary<NodeReference, EmitterState> EmitStates = new Dictionary<NodeReference, EmitterState>();
        public Dictionary<NodeReference, LineBuffer> BeamAppearances = new Dictionary<NodeReference, LineBuffer>();
        public Dictionary<NodeReference, bool> EnableStates = new Dictionary<NodeReference, bool>();
        double globaltime = 0;
        public double GlobalTime => globaltime;

        public ParticleEffectInstance(ParticleEffect fx)
        {
            Effect = fx;
            foreach (var node in fx.References)
            {
                if (node.Node is FLBeamAppearance)
                {
                    BeamAppearances.Add(node, new LineBuffer(512));
                }
            }
        }

        public void Reset()
        {
            globaltime = 0;
            Pool.KillAll(this);
            foreach (var state in EmitStates)
            {
                state.Value.SpawnTimer = 0;
                state.Value.ParticleCount = 0;
            }
        }

        public double LastTime => lasttime;

        double lasttime = 0;
        public Vector3 Position = Vector3.Zero;
        public void Update(TimeSpan delta, Matrix4 transform, float sparam)
        {
            if (Pool == null) return;
            Position = transform.Transform(Vector3.Zero);
            lasttime = globaltime;
            globaltime += delta.TotalSeconds;
            //Line buffers
            foreach (var buf in BeamAppearances.Values)
            {
                for (int i = 0; i < buf.Count(); i++)
                {
                    if (buf[i].ParticleIndex >= 0 && (!Pool.Particles[buf[i].ParticleIndex].Active || Pool.Particles[buf[i].ParticleIndex].Instance != this))
                        buf[i] = new LinePointer() { Active = false, ParticleIndex = -1 };
                }
            }
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

        public EmitterState GetEmitterState(NodeReference emitter)
        {
            EmitterState es;
            if (!EmitStates.TryGetValue(emitter, out es))
            {
                es = new EmitterState();
                EmitStates.Add(emitter, es);
            }
            return es;
        }

        public int GetNextFreeParticle() => Pool.GetFreeParticle();

        public bool NodeEnabled(NodeReference node)
        {
            bool val;
            if (!EnableStates.TryGetValue(node, out val)) return true;
            return val;
        }

        public Matrix4 DrawTransform;
        public float DrawSParam;
        public void Draw(PolylineRender polyline, Billboards billboards, PhysicsDebugRenderer debug, Matrix4 transform, float sparam)
        {
            DrawTransform = transform;
            DrawSParam = sparam;
            if (Pool == null) return;
            DrawTransform = transform;
            DrawSParam = sparam;
            foreach (var kv in BeamAppearances)
            {
                if (NodeEnabled(kv.Key))
                {
                    var app = (FLBeamAppearance)kv.Key.Node;
                    app.DrawBeamApp(polyline, kv.Value, (float)globaltime, kv.Key, this, Effect.ResourceManager, billboards, ref transform, sparam);
                }
            }
        }
    }
}

