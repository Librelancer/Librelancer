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
	public class FxOrientedAppearance : FxBasicAppearance
	{
		public AlchemyFloatAnimation Height;
		public AlchemyFloatAnimation Width;

		public FxOrientedAppearance(AlchemyNode ale) : base(ale) 
		{
			AleParameter temp;
			if (ale.TryGetParameter("OrientedApp_Height", out temp))
			{
				Height = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter("OrientedApp_Width", out temp))
			{
				Width = (AlchemyFloatAnimation)temp.Value;
			}
		}

		public override void Draw(ref Particle particle, float globaltime, ParticleEffect effect, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			var time = particle.TimeAlive / particle.LifeSpan;
			var node_tr = GetTranslation(effect, transform, sparam, time);

			var p = node_tr.Transform(particle.Position);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
			HandleTexture(res, globaltime, sparam, ref particle, out tex, out tl, out tr, out bl, out br);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);

			var p2 = node_tr.Transform(particle.Position + particle.Normal);
			//var n = (p - p2).Normalized();
			var n = Vector3.UnitZ;

			billboards.DrawPerspective(
				tex,
				p,
				new Vector2(Width.GetValue(sparam, time), Height.GetValue(sparam, time)),
				new Color4(c, a),
				tl,
				tr,
				bl,
				br,
				n,
				Rotate == null ? 0f : Rotate.GetValue(sparam, time),
				SortLayers.OBJECT,
				BlendInfo
			);
		}
	}
}

