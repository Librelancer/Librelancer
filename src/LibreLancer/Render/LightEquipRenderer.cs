// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Graphics;
using LibreLancer.Resources;

namespace LibreLancer.Render
{
    public class LightEquipRenderer : ObjectRenderer
    {
        const float BASE_SIZE = 10f;
        Vector3 pos;
        SystemRenderer sys;
        LightEquipment equip;
        public bool LightOn = true;
        static Random rnd = new Random();
        public LightEquipRenderer(LightEquipment e)
        {
            equip = e;
            colorBulb = equip.Color;
            colorGlow = equip.GlowColor;
        }
        static TextureShape bulbshape = new (ResourceManager.NullTextureName, "", new RectangleF(0, 0, 1, 1));
        static Texture2D bulbtex = null;
        static TextureShape shineshape = new (ResourceManager.NullTextureName, "", new RectangleF(0, 0, 1, 1));
        static Texture2D shinetex = null;
        static bool frameStart = false;
        public static void FrameStart()
        {
            frameStart = true;
        }
        const float CULL_DISTANCE = 20000;
        const float CULL = CULL_DISTANCE * CULL_DISTANCE;
        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            if (frameStart)
            {
                sys.ResourceManager.TryGetShape("bulb", out bulbshape);
                bulbtex = (Texture2D)sys.ResourceManager.FindTexture(bulbshape.Texture);
                sys.ResourceManager.TryGetShape("shine", out shineshape);
                shinetex = (Texture2D)sys.ResourceManager.FindTexture(shineshape.Texture);
                frameStart = false;
            }
            if (bulbtex == null || shinetex == null)
                return;
            sys.Billboards.Draw(
                shinetex,
                pos,
                new Vector2(equip.GlowSize) * 2,
                new Color4(colorGlow, 1f),
                new Vector2(shineshape.Dimensions.X, shineshape.Dimensions.Y),
                new Vector2(shineshape.Dimensions.X + shineshape.Dimensions.Width, shineshape.Dimensions.Y),
                new Vector2(shineshape.Dimensions.X, shineshape.Dimensions.Y + shineshape.Dimensions.Height),
                new Vector2(shineshape.Dimensions.X + shineshape.Dimensions.Width, shineshape.Dimensions.Y + shineshape.Dimensions.Height),
                0,
                SortLayers.OBJECT,
                BlendMode.Additive
            );
            sys.Billboards.Draw(
                bulbtex,
                pos,
                new Vector2(equip.BulbSize) * 2,
                new Color4(colorBulb, 1),
                new Vector2(bulbshape.Dimensions.X, bulbshape.Dimensions.Y),
                new Vector2(bulbshape.Dimensions.X + bulbshape.Dimensions.Width, bulbshape.Dimensions.Y),
                new Vector2(bulbshape.Dimensions.X, bulbshape.Dimensions.Y + bulbshape.Dimensions.Height),
                new Vector2(bulbshape.Dimensions.X + bulbshape.Dimensions.Width, bulbshape.Dimensions.Y + bulbshape.Dimensions.Height),
                0,
                SortLayers.OBJECT,
                BlendMode.Additive
            );

        }

        double timer = 0;
        bool lt_on = true;
        Color3f colorBulb;
        Color3f colorGlow;
        public override void Update(double time, Vector3 position, Matrix4x4 transform)
        {
            if (!LightOn || sys == null)
                return;
            pos = position;
            if (equip.Animated)
            {
                timer -= time;
                if (timer < 0)
                {
                    if (lt_on)
                    {
                        timer = equip.BlinkDuration;
                        colorBulb = equip.Color;
                        colorGlow = equip.GlowColor;
                    }
                    else
                    {
                        timer = equip.AvgDelay + rnd.NextFloat(-(equip.AvgDelay / 2f), +(equip.AvgDelay / 2f));
                        colorBulb = equip.MinColor;
                        colorGlow = equip.MinColor;
                    }
                    lt_on = !lt_on;
                }
            }
        }

        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
        {
            var visible = (
                !forceCull &&
                LightOn &&
                Vector3.DistanceSquared(camera.Position, pos) < CULL &&
                camera.FrustumCheck(new BoundingSphere(pos, equip.BulbSize * 3))
            );
            this.sys = sys;
            bool showLight = !equip.Animated || !lt_on;
            if (equip.EmitRange > 0 && showLight && camera.FrustumCheck(new BoundingSphere(pos, equip.EmitRange)))
            {
                //sys.PointLightDX(pos, equip.EmitRange, new Color4(equip.GlowColor, 1), equip.EmitAttenuation);
            }
            if (visible) {
                sys.AddObject(this);
                return true;
            } else {
                return false;
            }

        }
    }
}

