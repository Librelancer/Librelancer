/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Utf.Ale;
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
				if ((bool)temp.Value)
					throw new NotImplementedException("BasicApp_TriTexture");
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

		public override void Draw(ref Particle particle, ParticleEffect effect, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			var time = particle.TimeAlive / particle.LifeSpan;
			var tr = GetTranslation(effect, transform, sparam, time);

			var p = tr.Transform(particle.Position);
			Texture2D tex;
			var shape = GetTexture(res, out tex);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);

			billboards.Draw(
				tex,
				p,
				new Vector2(Size.GetValue(sparam, time)) * 2,
				new Color4(c, a),
				new Vector2(FlipHorizontal ? 1 : 0, FlipVertical ? 1 : 0),
				new Vector2(FlipHorizontal ? 0 : 1, FlipVertical ? 1 : 0),
				new Vector2(FlipHorizontal ? 1 : 0, FlipVertical ? 0 : 1),
				new Vector2(FlipHorizontal ? 0 : 1, FlipVertical ? 0 : 1),
				Rotate.GetValue(sparam, time),
				SortLayers.OBJECT,
				BlendInfo
			);
		}

		TextureShape _tex;
		Texture2D _tex2D;
		protected TextureShape GetTexture(ResourceManager res, out Texture2D tex2d)
		{
			if (_tex == null && _tex2D != null)
			{
				if (_tex2D == null || _tex2D.IsDisposed)
					_tex2D = (Texture2D)res.FindTexture(Texture);
				tex2d = _tex2D;
				return null;
			}
			if (_tex == null)
			{
				if (res.TryGetShape(Texture, out _tex))
					_tex2D = (Texture2D)res.FindTexture(_tex.Texture);
				else
				{
					_tex2D = (Texture2D)res.FindTexture(Texture);
				}
			}
			if (_tex2D == null || _tex2D.IsDisposed)
				_tex2D = (Texture2D)res.FindTexture(_tex.Texture);
			tex2d = _tex2D;
			return _tex;
		}


	}
}

