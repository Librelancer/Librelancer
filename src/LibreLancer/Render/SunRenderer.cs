// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.GameData.Archetypes;

namespace LibreLancer
{
    public class SunRenderer : ObjectRenderer
	{

		public Sun Sun { get; private set; }
		Vector3 pos;
		SystemRenderer sysr;
        int ID;
        VertexBillboardColor2[] vertices;

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

        public override void Update(TimeSpan elapsed, Vector3 position, Matrix4 transform)
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

        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            sysr = sys;
            sys.AddObject(this);
            return true;
        }

        static ShaderVariables radialShader;
        static ShaderVariables spineShader;
        static int radialTex0;
        static int spineTex0;
        static ShaderAction RadialSetup = (Shader shdr, RenderState res, ref RenderCommand cmd) =>
        {
            if (cmd.UserData.Float == 0)
                res.BlendMode = BlendMode.Additive;
            else
                res.BlendMode = BlendMode.Normal;
            shdr.SetInteger(radialTex0, 0);
            cmd.UserData.Texture.BindTo(0);
            res.Cull = false;
        };
        static ShaderAction SpineSetup = (Shader shdr, RenderState res, ref RenderCommand cmd) =>
        {
            res.BlendMode = BlendMode.Normal;
            shdr.SetInteger(spineTex0, 0);
            cmd.UserData.Texture.BindTo(0);
            res.Cull = false;
        };
        static Action<RenderState> Cleanup = (x) =>
        {
            x.Cull = true;
        };

        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            if (sysr == null || vertices == null)
                return;
            float z = RenderHelpers.GetZ(Matrix4.Identity, camera.Position, pos);
            if (z > 900000) // Reduce artefacts from fast Z-sort calculation. This'll probably cause issues somewhere else
                z = 900000;
            //var dist_scale = nr != null ? nr.Nebula.SunBurnthroughScale : 1; // TODO: Modify this based on nebula burn-through.
            //var alpha = nr != null ? nr.Nebula.SunBurnthroughIntensity : 1;
            //var glow_scale = dist_scale * Sun.GlowScale;
            if (radialShader == null)
            {
                radialShader = ShaderCache.Get("sun.vs", "sun_radial.frag");
                radialTex0 = radialShader.Shader.GetLocation("tex0");
            }
            if (spineShader == null)
            {
                spineShader = ShaderCache.Get("sun.vs", "sun_spine.frag");
                spineTex0 = spineShader.Shader.GetLocation("tex0");
            } 
            radialShader.SetViewProjection(camera);
            radialShader.SetView(camera);
            spineShader.SetViewProjection(camera);
            spineShader.SetView(camera);

            int idx = sysr.StaticBillboards.DoVertices(ref ID, vertices);

            if (Sun.CenterSprite != null)
            {
                //draw center
                var cr = (Texture2D)sysr.ResourceManager.FindTexture(Sun.CenterSprite);
                commands.AddCommand(radialShader.Shader, RadialSetup, Cleanup, Matrix4.Identity,
                new RenderUserData() { Float = 0, Texture = cr }, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                idx, 2, true, SortLayers.SUN, z);
                //next
                idx += 6;
            }
            //draw glow
            var gr = (Texture2D)sysr.ResourceManager.FindTexture(Sun.GlowSprite);
            commands.AddCommand(radialShader.Shader, RadialSetup, Cleanup, Matrix4.Identity,
                new RenderUserData() { Float = 1, Texture = gr }, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                idx, 2, true, SortLayers.SUN, z + 108f);
            //next
            idx += 6;
            //draw spines
            if(Sun.SpinesSprite != null && nr == null)
            {
                var spinetex = (Texture2D)sysr.ResourceManager.FindTexture(Sun.SpinesSprite);
                commands.AddCommand(spineShader.Shader, SpineSetup, Cleanup, Matrix4.Identity,
                    new RenderUserData() { Texture = spinetex }, sysr.StaticBillboards.VertexBuffer, PrimitiveTypes.TriangleList,
                    idx, 2 * Sun.Spines.Count, true, SortLayers.SUN, z + 1112f);
            }
        }
	}
}

