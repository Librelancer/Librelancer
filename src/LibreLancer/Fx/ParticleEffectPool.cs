// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Fx;

namespace LibreLancer.Fx
{
    public class ParticleEffectPool
    {
        public const int MAX_PARTICLES = 100000;
        public Particle[] Particles = new Particle[MAX_PARTICLES];

        public Queue<int> FreeParticles = new Queue<int>();

        public ParticleEffectPool()
        {
            for(int i = 0; i < MAX_PARTICLES; i++)
            {
                FreeParticles.Enqueue(i);
            }
        }

        public void Update(TimeSpan delta)
        {
            for (int i = 0; i < Particles.Length; i++)
            {
                if (!Particles[i].Active)
                    continue;
                Particles[i].Position += Particles[i].Normal * (float)delta.TotalSeconds;
                Particles[i].TimeAlive += (float)delta.TotalSeconds;
                if (Particles[i].TimeAlive >= Particles[i].LifeSpan)
                {
                    Particles[i].Active = false;
                    Particles[i].Instance.EmitStates[Particles[i].Emitter].ParticleCount--;
                    Particles[i].Instance = null;
                    FreeParticles.Enqueue(i);
                    continue;
                }
            }
        }

        public void KillAll(ParticleEffectInstance instance)
        {
            for(int i = 0; i < Particles.Length; i++)
            {
                if (!Particles[i].Active)
                    continue;
                if(Particles[i].Instance == instance)
                {
                    Particles[i].Active = false;
                    Particles[i].Instance = null;
                    FreeParticles.Enqueue(i);
                }
            }
        }

        public int GetFreeParticle()
        {
            if (FreeParticles.Count > 0)
                return FreeParticles.Dequeue();
            else
                return -1;
        }

        public void Draw(PolylineRender polyline, Billboards billboards, PhysicsDebugRenderer debug)
        {
            for (int i = 0; i < Particles.Length; i++)
            {
                if (!Particles[i].Active)
                    continue;
                var inst = Particles[i].Instance;
                if(inst.NodeEnabled(Particles[i].Appearance))
                {
                    var app = (FxAppearance)Particles[i].Appearance.Node;
                    app.Debug = debug;
                    app.Draw(ref Particles[i], (float)inst.LastTime, (float)inst.GlobalTime, Particles[i].Appearance, inst.Resources, billboards, ref inst.DrawTransform, inst.DrawSParam);
                }
            }
        }
    }
}
