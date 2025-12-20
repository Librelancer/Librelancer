// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.Archetypes;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render.Cameras;
using LibreLancer.Render.Materials;

namespace LibreLancer.Render
{
    public class SunRenderer : ObjectRenderer
	{
        public Sun Sun { get; private set; }
		Vector3 pos;
		SystemRenderer sysr;
        int ID;
        VertexBillboardColor2[] vertices;

        private SunSpineMaterial spineMaterial;
        private SunRadialMaterial centerMaterial;
        private SunRadialMaterial glowMaterial;

		public SunRenderer (Sun sun)
		{
			Sun = sun;
            pos = Vector3.Zero;
        }

        static void AddQuad(VertexBillboardColor2[] vx, ref int i, Vector3 pos, Vector2 size, float angle, Color4 c1, Color4 c2)
        {
            vx[i++] = new VertexBillboardColor2(
                pos, -0.5f * size.X, -0.5f * size.Y, angle,
                c1,c2,
                new Vector2(0, 0)
            );
            vx[i++] = new VertexBillboardColor2(
                pos, 0.5f * size.X, -0.5f * size.Y, angle,
                c1,c2,
                new Vector2(1, 0)
            );
            vx[i++] = new VertexBillboardColor2(
                pos, -0.5f * size.X, 0.5f * size.Y, angle,
                c1,c2,
                new Vector2(0, 1)
            );
            vx[i++] = new VertexBillboardColor2(
                pos, 0.5f * size.X, 0.5f * size.Y, angle,
                c1,c2,
                new Vector2(1, 1)
            );
        }

        private Vector3 genPos;
        private int bufferIndex;

        public override void Update(double elapsed, Vector3 position, Matrix4x4 transform)
        {
            pos = position;
        }

        public static int GetVertexCount(Sun sun)
        {
            int count = 4; //glow quad
            if (sun.CenterSprite != null)
                count += 4; //center quad
            if (sun.SpinesSprite != null)
                count += sun.Spines.Count * 4;
            return count;
        }

        public static void CreateVertices(VertexBillboardColor2[] vx, Vector3 pos, Sun sun)
        {
            int idx = 0;
            //center
            if (sun.CenterSprite != null)
                AddQuad(vx, ref idx, pos, new Vector2(sun.Radius * sun.CenterScale), 0,
                    new Color4(sun.CenterColorInner, 1),
                    new Color4(sun.CenterColorOuter, 1));
            //glow
            AddQuad(vx, ref idx, pos, new Vector2(sun.Radius * sun.GlowScale), 0,
                new Color4(sun.GlowColorInner, 1),
                new Color4(sun.GlowColorOuter, 1));
            //spines
            if (sun.SpinesSprite != null)
            {
                double current_angle = 0;
                double delta_angle = (2 * Math.PI) / sun.Spines.Count;
                for (int i = 0; i < sun.Spines.Count; i++)
                {
                    current_angle += delta_angle;
                    var s = sun.Spines[i];
                    AddQuad(vx,
                        ref idx,
                        pos,
                        new Vector2(sun.Radius) * sun.SpinesScale * new Vector2(s.WidthScale / s.LengthScale, s.LengthScale),
                        (float)current_angle,
                        new Color4(s.InnerColor, s.Alpha),
                        new Color4(s.OuterColor, s.Alpha)
                    );
                }
            }
        }

        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
        {
            if (sysr == null)
            {
                spineMaterial = new SunSpineMaterial(sys.ResourceManager);
                spineMaterial.Texture = Sun.SpinesSprite;
                spineMaterial.SizeMultiplier = Vector2.One;
                centerMaterial = new SunRadialMaterial(sys.ResourceManager);
                centerMaterial.Texture = Sun.CenterSprite;
                centerMaterial.Additive = true;
                glowMaterial = new SunRadialMaterial(sys.ResourceManager);
                glowMaterial.Texture = Sun.GlowSprite;
            }

            sysr = sys;
            sys.AddObject(this);
            if (vertices == null || pos != genPos)
            {
                 vertices = new VertexBillboardColor2[GetVertexCount(Sun)];
                 CreateVertices(vertices, pos, Sun);
            }

            bufferIndex = vertices != null ? sys.QuadBuffer.DoVertices(vertices) : -1;
            return true;
        }

        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            if (sysr == null || vertices == null || bufferIndex == -1)
                return;
            float z = RenderHelpers.GetZ(Matrix4x4.Identity, camera.Position, pos);
            if (z > 900000) // Reduce artefacts from fast Z-sort calculation. This'll probably cause issues somewhere else
                z = 900000;
            var dist_scale = nr != null ? nr.Nebula.SunBurnthroughScale : 1;
            var alpha = nr != null ? nr.Nebula.SunBurnthroughIntensity : 1;
            var idx = bufferIndex;
            //draw center
            if (Sun.CenterSprite != null)
            {
                centerMaterial.SizeMultiplier = new Vector2(dist_scale);
                centerMaterial.OuterAlpha = alpha;
                commands.AddCommand(centerMaterial, null, commands.WorldBuffer.Identity,
                    Lighting.Empty, sysr.QuadBuffer.VertexBuffer, PrimitiveTypes.TriangleList,
                    0, idx, 2, SortLayers.SUN, z);
                idx += 6;
            }
            //draw glow
            glowMaterial.SizeMultiplier = new Vector2(dist_scale);
            glowMaterial.OuterAlpha = alpha;
            commands.AddCommand(glowMaterial, null, commands.WorldBuffer.Identity,
                Lighting.Empty, sysr.QuadBuffer.VertexBuffer, PrimitiveTypes.TriangleList,
                0, idx, 2, SortLayers.SUN, z, null, 1);
            //next
            idx += 6;
            //draw spines
            if (Sun.SpinesSprite != null
                && Sun.Spines.Count > 0
                && nr == null)
            {
                commands.AddCommand(spineMaterial, null, commands.WorldBuffer.Identity,
                    Lighting.Empty, sysr.QuadBuffer.VertexBuffer, PrimitiveTypes.TriangleList,
                    0, idx, 2 * Sun.Spines.Count, SortLayers.SUN, z, null, 2);
            }
        }
    }
}

