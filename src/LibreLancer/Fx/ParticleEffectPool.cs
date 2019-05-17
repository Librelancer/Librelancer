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
        public const int MAX_PARTICLES = 20000;
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

        Billboards currentBillboards;
        int countApp = 0;
        (ParticleEffectInstance i, FxAppearance a)[] appearances = new (ParticleEffectInstance i, FxAppearance a)[1024];
        int countParticle = 0;
        ParticleDraw[] draws = new ParticleDraw[MAX_PARTICLES];
        int[] starts = new int[MAX_PARTICLES];
        
        public void Draw(PolylineRender polyline, Billboards billboards, PhysicsDebugRenderer debug)
        {
            countApp = countParticle = 0;
            currentBillboards = billboards;

            for (int i = 0; i < Particles.Length; i++)
            {
                if (!Particles[i].Active)
                    continue;
                var inst = Particles[i].Instance;
                if(inst.NodeEnabled(Particles[i].Appearance))
                {
                    var app = (FxAppearance)Particles[i].Appearance.Node;
                    app.Debug = debug;
                    app.Draw(ref Particles[i], (float)inst.LastTime, (float)inst.GlobalTime, Particles[i].Appearance, inst.Resources, this, ref inst.DrawTransform, inst.DrawSParam);
                }
            }
            //Batching :D
            Array.Sort(draws, 0, countParticle);
            int currIdx = -1;
            int startsIdx = 0;
            Texture2D currTex = null;
            for(int i = 0; i < countParticle; i++)
            {
                if((currIdx != -1 && draws[i].AppearanceIdx != currIdx) ||
                    (currTex != null && draws[i].Texture != currTex))
                {
                    if (startsIdx == 0) continue;
                    if(currTex == null)
                    {
                        startsIdx = 0; continue;
                    }
                    var app = appearances[currIdx].a;
                    var inst = appearances[currIdx].i;
                    var p = (FxBasicAppearance)app;
                    if (app is FxPerpAppearance) {
                        billboards.CommandRect(currTex, p.BlendInfo, starts, startsIdx, inst.Position);
                    }
                    else {
                        billboards.CommandBasic(currTex, p.BlendInfo, starts, startsIdx, inst.Position);
                    }
                    startsIdx = 0;
                }
                currIdx = draws[i].AppearanceIdx;
                currTex = draws[i].Texture;
                starts[startsIdx++] = draws[i].StartVertex;
            }
        }


        int GetAppFxIdx(ParticleEffectInstance instance, FxAppearance a)
        {
            var item = (instance, a);
            for(int i = 0; i < countApp; i++) {
                if (appearances[i].Equals(item)) return i;
            }
            appearances[countApp] = item;
            return countApp++;
        }

        public void DrawPerspective(
            ParticleEffectInstance instance,
            FxBasicAppearance appearance,
            Texture2D texture,
            Vector3 pos,
            Matrix4 world,
            Vector2 size,
            Color4 color,
            Vector2 topleft,
            Vector2 topright,
            Vector2 bottomleft,
            Vector2 bottomright,
            Vector3 normal,
            float angle)
        {
            if (countParticle >= MAX_PARTICLES) return;
            var idx = GetAppFxIdx(instance, appearance);
            var start = currentBillboards.AddPerspective(pos, world, size, color, topleft, topright, bottomleft, bottomright, normal, angle);
            draws[countParticle++] = new ParticleDraw()
            {
                Texture = texture,
                AppearanceIdx = idx,
                StartVertex = start
            };
        }

        public void DrawBasic(
            ParticleEffectInstance instance,
            FxBasicAppearance appearance,
            Texture2D texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            Vector2 topleft,
            Vector2 topright,
            Vector2 bottomleft,
            Vector2 bottomright,
            float angle
        )
        {
            if (countParticle >= MAX_PARTICLES) return;
            var idx = GetAppFxIdx(instance, appearance);
            int start = currentBillboards.AddBasic(Position, size, color, topleft, topright, bottomleft, bottomright, angle);
            draws[countParticle++] = new ParticleDraw()
            {
                Texture = texture,
                AppearanceIdx = idx,
                StartVertex = start
            };
        }

        public void DrawRect(
            ParticleEffectInstance instance, 
            FxBasicAppearance appearance,
            Texture2D texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            Vector2 topleft,
            Vector2 topright,
            Vector2 bottomleft,
            Vector2 bottomright,
            Vector3 normal,
            float angle
        )
        {
            if (countParticle >= MAX_PARTICLES) return;
            var idx = GetAppFxIdx(instance, appearance);
            int start = currentBillboards.AddRectAppearance(Position, size, color, topleft, topright, bottomleft, bottomright, normal, angle);
            draws[countParticle++] = new ParticleDraw()
            {
                Texture = texture,
                AppearanceIdx = idx,
                StartVertex = start
            };
        }

        struct ParticleDraw : IComparable<ParticleDraw>
        {
            public Texture2D Texture;
            public int AppearanceIdx;
            public int StartVertex;

            public int CompareTo(ParticleDraw other)
            {
                return AppearanceIdx.CompareTo(other.AppearanceIdx);
            }
        }
    }
}
