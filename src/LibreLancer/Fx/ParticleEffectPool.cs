// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Fx;
using LibreLancer.Render;
using LibreLancer.Render.Materials;
using LibreLancer.Vertices;

namespace LibreLancer.Fx
{
    public unsafe class ParticleEffectPool : IDisposable
    {
        //Limits
        private const int PARTICLE_BIT_COUNT = 1250;
        const int MAX_PARTICLES = PARTICLE_BIT_COUNT * 32;
        private const int INITIAL_PARTICLES = 1000;
        //How many will render at once
        const int MAX_APP_NODES = 2048;
        const int MAX_BEAMS = 512;
        const int MAX_APP_PARTICLES = 5000;

        public IdPool ParticleAllocator = new IdPool(PARTICLE_BIT_COUNT, false);
        public Particle[] Particles = new Particle[INITIAL_PARTICLES];

        VertexBuffer vbo;
        ParticleVertex* vertices;
        CommandBuffer cmd;

        private ParticleMaterial basicMaterial = new ParticleMaterial(null);

        [StructLayout(LayoutKind.Sequential)]
        struct ParticleVertex : IVertexType
        {
            public Vector3 Position;
            public Color4 Color;
            public Vector4 TextureCoordinates;
            public Vector3 Dimensions;
            public Vector3 Right;
            public Vector3 Up;
            public VertexDeclaration GetVertexDeclaration()
            {
                return new VertexDeclaration(
                    sizeof(float) * 3 + sizeof(float) * 4 * 2 + sizeof(float) * 3 * 3,
                    new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
                    new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 3),
                    new VertexElement(VertexSlots.Texture1, 4, VertexElementType.Float, false, sizeof(float) * 7),
                    new VertexElement(VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 11),
                    new VertexElement(VertexSlots.Right, 3, VertexElementType.Float, false, sizeof(float) * 14),
                    new VertexElement(VertexSlots.Up, 3, VertexElementType.Float, false, sizeof(float) * 17)
                );
            }
        }

        void CreateQuad(ref int count, Vector3 position, Vector2 size, Color4 color, float angle, ParticleTexture texture, float frame, Vector3 src_right, Vector3 src_up)
        {
            var sz = new Vector3(size.X, size.Y, angle);
            var frameNo = (int)Math.Floor((texture.FrameCount - 1) * frame);
            vertices[count++] = new ParticleVertex()
            {
                Position = position,
                Color = color,
                TextureCoordinates = texture.GetCoordinates(frameNo),
                Dimensions = sz,
                Right = src_right,
                Up = src_up
            };
        }

        public ParticleEffectPool(CommandBuffer commands)
        {
            cmd = commands;
            //Set up vertices
            vbo = new VertexBuffer(typeof(ParticleVertex), MAX_PARTICLES, true);
        }

        public void Update(double delta)
        {
            foreach (var i in ParticleAllocator.GetAllocated())
            {
                Particles[i].Position += Particles[i].Normal * (float)delta;
                Particles[i].TimeAlive += (float)delta;
                if (Particles[i].TimeAlive >= Particles[i].LifeSpan)
                {
                    Particles[i].Instance.ParticleCounts[Particles[i].Emitter.EmitterIndex]--;
                    Particles[i].Instance = null;
                    ParticleAllocator.Free(i);
                }
            }
        }

        public void KillAll(ParticleEffectInstance instance)
        {
            foreach (var i in ParticleAllocator.GetAllocated())
            {
                if(Particles[i].Instance == instance)
                {
                    Particles[i].Instance.ParticleCounts[Particles[i].Emitter.EmitterIndex]--;
                    Particles[i].Instance = null;
                    ParticleAllocator.Free(i);
                }
            }
        }

        public int GetFreeParticle()
        {
            if (ParticleAllocator.TryAllocate(out int a))
            {
                if (a >= Particles.Length) {
                    Array.Resize(ref Particles, Particles.Length * 2 > MAX_PARTICLES ? MAX_PARTICLES : Particles.Length * 2);
                }
                return a;
            }
            return -1;
        }

        struct BufferInfo
        {
            public int Start;
            public int Count;
            public int Current;
        }

        //Drawing info
        (ParticleEffectInstance i, FxAppearance a, int idx)[] appearances = new (ParticleEffectInstance i, FxAppearance a, int idx)[MAX_APP_NODES];
        ParticleEffectInstance[] beams = new ParticleEffectInstance[MAX_BEAMS];
        BufferInfo[] bufspace = new BufferInfo[MAX_APP_NODES];
        int countApp = 0;
        ICamera camera;

        public ICamera Camera => camera;

        private int fNo = 0;

        public void Draw(ICamera camera, PolylineRender polyline, ResourceManager res, LineRenderer debug)
        {
            this.camera = camera;
            countApp = 0;
            int beamPtr = 0;
            fNo++;
            //Generate list of active nodes
            foreach(var i in ParticleAllocator.GetAllocated())
            {
                var inst = Particles[i].Instance;
                if(inst.Culled) continue;
                inst.Pool = this; //HACK
                if(inst.NodeEnabled(Particles[i].Appearance))
                {
                    var app = (FxAppearance)Particles[i].Appearance.Node;
                    //check for beams
                    if(app is FLBeamAppearance) {
                        bool append = true;
                        for(int j = 0; j < beamPtr; j++)
                        {
                            if (beams[j] == inst) { append = false; break; }
                        }
                        if (append)
                            beams[beamPtr++] = inst;
                    } else if (app is FxParticleAppearance || app is FxMeshAppearance)
                    {
                        //FxParticleAppearance/FxMeshAppearance does not
                        //render with vertices
                    }
                    else { //getindex
                        var idx = GetAppFxIdx(inst, app, Particles[i].Appearance.Index);
                        if (idx != -1) {
                            bufspace[idx].Count++;
                            if (bufspace[idx].Count >= MAX_APP_PARTICLES) bufspace[idx].Count = MAX_APP_PARTICLES;
                        }
                    }
                }
            }
            if (countApp <= 0) return; //No particles no drawing!
            for (int i = 1; i < countApp; i++) {
                bufspace[i].Start = (bufspace[i - 1].Start + bufspace[i - 1].Count);
                bufspace[i].Current = bufspace[i].Start;
            }
            int maxVbo = (bufspace[countApp - 1].Start + bufspace[countApp - 1].Count);
            //Fill buffer
            vertices = (ParticleVertex*)vbo.BeginStreaming();
            foreach (var i in ParticleAllocator.GetAllocated())
            {
                var inst = Particles[i].Instance;
                if(inst.Culled) continue;
                if (inst.NodeEnabled(Particles[i].Appearance))
                {
                    var app = (FxAppearance)Particles[i].Appearance.Node;
                    app.Debug = debug;
                    app.Draw(ref Particles[i], i, (float)inst.LastTime, (float)inst.GlobalTime, Particles[i].Appearance, inst.Resources, inst, ref inst.DrawTransform, inst.DrawSParam);
                    app.Debug = null;
                }
            }
            vbo.EndStreaming(maxVbo);
            //Draw buffers
            int basicCount = 0;
            basicMaterial.Library = res;
            basicMaterial.Parameters.Clear();
            for (int i = 0; i < countApp; i++)
            {
                //Get Variables
                var ni = appearances[i];
                var pos = Vector3.Transform(Vector3.Zero, ni.i.DrawTransform);
                var z = RenderHelpers.GetZ(camera.Position, pos);
                var startIndex = bufspace[i].Start;
                var primCount = bufspace[i].Count;
                //Draw
                Texture2D texture;
                int drawIdx = (ni.i.DrawIndex << 11) + ni.idx;
                switch(ni.a)
                {
                    case FxPerpAppearance perp:
                        texture = perp.TextureHandler.Texture ?? res.WhiteTexture;
                        if (texture == null) throw new InvalidOperationException("texture null");
                        cmd.AddCommand(
                            basicMaterial, null,
                            cmd.WorldBuffer.Identity, Lighting.Empty,
                            vbo, PrimitiveTypes.Points, -1, startIndex, primCount,
                            SortLayers.OBJECT, z, null, drawIdx, basicMaterial.Parameters.Count
                        );
                        basicMaterial.Parameters.Add((texture, perp.BlendInfo));
                        basicCount += primCount / 2;
                        break;
                    case FxRectAppearance rect:
                        texture = rect.TextureHandler.Texture ?? res.WhiteTexture;
                        if (texture == null) throw new InvalidOperationException("texture null");
                        cmd.AddCommand(
                            basicMaterial, null,
                            cmd.WorldBuffer.Identity, Lighting.Empty,
                            vbo, PrimitiveTypes.Points, -1, startIndex, primCount,
                            SortLayers.OBJECT, z, null, drawIdx, basicMaterial.Parameters.Count
                        );
                        basicMaterial.Parameters.Add((texture, rect.BlendInfo));
                        basicCount += primCount / 2;
                        break;
                    case FxOrientedAppearance orient:
                        break;
                    case FxBasicAppearance basic:
                        texture = basic.TextureHandler.Texture ?? res.WhiteTexture;
                        cmd.AddCommand(
                            basicMaterial, null,
                            cmd.WorldBuffer.Identity, Lighting.Empty,
                            vbo, PrimitiveTypes.Points, -1, startIndex, primCount,
                            SortLayers.OBJECT, z, null, drawIdx, basicMaterial.Parameters.Count
                        );
                        basicMaterial.Parameters.Add((texture, basic.BlendInfo));
                        basicCount += primCount / 2;
                        break;
                    default:
                        throw new InvalidOperationException(ni.a.GetType().Name);
                }
                //Clear
                bufspace[i] = new BufferInfo();
            }
            //Draw beams!
            for (int i = 0; i < beamPtr; i++)
            {
                beams[i].DrawBeams(polyline, debug, beams[i].DrawTransform, beams[i].DrawSParam);
            }
        }
        
        int GetAppFxIdx(ParticleEffectInstance instance, FxAppearance a, int index)
        {
            if (instance.FrameNumber != fNo) {
                instance.FrameNumber = fNo;
                for (int i = 0; i < instance.ParticleIndex.Length; i++)
                {
                    instance.ParticleIndex[i] = -1;
                }
            }
            if (instance.ParticleIndex[index] == -1)
            {
                if (countApp + 1 >= MAX_APP_NODES) return -1;
                appearances[countApp] = (instance, a, index);
                instance.ParticleIndex[index] = countApp++;
            }
            return instance.ParticleIndex[index];
        }

        public void DrawPerspective(
            ParticleEffectInstance instance,
            FxBasicAppearance appearance,
            ParticleTexture texture,
            Vector3 pos,
            Matrix4x4 world,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle,
            int index)
        {
            var idx = GetAppFxIdx(instance, appearance, index);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count)) return;
            var right = Vector3.Cross(normal, Vector3.UnitY);
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            right.Normalize();
            CreateQuad(
                ref bufspace[idx].Current,
                pos, size, color, angle, texture, frame,
                right, up
            );
        }

        public void DrawBasic(
            ParticleEffectInstance instance,
            FxBasicAppearance appearance,
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            float angle,
            int index
        )
        {
            var idx = GetAppFxIdx(instance, appearance, index);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count)) return;
            CreateQuad(
                ref bufspace[idx].Current, 
                Position, size, color, angle, texture, frame,
                camera.View.GetRight(), camera.View.GetUp()
            );
        }

        public void DrawRect(
            ParticleEffectInstance instance, 
            FxBasicAppearance appearance,
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle,
            int index
        )
        {
            var idx = GetAppFxIdx(instance, appearance, index);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count)) return;
            var up = normal;
            var toCamera = (camera.Position - Position).Normalized();
            var right = Vector3.Cross(toCamera, up);
            CreateQuad(
                ref bufspace[idx].Current,
                Position, size, color, angle, texture, frame,
                right, up
            );
        }

        public void Dispose()
        {
            vbo.Dispose();
        }
    }
}
