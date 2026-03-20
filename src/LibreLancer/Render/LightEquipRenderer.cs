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
        private const float BASE_SIZE = 10f;
        private Vector3 pos;
        private SystemRenderer? sys;
        private LightEquipment equip;
        public bool LightOn = true;
        private static Random rnd = new();

        public LightEquipRenderer(LightEquipment e)
        {
            equip = e;
            colorBulb = equip.Color;
            colorGlow = equip.GlowColor;
        }

        private static TextureShape bulbshape = new(ResourceManager.NullTextureName, "", new RectangleF(0, 0, 1, 1));
        private static Texture2D? bulbtex = null;
        private static TextureShape shineshape = new(ResourceManager.NullTextureName, "", new RectangleF(0, 0, 1, 1));
        private static Texture2D? shinetex = null;
        private static bool frameStart = false;

        public static void FrameStart()
        {
            frameStart = true;
        }

        private const float CULL_DISTANCE = 20000;
        private const float CULL = CULL_DISTANCE * CULL_DISTANCE;

        public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
        {
            if (frameStart)
            {
                if (sys!.ResourceManager.TryGetShape("bulb", out var newBulbShape))
                {
                    bulbshape = newBulbShape!.Value;
                    bulbtex = (Texture2D?) sys.ResourceManager.FindTexture(bulbshape.Texture);
                }
                else
                {
                    bulbtex = null;
                }

                if (sys.ResourceManager.TryGetShape("shine", out var shineShape))
                {
                    shineshape = shineShape!.Value;
                    shinetex = (Texture2D?) sys.ResourceManager.FindTexture(shineshape.Texture);
                }
                else
                {
                    shinetex = null;
                }

                frameStart = false;
            }

            if (bulbtex == null || shinetex == null)
                return;

            sys!.Billboards.Draw(
                shinetex,
                pos,
                new Vector2(equip.GlowSize) * 2,
                new Color4(colorGlow, 1f),
                new Vector2(shineshape.Dimensions.X, shineshape.Dimensions.Y),
                new Vector2(shineshape.Dimensions.X + shineshape.Dimensions.Width, shineshape.Dimensions.Y),
                new Vector2(shineshape.Dimensions.X, shineshape.Dimensions.Y + shineshape.Dimensions.Height),
                new Vector2(shineshape.Dimensions.X + shineshape.Dimensions.Width,
                    shineshape.Dimensions.Y + shineshape.Dimensions.Height),
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
                new Vector2(bulbshape.Dimensions.X + bulbshape.Dimensions.Width,
                    bulbshape.Dimensions.Y + bulbshape.Dimensions.Height),
                0,
                SortLayers.OBJECT,
                BlendMode.Additive
            );

        }

        private double timer = 0;
        private bool lt_on = true;
        private Color3f colorBulb;
        private Color3f colorGlow;

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
            var showLight = !equip.Animated || !lt_on;

            if (equip.EmitRange > 0 && showLight && camera.FrustumCheck(new BoundingSphere(pos, equip.EmitRange)))
            {
                // sys.PointLightDX(pos, equip.EmitRange, new Color4(equip.GlowColor, 1), equip.EmitAttenuation);
            }

            if (visible)
            {
                sys.AddObject(this);
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
