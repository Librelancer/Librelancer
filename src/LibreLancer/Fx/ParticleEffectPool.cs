// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibreLancer.Fx;
using LibreLancer.Vertices;

namespace LibreLancer.Fx
{
    public class ParticleEffectPool : IDisposable
    {
        //Limits
        const int MAX_PARTICLES = 40000;
        //How many will render at once
        const int MAX_APP_NODES = 2048;
        const int MAX_BEAMS = 512;
        const int MAX_APP_PARTICLES = 5000;

        public Particle[] Particles = new Particle[MAX_PARTICLES];
        public Queue<int> FreeParticles = new Queue<int>();

        ElementBuffer ibo;
        VertexBuffer vbo;
        ParticleVertex[] vertices = new ParticleVertex[MAX_PARTICLES * 4];
        CommandBuffer cmd;

        ShaderVariables basicShader;

        [StructLayout(LayoutKind.Sequential)]
        struct ParticleVertex : IVertexType
        {
            public Vector3 Position;
            public Color4 Color;
            public Vector2 TextureCoordinate;
            public Vector3 Dimensions;
            public Vector3 Right;
            public Vector3 Up;
            public VertexDeclaration GetVertexDeclaration()
            {
                return new VertexDeclaration(
                    sizeof(float) * 3 + sizeof(float) * 4 + sizeof(float) * 2 + sizeof(float) * 3 * 3,
                    new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
                    new VertexElement(VertexSlots.Color, 4, VertexElementType.Float, false, sizeof(float) * 3),
                    new VertexElement(VertexSlots.Texture1, 2, VertexElementType.Float, false, sizeof(float) * 7),
                    new VertexElement(VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 9),
                    new VertexElement(VertexSlots.Right, 3, VertexElementType.Float, false, sizeof(float) * 12),
                    new VertexElement(VertexSlots.Up, 3, VertexElementType.Float, false, sizeof(float) * 15)
                );
            }
        }

        void CreateQuad(ref int count, Vector3 position, Vector2 size, Color4 color, float angle, Vector2 topleft, Vector2 topright, Vector2 bottomleft, Vector2 bottomright, Vector3 src_right, Vector3 src_up)
        {
            var sz1 = new Vector3(size.X * -0.5f, size.Y * -0.5f, angle);
            var sz2 = new Vector3(size.X * 0.5f, size.Y * -0.5f, angle);
            var sz3 = new Vector3(size.X * -0.5f, size.Y * 0.5f, angle);
            var sz4 = new Vector3(size.X * 0.5f, size.Y * 0.5f, angle);
            vertices[count++] = new ParticleVertex()
            {
                Position = position,
                Color = color,
                TextureCoordinate = bottomleft,
                Dimensions = sz1,
                Right = src_right,
                Up = src_up
            };
            vertices[count++] = new ParticleVertex()
            {
                Position = position,
                Color = color,
                TextureCoordinate = topleft,
                Dimensions = sz2,
                Right = src_right,
                Up = src_up
            };
            vertices[count++] = new ParticleVertex()
            {
                Position = position,
                Color = color,
                TextureCoordinate = bottomright,
                Dimensions = sz3,
                Right = src_right,
                Up = src_up
            };
            vertices[count++] = new ParticleVertex()
            {
                Position = position,
                Color = color,
                TextureCoordinate = topright,
                Dimensions = sz4,
                Right = src_right,
                Up = src_up
            };
        }
        public ParticleEffectPool(CommandBuffer commands)
        {
            cmd = commands;
            //Free particles (is this efficient?)
            for(int i = 0; i < MAX_PARTICLES; i++)
            {
                FreeParticles.Enqueue(i);
            }
            //Set up vertices
            vbo = new VertexBuffer(typeof(ParticleVertex), MAX_PARTICLES * 4, true);
            //Indices
            ibo = new ElementBuffer(MAX_PARTICLES * 6);
            ushort[] indices = new ushort[MAX_PARTICLES * 6];
            int iptr = 0;
            for (int i = 0; i < (MAX_PARTICLES * 4); i += 4)
            {
                //Triangle 1
                indices[iptr++] = (ushort)i;
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 2);
                //Triangle 2
                indices[iptr++] = (ushort)(i + 1);
                indices[iptr++] = (ushort)(i + 3);
                indices[iptr++] = (ushort)(i + 2);
            }
            ibo.SetData(indices);
            vbo.SetElementBuffer(ibo);
            basicShader = ShaderCache.Get("Particle.vs", "Billboard.frag");
        }

        int maxActive = 0;
        public void Update(TimeSpan delta)
        {
            maxActive = 0;
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
                maxActive = Math.Max(maxActive, i);
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
                    Particles[i].Instance.ParticleCounts[Particles[i].Emitter.EmitterIndex]--;
                    Particles[i].Instance = null;
                    FreeParticles.Enqueue(i);
                }
            }
        }

        public int GetFreeParticle()
        {
            if (FreeParticles.Count > 0)
            {
                var p = FreeParticles.Dequeue();
                maxActive = Math.Max(p, maxActive);
                return p;
            }
            else
                return -1;
        }

        struct BufferInfo
        {
            public int Start;
            public int Count;
            public int Current;
        }

        //Drawing info
        (ParticleEffectInstance i, FxAppearance a)[] appearances = new (ParticleEffectInstance i, FxAppearance a)[MAX_APP_NODES];
        ParticleEffectInstance[] beams = new ParticleEffectInstance[MAX_BEAMS];
        BufferInfo[] bufspace = new BufferInfo[MAX_APP_NODES];
        int countApp = 0;
        ICamera camera;

        public ICamera Camera => camera;

        public void Draw(ICamera camera, PolylineRender polyline, ResourceManager res, PhysicsDebugRenderer debug)
        {
            this.camera = camera;
            countApp = 0;
            int beamPtr = 0;
            //Generate list of active nodes
            for (int i = 0; i < maxActive; i++)
            {
                if (!Particles[i].Active)
                    continue;
                var inst = Particles[i].Instance;
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
                        var idx = GetAppFxIdx(inst, app);
                        if (idx != -1) {
                            bufspace[idx].Count++;
                            if (bufspace[idx].Count >= MAX_APP_PARTICLES) bufspace[idx].Count = MAX_APP_PARTICLES;
                        }
                    }
                }
            }
            if (countApp <= 0) return; //No particles no drawing!

            for(int i = 1; i < countApp; i++) {
                bufspace[i].Start = (bufspace[i - 1].Start + bufspace[i - 1].Count);
                bufspace[i].Current = bufspace[i].Start * 4;
            }
            int maxVbo = (bufspace[countApp - 1].Start + bufspace[countApp - 1].Count) * 4;
            //Fill buffers
            for (int i = 0; i < maxActive; i++)
            {
                if (!Particles[i].Active)
                    continue;
                var inst = Particles[i].Instance;
                if (inst.NodeEnabled(Particles[i].Appearance))
                {
                    var app = (FxAppearance)Particles[i].Appearance.Node;
                    app.Debug = debug;
                    app.Draw(ref Particles[i], i, (float)inst.LastTime, (float)inst.GlobalTime, Particles[i].Appearance, inst.Resources, inst, ref inst.DrawTransform, inst.DrawSParam);
                }
            }
            //Set shader params early
            var view = camera.View;
            var vp = camera.ViewProjection;
            basicShader.SetViewProjection(ref vp);
            //Draw buffers
            int basicCount = 0;

            for (int i = 0; i < countApp; i++)
            {
                //Get Variables
                var ni = appearances[i];
                var pos = ni.i.DrawTransform.Transform(Vector3.Zero);
                var z = RenderHelpers.GetZ(camera.Position, pos);
                var startIndex = bufspace[i].Start * 6;
                var primCount = bufspace[i].Count * 2;
                //Draw
                Texture2D texture;
                switch(ni.a)
                {
                    case FxPerpAppearance perp:
                        perp.GetTexture2D(res, out texture);
                        if (texture == null) throw new InvalidOperationException("texture null");
                        cmd.AddCommand(
                            basicShader.Shader,
                            SetupShader,
                            EnableCull,
                            Matrix4.Identity,
                            new RenderUserData() { Texture = texture, Float = (float)perp.BlendInfo },
                            vbo, PrimitiveTypes.TriangleList, 0, startIndex, primCount, true,
                            SortLayers.OBJECT, z
                        );
                        basicCount += primCount / 2;
                        break;
                    case FxRectAppearance rect:
                        rect.GetTexture2D(res, out texture);
                        if (texture == null) throw new InvalidOperationException("texture null");
                        cmd.AddCommand(
                            basicShader.Shader,
                            SetupShader,
                            EnableCull,
                            Matrix4.Identity,
                            new RenderUserData() { Texture = texture, Float = (float)rect.BlendInfo },
                            vbo, PrimitiveTypes.TriangleList, 0, startIndex, primCount, true,
                            SortLayers.OBJECT, z
                        );
                        basicCount += primCount / 2;
                        break;
                    case FxOrientedAppearance orient:
                        break;
                    case FxBasicAppearance basic:
                        basic.GetTexture2D(res, out texture);
                        if (texture == null) throw new InvalidOperationException("texture null");
                        cmd.AddCommand(
                            basicShader.Shader,
                            SetupShader,
                            EnableCull,
                            Matrix4.Identity,
                            new RenderUserData() { Texture = texture, Float = (float)basic.BlendInfo },
                            vbo, PrimitiveTypes.TriangleList, 0, startIndex, primCount, true,
                            SortLayers.OBJECT, z
                        );
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
            //Upload to vbo
            vbo.SetData(vertices, maxVbo);
        }

        static void SetupShader(Shader shdr, RenderState res, ref RenderCommand cmd)
        {
            cmd.UserData.Texture.BindTo(0);
            res.BlendMode = (BlendMode)cmd.UserData.Float;
            res.Cull = false;
        }
        static void EnableCull(RenderState rs)
        {
            rs.Cull = true;
        }
        int GetAppFxIdx(ParticleEffectInstance instance, FxAppearance a)
        {
            var item = (instance, a);
            for(int i = 0; i < countApp; i++) {
                if (appearances[i].Equals(item)) return i;
            }
            if (countApp + 1 >= MAX_APP_NODES) return -1;
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
            var idx = GetAppFxIdx(instance, appearance);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count) * 4) return;
            var right = Vector3.Cross(normal, Vector3.UnitY);
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            right.Normalize();
            CreateQuad(
                ref bufspace[idx].Current,
                pos, size, color, angle, topleft, topright, bottomleft, bottomright,
                right, up
            );
        }

        public void DrawBasic(
            ParticleEffectInstance instance,
            FxBasicAppearance appearance,
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
            var idx = GetAppFxIdx(instance, appearance);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count) * 4) return;
            CreateQuad(
                ref bufspace[idx].Current, 
                Position, size, color, angle, topleft, topright, bottomleft, bottomright,
                camera.View.GetRight(), camera.View.GetUp()
            );
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
            var idx = GetAppFxIdx(instance, appearance);
            if (bufspace[idx].Current == (bufspace[idx].Start + bufspace[idx].Count) * 4) return;
            var up = normal;
            var toCamera = (camera.Position - Position).Normalized();
            var right = Vector3.Cross(toCamera, up);
            CreateQuad(
                ref bufspace[idx].Current,
                Position, size, color, angle, topleft, topright, bottomleft, bottomright,
                right, up
            );
        }

        public void Dispose()
        {
            vbo.Dispose();
            ibo.Dispose();
        }
    }
}
