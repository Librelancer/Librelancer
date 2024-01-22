// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.GameData.Archetypes;
using LibreLancer.Graphics;
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

        void AddQuad(List<VertexBillboardColor2> vx, Vector3 pos, Vector2 size, float angle, Color4 c1, Color4 c2)
        {
            vx.Add(new VertexBillboardColor2(
                pos, -0.5f * size.X, -0.5f * size.Y, angle,
                c1,c2,
                new Vector2(0, 0)
            ));
            vx.Add(new VertexBillboardColor2(
                pos, 0.5f * size.X, -0.5f * size.Y, angle,
                c1,c2,
                new Vector2(1, 0)
            ));
            vx.Add(new VertexBillboardColor2(
                pos, -0.5f * size.X, 0.5f * size.Y, angle,
                c1,c2,
                new Vector2(0, 1)
            ));
            vx.Add(new VertexBillboardColor2(
                pos, 0.5f * size.X, 0.5f * size.Y, angle,
                c1,c2,
                new Vector2(1, 1)
            ));
        }

        public override void Update(double elapsed, Vector3 position, Matrix4x4 transform)
		{
			pos = position;
            if (vertices == null) { CreateVertices(); }
        }

        void CreateVertices()
        {
            var vx = new List<VertexBillboardColor2>();
            //center
            if (Sun.CenterSprite != null)
                AddQuad(vx, pos, new Vector2(Sun.Radius * Sun.CenterScale), 0,
                    new Color4(Sun.CenterColorInner, 1),
                    new Color4(Sun.CenterColorOuter, 1));
            //glow
            AddQuad(vx, pos, new Vector2(Sun.Radius * Sun.GlowScale), 0,
                new Color4(Sun.GlowColorInner, 1),
                new Color4(Sun.GlowColorOuter, 1));
            //spines
            if (Sun.SpinesSprite != null)
            {
                double current_angle = 0;
                double delta_angle = (2 * Math.PI) / Sun.Spines.Count;
                for (int i = 0; i < Sun.Spines.Count; i++)
                {
                    current_angle += delta_angle;
                    var s = Sun.Spines[i];
                    AddQuad(vx,
                        pos,
                        new Vector2(Sun.Radius, Sun.Radius) * Sun.SpinesScale * new Vector2(s.WidthScale / s.LengthScale, s.LengthScale),
                        (float)current_angle,
                        new Color4(s.InnerColor, s.Alpha),
                        new Color4(s.OuterColor, s.Alpha)
                    );
                }
            }
            vertices = vx.ToArray();
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
            return true;
        }

        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            if (sysr == null || vertices == null)
                return;
            float z = RenderHelpers.GetZ(Matrix4x4.Identity, camera.Position, pos);
            if (z > 900000) // Reduce artefacts from fast Z-sort calculation. This'll probably cause issues somewhere else
                z = 900000;
            var dist_scale = nr != null ? nr.Nebula.SunBurnthroughScale : 1;
            var alpha = nr != null ? nr.Nebula.SunBurnthroughIntensity : 1;
            int idx = sysr.StaticBillboards.DoVertices(ref ID, vertices);
            //draw center
            if (Sun.CenterSprite != null)
            {
                centerMaterial.SizeMultiplier = new Vector2(dist_scale);
                centerMaterial.OuterAlpha = alpha;
                commands.AddCommand(centerMaterial, null, commands.WorldBuffer.Identity,
                    Lighting.Empty, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                    0, idx, 2, SortLayers.SUN, z);
                idx += 6;
            }
            //draw glow
            glowMaterial.SizeMultiplier = new Vector2(dist_scale);
            glowMaterial.OuterAlpha = alpha;
            commands.AddCommand(glowMaterial, null, commands.WorldBuffer.Identity,
                Lighting.Empty, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                0, idx, 2, SortLayers.SUN, z, null, 1);
            //next
            idx += 6;
            //draw spines
            if (Sun.SpinesSprite != null && nr == null)
            {
                commands.AddCommand(spineMaterial, null, commands.WorldBuffer.Identity,
                    Lighting.Empty, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                    0, idx, 2 * Sun.Spines.Count, SortLayers.SUN, z, null, 2);
            }
        }
    }
}

