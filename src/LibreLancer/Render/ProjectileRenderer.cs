// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    //Renders vis_beam
    public class ProjectileRenderer : ObjectRenderer
    {
        public ProjectileManager Projectiles;
        Projectile[] toRender;
        int renderCount = 0;
        Billboards billboards;
        ResourceManager res;
        public ProjectileRenderer(ProjectileManager projs)
        {
            Projectiles = projs;
            toRender = new Projectile[Projectiles.Projectiles.Length];
        }
        public override void DepthPrepass(ICamera camera, RenderState rstate)
        {
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            billboards = sys.Billboards;
            res = sys.ResourceManager;
            renderCount = 0;
            for(int i = 0; i < Projectiles.Projectiles.Length; i++) {
                if (Projectiles.Projectiles[i].Alive)
                    toRender[renderCount++] = Projectiles.Projectiles[i];
            }
            if (renderCount > 0) sys.AddObject(this);
            return true;
        }
        public override bool OutOfView(ICamera camera)
        {
            return false;
        }
        public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
        {
        }
        static void CoordsFromTexture(string tex, out Vector2 tl, out Vector2 tr, out Vector2 bl, out Vector2 br)
        {
            switch (tex)
            {
                case "ball":
                    tl = new Vector2(0.5f, 0.5f);
                    tr = new Vector2(1, 0.5f);
                    bl = new Vector2(0.5f, 0f);
                    br = new Vector2(1f, 0f);
                    break;
                case "star":
                    tl = new Vector2(0.5f, 1f);
                    tr = new Vector2(1f, 1f);
                    bl = new Vector2(0.5f, 0.5f);
                    br = new Vector2(1f, 0.5f);
                    break;
                case "wide":
                    tl = new Vector2(0f,1f);
                    tr = new Vector2(0.5f, 1f);
                    bl = new Vector2(0f, 0.5f);
                    br = new Vector2(0.5f, 0.5f);
                    break;
                case "thin":
                    tl = new Vector2(0f, 0.5f);
                    tr = new Vector2(0.5f, 0.5f);
                    bl = new Vector2(0, 0f);
                    br = new Vector2(0.5f, 0f);
                    break;
                default:
                    throw new Exception("bad texture");
            }
        }
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            var code_beam = (Texture2D)res.FindTexture("code_beam");
            code_beam.SetWrapModeS(WrapMode.ClampToEdge);
            code_beam.SetWrapModeT(WrapMode.ClampToEdge);
            for(int i = 0; i < renderCount; i++) {
                var p = toRender[i];
                if(p.Data.Munition.ConstEffect_Beam != null)
                {
                    var beam = p.Data.Munition.ConstEffect_Beam;
                    Vector2 tl, tr, bl, br;
                    CoordsFromTexture(beam.HeadTexture, out tl, out tr, out bl, out br);
                    billboards.Draw(
                        code_beam,
                        p.Position,
                        new Vector2(beam.HeadWidth, beam.HeadWidth),
                        beam.CoreColor,
                        tl,tr,bl,br,
                        0,
                        SortLayers.OBJECT,
                        BlendMode.Additive
                    );
                    CoordsFromTexture(beam.TrailTexture, out tl, out tr, out bl, out br);

                }
                else if(p.Data.Munition.ConstEffect_Bolt != null)
                {
                    //bolt
                    var bolt = p.Data.Munition.ConstEffect_Bolt;
                    Vector2 tl, tr, bl, br;
                    CoordsFromTexture(bolt.HeadTexture, out tl, out tr, out bl, out br);
                    billboards.Draw(
                        code_beam,
                        p.Position,
                        new Vector2(bolt.HeadWidth, bolt.HeadWidth),
                        bolt.CoreColor,
                        tl,tr,bl,br,
                        0,
                        SortLayers.OBJECT,
                        BlendMode.Additive
                    );
                }
            }
        }
    }
}
