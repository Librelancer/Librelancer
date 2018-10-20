// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
using LibreLancer.Utf.Mat;
namespace LibreLancer.Fx
{
	public class FxBasicAppearance : FxAppearance
	{
		public bool QuadTexture;
		public bool MotionBlur;
		public AlchemyColorAnimation Color;
		public AlchemyFloatAnimation Alpha;
		public AlchemyFloatAnimation HToVAspect;
		public AlchemyFloatAnimation Rotate;
		public AlchemyFloatAnimation Size;
		public BlendMode BlendInfo = BlendMode.Normal;
		public string Texture;
		public bool UseCommonAnimation = false;
		public AlchemyFloatAnimation Animation;
		public AlchemyCurveAnimation CommonAnimation;
		public bool FlipHorizontal = false;
		public bool FlipVertical = false;

		public FxBasicAppearance (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter ("BasicApp_QuadTexture", out temp)) {
				QuadTexture = (bool)temp.Value;
			}
			if (ale.TryGetParameter("BasicApp_TriTexture", out temp))
			{
				if ((bool)temp.Value) {
					FLLog.Warning ("ALE", "BasicApp_TriTexture not implemented");
				}
			}
			if (ale.TryGetParameter ("BasicApp_MotionBlur", out temp)) {
				MotionBlur = (bool)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Color", out temp)) {
				Color = (AlchemyColorAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Alpha", out temp)) {
				Alpha = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_HtoVAspect", out temp)) {
				HToVAspect = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_Rotate", out temp)) {
				Rotate = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_TexName", out temp)) {
				Texture = (string)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_UseCommonTexFrame", out temp)) {
				UseCommonAnimation = (bool)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_TexFrame", out temp)) {
				Animation = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_CommonTexFrame", out temp)) {
				CommonAnimation = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("BasicApp_FlipTexU", out temp)) {
				FlipHorizontal = (bool)temp.Value;
			}
			if (ale.TryGetParameter("BasicApp_FlipTexV", out temp)) {
				FlipVertical = (bool)temp.Value;
			}
			if (ale.TryGetParameter("BasicApp_Size", out temp)) {
				Size = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter("BasicApp_BlendInfo", out temp)) {
				BlendInfo = BlendMap.Map((Tuple<uint, uint>)temp.Value);
			}
		}

		public override void Draw(ref Particle particle, float lasttime, float globaltime, NodeReference reference, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			var time = particle.TimeAlive / particle.LifeSpan;
            var node_tr = GetAttachment(reference, transform);

            Vector3 deltap;
            Quaternion deltaq;
            if(DoTransform(reference,sparam,lasttime,globaltime,out deltap, out deltaq)) {
                particle.Position += deltap;
                particle.Orientation *= deltaq;
            }
			var p = node_tr.Transform(particle.Orientation * particle.Position);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
			HandleTexture(res, sparam, globaltime, ref particle, out tex, out tl, out tr, out bl, out br);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);
			billboards.Draw(
				tex,
				p,
				new Vector2(Size.GetValue(sparam, time)) * 2,
				new Color4(c, a),
				tl,
				tr,
				bl,
				br,
                Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time)),
				SortLayers.OBJECT,
				BlendInfo
			);
		}

		TextureShape _tex;
		TexFrameAnimation _frame;
		Texture2D _tex2D;

		protected void HandleTexture(
			ResourceManager res,
			float globaltime,
			float sparam, 
			ref Particle particle, 
			out Texture2D tex2d, 
			out Vector2 tl, 
			out Vector2 tr, 
			out Vector2 bl, 
			out Vector2 br
		)
		{
			//Initial texcoords
			tl = new Vector2(0, 0);
			tr = new Vector2(1, 0);
			bl = new Vector2(0, 1);
			br = new Vector2(1, 1);
			if (Texture == null) {
				tex2d = res.NullTexture;
				return;
			}
			//Get the Texture2D
			if (_tex == null && _frame == null && _tex2D != null)
			{
				if (_tex2D == null || _tex2D.IsDisposed)
					_tex2D = res.FindTexture(Texture) as Texture2D;
				tex2d = _tex2D;
			}
			else if (_tex == null)
			{
				if (res.TryGetShape(Texture, out _tex))
					_tex2D = (Texture2D)res.FindTexture(_tex.Texture);
				else if (res.TryGetFrameAnimation(Texture, out _frame))
				{
					_tex2D = res.FindTexture(Texture + "_0") as Texture2D;
				}
				else
				{
					_tex2D = res.FindTexture(Texture) as Texture2D;
				}
			}
			if (_tex2D == null || _tex2D.IsDisposed)
			{
				if (_frame == null)
					_tex2D = res.FindTexture(_tex == null ? Texture : _tex.Texture) as Texture2D;
				else
					_tex2D = res.FindTexture(Texture + "_0") as Texture2D;
			}
			tex2d = _tex2D;
			if (tex2d == null) tex2d = (Texture2D)res.FindTexture(ResourceManager.WhiteTextureName);
			//Shape?
			if (_tex != null)
			{
				tl = new Vector2(_tex.Dimensions.X, _tex.Dimensions.Y);
				tr = new Vector2(_tex.Dimensions.X + _tex.Dimensions.Width, _tex.Dimensions.Y);
				bl = new Vector2(_tex.Dimensions.X, _tex.Dimensions.Y + _tex.Dimensions.Height);
				br = new Vector2(_tex.Dimensions.X + _tex.Dimensions.Width, _tex.Dimensions.Y + _tex.Dimensions.Height);
			}
			else if (_frame != null)
			{
				float frame = 0;
				if (UseCommonAnimation)
				{
					frame = CommonAnimation.GetValue(sparam, globaltime);
				}
				else
				{
					frame = Animation.GetValue(sparam, particle.TimeAlive / particle.LifeSpan);
				}
				frame = MathHelper.Clamp(frame, 0, 1);
				var frameNo = (int)Math.Floor(frame / _frame.FrameCount);
				var rect = _frame.Frames[frameNo];
				tl = new Vector2(rect.X, rect.Y);
				tr = new Vector2(rect.X + rect.Width, rect.Y);
				bl = new Vector2(rect.X, rect.Y + rect.Height);
				br = new Vector2(rect.X + rect.Width, rect.Y + rect.Height);
			}

			//Flip
			if (FlipHorizontal)
			{
				tl.X = 1 - tl.X;
				tr.X = 1 - tl.X;
				bl.X = 1 - bl.X;
				br.X = 1 - br.X;
			}
			if (FlipVertical)
			{
				tl.Y = 1 - tl.Y;
				tr.Y = 1 - tr.Y;
				bl.Y = 1 - bl.Y;
				br.Y = 1 - br.Y;
			}
		}


	}
}

