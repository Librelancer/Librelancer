// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
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
        public override void DepthPrepass(ICamera camera, RenderState rstate)
        {
        }
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            beams = sys.Beams;
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
        public override void Update(double time, Vector3 position, Matrix4x4 transform)
        {
        }
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            for (int i = 0; i < renderCount; i++)
            {
                var p = toRender[i];
                if(p.Data.Munition.ConstEffect_Spear != null)
                    beams.AddBeamSpear(p.Position, p.Normal.Normalized(), p.Data.Munition.ConstEffect_Spear);
                if(p.Data.Munition.ConstEffect_Bolt != null)
                    beams.AddBeamBolt(p.Position, p.Normal.Normalized(), p.Data.Munition.ConstEffect_Bolt);
            }
        }
    }
}
