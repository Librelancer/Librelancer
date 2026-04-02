// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
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
        // Limits
        private const int PARTICLE_SIZE = 16 * sizeof(float);

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = PARTICLE_SIZE)]
        struct ParticleData
        {
            public Vector3 Position;
            public VertexDiffuse Color;
            public Vector3 Normal;
            public float Rotate;
            public Vector4 TextureCoordinates;
            public Vector2 HalfSize;
            // Padding After
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = PARTICLE_SIZE)]
        struct BeamData
        {
            public Vector3 Position;
            private float padding;
            public Vector3 Normal;
            public float HalfSize;
            public Vector4 TextureCoordinates;
            public Color4 Color;
        }

        public const int MaxParticles = 30_000;

        private VertexBuffer vbo;
        private StorageBuffer sbo;

        private CommandBuffer cmd;

        private ParticleMaterial particleMaterial;
        private ParticleBeamMaterial beamMaterial;

        private ICamera camera = null!;
        public ICamera Camera => camera;

        public ParticleEffectPool(RenderContext context, CommandBuffer commands)
        {
            cmd = commands;
            // Set up vertices
            vbo = new VertexBuffer(context, typeof(VertexPositionColorTexture), 0);
            sbo = new StorageBuffer(context, MaxParticles * PARTICLE_SIZE,
                PARTICLE_SIZE, typeof(ParticleData), true);
            particleMaterial = new(sbo);
            beamMaterial = new(sbo);
        }

        public void StartFrame(ICamera camera)
        {
            this.camera = camera;
            sbo.BeginStreaming();
            particleMaterial.Parameters.Clear();
            beamMaterial.Parameters.Clear();
            lastDrawCommand = nextParticle = 0;
        }

        private int lastDrawCommand = 0;
        private int nextParticle = 0;

        public void AddParticle(
            ParticleTexture texture,
            Vector3 pos,
            Vector2 size,
            Color4 color,
            float frame,
            Vector3 normal,
            float angle,
            bool flipU,
            bool flipV)
        {
            if (color.A <= 0.0001f) //won't render
                return;
            if (nextParticle == MaxParticles)
                return;
            var frameNo = (int) Math.Floor((texture.FrameCount - 1) * frame);
            var texCoords = texture.GetCoordinates(frameNo);
            float top = flipV ? texCoords.Y + texCoords.W : texCoords.Y;
            float bottom = flipV ? texCoords.Y : texCoords.Y + texCoords.W;
            float left = flipU ? texCoords.X + texCoords.Z : texCoords.X;
            float right = flipU ? texCoords.X : texCoords.X + texCoords.Z;
            sbo.Data<ParticleData>(nextParticle++) = new()
            {
                Position = pos,
                Color = (VertexDiffuse)color,
                Normal = normal,
                Rotate = angle,
                TextureCoordinates = new(left,top,right,bottom),
                HalfSize = size * 0.5f
            };
        }

        ref BeamData Beam(int index) => ref Unsafe.As<ParticleData, BeamData>(ref sbo.Data<ParticleData>(index));

        public void AddBeamPoint(
            Vector3 position,
            Vector3 forward,
            float halfSize,
            Vector4 textureCoordinates,
            Color4 color
        )
        {
            Beam(nextParticle++) = new()
            {
                Position = position,
                Normal = forward,
                HalfSize = halfSize,
                TextureCoordinates = textureCoordinates,
                Color = color
            };
        }

        public void DrawBeamBuffer(Texture texture, ushort blendInfo, float z)
        {
            int start = lastDrawCommand;
            int count = nextParticle - lastDrawCommand;

            if (count > 256)
            {
                throw new InvalidOperationException("256-limit for FLBeamAppearance exceeded (should never happen)");
            }

            var triCount = (count - 1) * 2;
            if (triCount == 0)
            {
                if (nextParticle != MaxParticles)
                {
                    nextParticle = sbo.GetAlignedIndex(nextParticle);
                }
                lastDrawCommand = nextParticle;
                return;
            }
            // Section one
            cmd.AddCommand(
                beamMaterial, null,
                cmd.WorldBuffer.Identity, Lighting.Empty,
                vbo, 1.0f, PrimitiveTypes.TriangleStrip, -1, 0, triCount,
                SortLayers.OBJECT, z, null, 0,
                beamMaterial.AddParameters(texture, blendInfo, false, start, count)
            );
            // Rotated section
            cmd.AddCommand(
                beamMaterial, null,
                cmd.WorldBuffer.Identity, Lighting.Empty,
                vbo, 1.0f, PrimitiveTypes.TriangleStrip, -1, 0, triCount,
                SortLayers.OBJECT, z, null, 1,
                beamMaterial.AddParameters(texture, blendInfo, true, start, count)
            );
            if (nextParticle != MaxParticles)
            {
                nextParticle = sbo.GetAlignedIndex(nextParticle);
            }
            lastDrawCommand = nextParticle;
        }


        public void DrawBuffer(ParticleDrawKind drawKind, FxBasicAppearance app, ResourceManager res, Matrix4x4 tr, int drawIndex)
        {
            if (lastDrawCommand == nextParticle)
                return;
            var z = RenderHelpers.GetZ(camera.Position, Vector3.Transform(Vector3.Zero, tr));
            var texture = app.TextureHandler.Texture ?? res.WhiteTexture;
            if (texture == null) throw new InvalidOperationException("texture null");

            int start = lastDrawCommand;
            int count = nextParticle - lastDrawCommand;
            while (count > 256)
            {
                cmd.AddCommand(
                    particleMaterial, null,
                    cmd.WorldBuffer.Identity, Lighting.Empty,
                    vbo, 1.0f, PrimitiveTypes.TriangleList, -1, 0, 256 * 2,
                    SortLayers.OBJECT, z, null, drawIndex,
                    particleMaterial.AddParameters(texture, app.BlendInfo, drawKind, start, 256)
                );
                count -= 256;
                start += 256;
            }
            cmd.AddCommand(
                particleMaterial, null,
                cmd.WorldBuffer.Identity, Lighting.Empty,
                vbo, 1.0f, PrimitiveTypes.TriangleList, -1, 0, count * 2,
                SortLayers.OBJECT, z, null, drawIndex,
                particleMaterial.AddParameters(texture, app.BlendInfo, drawKind, start, count)
            );
            if (nextParticle != MaxParticles)
            {
                nextParticle = sbo.GetAlignedIndex(nextParticle);
            }
            lastDrawCommand = nextParticle;
        }

        public void EndFrame()
        {
            sbo.EndStreaming(lastDrawCommand);
        }

        public void Dispose()
        {
            vbo.Dispose();
            sbo.Dispose();
        }
    }
}
