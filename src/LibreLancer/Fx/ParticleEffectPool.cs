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
using LibreLancer.Resources;

namespace LibreLancer.Fx
{
    public unsafe class ParticleEffectPool : IDisposable
    {
        //Limits
        public const int MaxParticles = 30_000;
        public const int MaxParticlesPerCall = 2000;

        VertexBuffer vbo;
        ElementBuffer ebo;

        VertexPositionColorTexture* vertices;
        CommandBuffer cmd;

        private QuadMaterial basicMaterial = new(null);

        void CreateQuad(Vector3 p, Vector2 size, Color4 color, float angle, ParticleTexture texture, float frame,
            Vector3 src_right, Vector3 src_up, bool flipU, bool flipV)
        {
            var frameNo = (int)Math.Floor((texture.FrameCount - 1) * frame);
            if (nextParticle + 4 >= (MaxParticles * 6))
                throw new Exception("Particle overflow");
            var texCoords = texture.GetCoordinates(frameNo);
            var (sin, cos) = MathF.SinCos(angle);

            float top = flipV ? texCoords.Y + texCoords.W : texCoords.Y;
            float bottom = flipV ? texCoords.Y : texCoords.Y + texCoords.W;
            float left = flipU ? texCoords.X + texCoords.Z : texCoords.X;
            float right = flipU ? texCoords.X : texCoords.X + texCoords.Z;

            var vUp = (cos * src_right - sin * src_up) * (0.5f * size.X);
            var vRight = (sin * src_right + cos * src_up) * (0.5f * size.Y);

            //bottom-left
            vertices[nextParticle++] = new(
                p - vRight - vUp,
                color,
                new(left, bottom)
            );

            //bottom-right
            vertices[nextParticle++] = new(
                p + vRight - vUp,
                color,
                new Vector2(right, bottom)
            );

            //top-left
            vertices[nextParticle++] = new(
                p - vRight + vUp,
                color,
                new Vector2(left, top)
            );

            //top-right
            vertices[nextParticle++] = new(
                p + vRight + vUp,
                color,
                new Vector2(right, top)
            );
        }

        public ParticleEffectPool(RenderContext context, CommandBuffer commands)
        {
            cmd = commands;
            //Set up vertices
            vbo = new VertexBuffer(context, typeof(VertexPositionColorTexture), MaxParticles * 4, true);
            ebo = new ElementBuffer(context, MaxParticlesPerCall * 6);
            var indices = new ushort[MaxParticlesPerCall * 6];
            int iptr = 0;
            for (int i = 0; i < (MaxParticlesPerCall * 4); i += 4)
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
            ebo.SetData(indices);
            vbo.SetElementBuffer(ebo);
        }

        ICamera camera;

        public ICamera Camera => camera;

        public PolylineRender Lines;

        public void StartFrame(ICamera camera, PolylineRender lines)
        {
            this.camera = camera;
            Lines = lines;
            lines.StartFrame();
            vertices = (VertexPositionColorTexture*)vbo.BeginStreaming();
            basicMaterial.Parameters.Clear();
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
            float angle,
            bool flipU,
            bool flipV
            )
        {
            var right = Vector3.Cross(normal, Vector3.UnitY);
            var up = Vector3.Cross(right, normal);
            up.Normalize();
            right.Normalize();
            CreateQuad(
                pos, size, color, angle, texture, frame,
                right, up, flipU, flipV
            );
        }

        public void AddBasic(
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            float angle,
            bool flipU,
            bool flipV
        )
        {
            CreateQuad(
                Position, size, color, angle, texture, frame,
                camera.View.GetRight(), camera.View.GetUp(),
                flipU, flipV
            );
        }

        public void AddRect(
            ParticleTexture texture,
            Vector3 Position,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle,
            bool flipU,
            bool flipV
        )
        {
            var up = normal;
            var toCamera = (camera.Position - Position).Normalized();
            var right = Vector3.Cross(toCamera, up);
            CreateQuad(
                Position, size, color, angle, texture, frame,
                right, up, flipU, flipV
            );
        }

        public void DrawBuffer(FxBasicAppearance app, ResourceManager res, Matrix4x4 tr, int drawIndex)
        {
            if (lastDrawCommand == nextParticle)
                return;
            var z = RenderHelpers.GetZ(camera.Position, Vector3.Transform(Vector3.Zero, tr));
            var texture = app.TextureHandler.Texture ?? res.WhiteTexture;
            if (texture == null) throw new InvalidOperationException("texture null");
            cmd.AddCommand(
                basicMaterial, null,
                cmd.WorldBuffer.Identity, Lighting.Empty,
                vbo, PrimitiveTypes.TriangleList, lastDrawCommand, 0,  ((nextParticle - lastDrawCommand) / 4 * 2),
                SortLayers.OBJECT, z, null, drawIndex,
                basicMaterial.AddParameters(texture, app.BlendInfo)
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
            ebo.Dispose();
        }
    }
}
