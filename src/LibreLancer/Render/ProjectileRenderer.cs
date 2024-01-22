// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.World;

namespace LibreLancer.Render
{
    //Renders vis_beam
    public class ProjectileRenderer : ObjectRenderer
    {
        public ProjectileManager Projectiles;
        Projectile[] toRender;
        int renderCount = 0;
        private BeamsBuffer beams;
        public ProjectileRenderer(ProjectileManager projs)
        {
            Projectiles = projs;
            toRender = new Projectile[Projectiles.Projectiles.Length];
        }
        public override void DepthPrepass(ICamera camera, RenderContext rstate)
        {
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
        {
            beams = sys.Beams;
            renderCount = 0;
            foreach(var i in Projectiles.Ids.GetAllocated()) {
                if (Projectiles.Projectiles[i].Alive)
                {
                    if (Projectiles.Projectiles[i].Effect != null)
                    {
                        Projectiles.Projectiles[i].Effect.Resources = sys.ResourceManager;
                        Projectiles.Projectiles[i].Effect.Pool = sys.FxPool;
                    }
                    toRender[renderCount++] = Projectiles.Projectiles[i];
                }
            }
            if (renderCount > 0) sys.AddObject(this);
            return true;
        }
        public override bool OutOfView(ICamera camera)
        {
            return false;
        }
        public override void Update(double time, Vector3 position, Matrix4x4 transform)
        {
            for (int i = 0; i < renderCount; i++)
            {
                var p = toRender[i];
                p.Effect?.Update(time, Matrix4x4.CreateTranslation(p.Position), 0);
            }
        }
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            for (int i = 0; i < renderCount; i++)
            {
                var p = toRender[i];
                var currDist = (p.Position - p.Start).Length();
                if(p.Data.Munition.ConstEffect_Spear != null)
                    beams.AddBeamSpear(p.Position, p.Normal.Normalized(), p.Data.Munition.ConstEffect_Spear, currDist);
                if(p.Data.Munition.ConstEffect_Bolt != null)
                    beams.AddBeamBolt(p.Position, p.Normal.Normalized(), p.Data.Munition.ConstEffect_Bolt, currDist);
                if (p.Effect != null) {
                    p.Effect.Draw(Matrix4x4.CreateTranslation(p.Position), 0);
                }
            }
        }
    }
}
