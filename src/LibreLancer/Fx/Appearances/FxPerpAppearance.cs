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
	public class FxPerpAppearance : FxOrientedAppearance
	{
		public FxPerpAppearance(AlchemyNode ale) : base(ale) { }

		public override void Draw(ref Particle particle, ParticleEffect effect, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			var time = particle.TimeAlive / particle.LifeSpan;
			var tr = GetTranslation(effect, transform, sparam, time);

			var p = tr.Transform(particle.Position);
			Texture2D tex;
			var shape = GetTexture(res, out tex);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);

			billboards.DrawPerspective(
				tex,
				p,
				new Vector2(Size.GetValue(sparam, time)) * 2,
				new Color4(c, a),
				new Vector2(FlipHorizontal ? 1 : 0, FlipVertical ? 1 : 0),
				new Vector2(FlipHorizontal ? 0 : 1, FlipVertical ? 1 : 0),
				new Vector2(FlipHorizontal ? 1 : 0, FlipVertical ? 0 : 1),
				new Vector2(FlipHorizontal ? 0 : 1, FlipVertical ? 0 : 1),
				particle.Normal.Normalized(),
				Rotate.GetValue(sparam, time),
				SortLayers.OBJECT,
				BlendInfo
			);
		}
	}
}

