// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
    public class FxBasicAppearance : FxAppearance
    {
        public bool QuadTexture = true;
        public bool MotionBlur;
        public AlchemyColorAnimation Color;
        public AlchemyFloatAnimation Alpha;
        public AlchemyFloatAnimation HToVAspect;
        public AlchemyFloatAnimation Rotate;
        public AlchemyFloatAnimation Size;
        public ushort BlendInfo = BlendMode.Normal;
        public string Texture = "";
        public bool UseCommonTexFrame = false;
        public AlchemyFloatAnimation TexFrame;
        public AlchemyCurveAnimation CommonTexFrame;
        public bool FlipHorizontal = false;
        public bool FlipVertical = false;

        public FxBasicAppearance(AlchemyNode ale) : base(ale)
        {
            var tt = ale.GetBoolean("BasicApp_TriTexture");
            if (tt)
            {
                QuadTexture = false;
            }
            MotionBlur = ale.GetBoolean("BasicApp_MotionBlur");
            Color = ale.GetColorAnimation("BasicApp_Color");
            Alpha = ale.GetFloatAnimation("BasicApp_Alpha");
            HToVAspect = ale.GetFloatAnimation("BasicApp_HToVAspect", false);
            Rotate = ale.GetFloatAnimation("BasicApp_Rotate", false);
            Texture = ale.GetString("BasicApp_TexName");
            UseCommonTexFrame = ale.GetBoolean("BasicApp_UseCommonTexFrame");
            TexFrame = ale.GetFloatAnimation("BasicApp_TexFrame", false);
            CommonTexFrame = ale.GetCurveAnimation("BasicApp_CommonTexFrame", false);
            FlipHorizontal = ale.GetBoolean("BasicApp_FlipTexU");
            FlipVertical = ale.GetBoolean("BasicApp_FlipTexV");
            Size = ale.GetFloatAnimation("BasicApp_Size");
            if (ale.TryGetParameter("BasicApp_BlendInfo", out var temp))
            {
                BlendInfo = BlendMap.Map((Tuple<uint, uint>) temp.Value);
            }
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            if (QuadTexture)
            {
                n.Parameters.Add(new("BasicApp_QuadTexture", true));
            }
            else
            {
                n.Parameters.Add(new("BasicApp_TriTexture", true));
            }
            if (MotionBlur)
            {
                n.Parameters.Add(new("BasicApp_MotionBlur", true));
            }
            n.Parameters.Add(new("BasicApp_Color", Color));
            if (Alpha != null)
            {
                n.Parameters.Add(new("BasicApp_Alpha", Alpha));
            }
            if (HToVAspect != null)
            {
                n.Parameters.Add(new("BasicApp_HToVAspect", HToVAspect));
            }
            if (Rotate != null)
            {
                n.Parameters.Add(new("BasicApp_Rotate", Rotate));
            }
            n.Parameters.Add(new("BasicApp_Texture", Texture));
            if (UseCommonTexFrame)
            {
                n.Parameters.Add(new("BasicApp_UseCommonTexFrame", true));
            }
            if (TexFrame != null)
            {
                n.Parameters.Add(new("BasicApp_TexFrame", TexFrame));
            }
            if (CommonTexFrame != null)
            {
                n.Parameters.Add(new("BasicApp_CommonTexFrame", CommonTexFrame));
            }
            if (FlipHorizontal)
            {
                n.Parameters.Add(new("BasicApp_FlipTexU", true));
            }
            if (FlipVertical)
            {
                n.Parameters.Add(new("BasicApp_FlipTexV", true));
            }
            if (BlendInfo != BlendMode.Normal)
            {
                var (src, dst) = BlendMode.Deconstruct(BlendInfo);
                n.Parameters.Add(new("BasicApp_BlendInfo", new Tuple<uint, uint>((uint)src, (uint)dst)));
            }
            return n;
        }

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node, int nodeIdx,
            Matrix4x4 transform, float sparam)
        {
            var count = instance.Buffer.GetCount(nodeIdx);
            TextureHandler.Update(Texture, instance.Resources);
            var node_tr = GetAttachment(node, transform);

            for (int i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[nodeIdx, i];
                var time = particle.TimeAlive / particle.LifeSpan;
                var p = Vector3.Transform(Vector3.Transform(particle.Position, particle.Orientation), node_tr);
                var c = Color.GetValue(sparam, time);
                var a = Alpha?.GetValue(sparam, time) ?? 1f;
                instance.Pool.AddBasic(
                    TextureHandler,
                    p,
                    new Vector2(Size.GetValue(sparam, time)) * 2,
                    new Color4(c, a),
                    GetFrame((float) instance.GlobalTime, sparam, ref particle),
                    Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time)),
                    FlipHorizontal, FlipVertical
                );
            }
            instance.Pool.DrawBuffer(
                this,
                instance.Resources,
                transform,
                (instance.DrawIndex << 11) + nodeIdx
            );
        }

        public ParticleTexture TextureHandler = new ParticleTexture();

        protected float GetFrame(float globaltime, float sparam, ref Particle particle)
        {
            float frame = 0;
            if (UseCommonTexFrame)
            {
                frame = CommonTexFrame?.GetValue(sparam, globaltime) ?? 0;
            }
            else
            {
                frame = TexFrame?.GetValue(sparam, particle.TimeAlive / particle.LifeSpan) ?? 0;
            }

            return MathHelper.Clamp(frame, 0, 1);
        }
    }
}
