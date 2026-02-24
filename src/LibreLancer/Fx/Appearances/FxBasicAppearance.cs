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
            var tt = ale.GetBoolean(AleProperty.BasicApp_TriTexture);
            if (tt)
            {
                QuadTexture = false;
            }
            MotionBlur = ale.GetBoolean(AleProperty.BasicApp_MotionBlur);
            Color = ale.GetColorAnimation(AleProperty.BasicApp_Color);
            Alpha = ale.GetFloatAnimation(AleProperty.BasicApp_Alpha);
            HToVAspect = ale.GetFloatAnimation(AleProperty.BasicApp_HToVAspect, false);
            Rotate = ale.GetFloatAnimation(AleProperty.BasicApp_Rotate, false);
            Texture = ale.GetString(AleProperty.BasicApp_TexName);
            UseCommonTexFrame = ale.GetBoolean(AleProperty.BasicApp_UseCommonTexFrame);
            TexFrame = ale.GetFloatAnimation(AleProperty.BasicApp_TexFrame, false);
            CommonTexFrame = ale.GetCurveAnimation(AleProperty.BasicApp_CommonTexFrame, false);
            FlipHorizontal = ale.GetBoolean(AleProperty.BasicApp_FlipTexU);
            FlipVertical = ale.GetBoolean(AleProperty.BasicApp_FlipTexV);
            Size = ale.GetFloatAnimation(AleProperty.BasicApp_Size);
            if (ale.TryGetParameter(AleProperty.BasicApp_BlendInfo, out var temp))
            {
                BlendInfo = BlendMap.Map((Tuple<uint, uint>) temp.Value);
            }
        }

        public FxBasicAppearance(string name) : base(name)
        {
            Size = new(1);
            Color = new(Color3f.White);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            if (QuadTexture)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_QuadTexture, true));
            }
            else
            {
                n.Parameters.Add(new(AleProperty.BasicApp_TriTexture, true));
            }
            if (MotionBlur)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_MotionBlur, true));
            }
            n.Parameters.Add(new(AleProperty.BasicApp_Color, Color));
            if (Alpha != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_Alpha, Alpha));
            }
            if (HToVAspect != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_HToVAspect, HToVAspect));
            }
            if (Size != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_Size, Size));
            }
            if (Rotate != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_Rotate, Rotate));
            }
            n.Parameters.Add(new(AleProperty.BasicApp_TexName, Texture));
            if (UseCommonTexFrame)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_UseCommonTexFrame, true));
            }
            if (TexFrame != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_TexFrame, TexFrame));
            }
            if (CommonTexFrame != null)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_CommonTexFrame, CommonTexFrame));
            }
            if (FlipHorizontal)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_FlipTexU, true));
            }
            if (FlipVertical)
            {
                n.Parameters.Add(new(AleProperty.BasicApp_FlipTexV, true));
            }
            if (BlendInfo != BlendMode.Normal)
            {
                var (src, dst) = BlendMode.Deconstruct(BlendInfo);
                n.Parameters.Add(new(AleProperty.BasicApp_BlendInfo, new Tuple<uint, uint>((uint)src, (uint)dst)));
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
