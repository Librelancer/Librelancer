// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Render.Materials;

namespace LibreLancer.Fx
{
    public unsafe class ParticleEffectPool : IDisposable
    {
        //Limits
        public const int MAX_PARTICLES = 80_000;

        VertexBuffer vbo;
        ParticleVertex* vertices;
        CommandBuffer cmd;

        private ParticleMaterial basicMaterial = new ParticleMaterial(null);


        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        struct Vector3s
        {
            public short X;
            public short Y;
            public short Z;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        struct ParticleVertex : IVertexType
        {
            public Vector3 Position;
            public Vector3 Dimensions;
            public Vector4 TextureCoordinates;
            public Vector3s Right;
            public Vector3s Up;
            public VertexDiffuse Color;
            public VertexDeclaration GetVertexDeclaration()
            {
                return new VertexDeclaration(
                    (sizeof(float) * 3) * 2 +
                    (sizeof(float) * 4) +
                    sizeof(short) * 3 * 2 +
                    sizeof(uint),
                    new VertexElement(VertexSlots.Position, 3, VertexElementType.Float, false, 0),
                    new VertexElement(VertexSlots.Dimensions, 3, VertexElementType.Float, false, sizeof(float) * 3),
                    new VertexElement(VertexSlots.Texture1, 4, VertexElementType.Float, false, sizeof(float) * 6),
                    new VertexElement(VertexSlots.Right, 3, VertexElementType.Short, true, sizeof(float) * 10),
                    new VertexElement(VertexSlots.Up, 3, VertexElementType.Short, true, sizeof(float) * 10 + sizeof(short) * 3),
                    new VertexElement(VertexSlots.Color, 4, VertexElementType.UnsignedByte, true, sizeof(float) * 10 + sizeof(short) * 6)
                );
            }
        }

        //GPU snorm is a bit odd
        static short FloatToSNorm(float v) => (short) (MathHelper.Clamp(
            v >= 0.0f ? (v * 32767.0f + 0.5f) : (v * 32767.0f - 0.5f),
            -32768.0f,
            32767.0f));

        static Vector3s PackNormal(Vector3 normal)
        {
            var n = normal.Normalized();
            return new Vector3s()
            {
                X = FloatToSNorm(n.X),
                Y = FloatToSNorm(n.Y),
                Z = FloatToSNorm(n.Z),
            };
        }

        void CreateQuad(Vector3 position, Vector2 size, Color4 color, float angle, ParticleTexture texture, float frame, Vector3 src_right, Vector3 src_up)
        {
            var sz = new Vector3(size.X, size.Y, angle);
            var frameNo = (int)Math.Floor((texture.FrameCount - 1) * frame);
            if (nextParticle + 1 >= MAX_PARTICLES)
                throw new Exception("Particle overflow");
            vertices[nextParticle++] = new ParticleVertex()
            {
                Position = position,
                Color = (VertexDiffuse)color,
                TextureCoordinates = texture.GetCoordinates(frameNo),
                Dimensions = sz,
                Right = PackNormal(src_right),
                Up = PackNormal(src_up),
            };
        }

        public ParticleEffectPool(RenderContext context, CommandBuffer commands)
        {
            cmd = commands;
            //Set up vertices
            vbo = new VertexBuffer(context, typeof(ParticleVertex), MAX_PARTICLES, true);
        }

        ICamera camera;

        public ICamera Camera => camera;

        public PolylineRender Lines;

        public void StartFrame(ICamera camera, PolylineRender lines)
        {
            this.camera = camera;
            Lines = lines;
            lines.StartFrame();
            vertices = (ParticleVertex*)vbo.BeginStreaming();
            basicMaterial.ResetParameters();
            lastDrawCommand = nextParticle = 0;
        }

        private int nextParticle = 0;
        private int lastDrawCommand = 0;

        public void AddPerp(
            ParticleTexture texture,
            Vector3 pos,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle
            )
        {
            var right = Vector3.Cross(normal, Vector3.UnitY);
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            right.Normalize();
            CreateQuad(
                pos, size, color, angle, texture, frame,
                right, up
            );
        }

        public void AddBasic(
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            float angle
        )
        {
            CreateQuad(
                Position, size, color, angle, texture, frame,
                camera.View.GetRight(), camera.View.GetUp()
            );
        }

        public void AddRect(
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle
        )
        {
            var up = normal;
            var toCamera = (camera.Position - Position).Normalized();
            var right = Vector3.Cross(toCamera, up);
            CreateQuad(
                Position, size, color, angle, texture, frame,
                right, up
            );
        }

        public void DrawBuffer(FxBasicAppearance app, ResourceManager res, Matrix4x4 tr, int drawIndex, bool flipU, bool flipV)
        {
            if (lastDrawCommand == nextParticle)
                return;
            var z = RenderHelpers.GetZ(camera.Position, Vector3.Transform(Vector3.Zero, tr));
            var texture = app.TextureHandler.Texture ?? res.WhiteTexture;
            if (texture == null) throw new InvalidOperationException("texture null");
            cmd.AddCommand(
                basicMaterial, null,
                cmd.WorldBuffer.Identity, Lighting.Empty,
                vbo, PrimitiveTypes.Points, -1, lastDrawCommand,  nextParticle - lastDrawCommand,
                SortLayers.OBJECT, z, null, drawIndex,
                basicMaterial.AddParameters(texture, app.BlendInfo, flipU, flipV)
            );
            basicMaterial.Library = res;
            lastDrawCommand = nextParticle;
        }

        public void EndFrame()
        {
            Lines.EndFrame();
            vbo.EndStreaming(lastDrawCommand);
        }

        public void Dispose()
        {
            vbo.Dispose();
        }
    }
}
