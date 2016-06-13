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
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using LibreLancer.GameData;
namespace LibreLancer
{
	public class ModelRenderer : ObjectRenderer
	{
		public ModelFile Model { get; private set; }
		public CmpFile Cmp { get; private set; }
		public SphFile Sph { get; private set; }

		public ModelRenderer (ICamera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject,ResourceManager cache)
			: base(camera, world, useObjectPosAndRotate, spaceObject)
		{
			IDrawable archetype = spaceObject.Archetype.Drawable;
			if (archetype is ModelFile) {
				Model = archetype as ModelFile;
				if (Model != null && Model.Levels.ContainsKey (0)) {
					Model.Initialize (cache);
				}
			} else if (archetype is CmpFile) {
				Cmp = archetype as CmpFile;
				Cmp.Initialize (cache);
			} else if (archetype is SphFile) {
				Sph = archetype as SphFile;
				Sph.Initialize (cache);
			}
		}

		public override void Update(TimeSpan elapsed)
		{
			if (Model != null)
				Model.Update (camera, elapsed);
			else if (Cmp != null)
				Cmp.Update (camera, elapsed);
			else if (Sph != null)
				Sph.Update (camera, elapsed);
			base.Update(elapsed);
		}

		public override void Draw(RenderState rstate, Lighting lights)
		{
			if (Model != null) {
				if (Model.Levels.ContainsKey (0)) {
					var bsphere = new BoundingSphere(
						VectorMath.Transform(Model.Levels[0].Center, World),
						Model.Levels[0].Radius
					);
					if (camera.Frustum.Intersects (bsphere))
						Model.Draw (rstate, World, lights);
				}
			} else if (Cmp != null) {
				foreach (ModelFile model in Cmp.Models.Values)
					if (model.Levels.ContainsKey (0)) {
						var bsphere = new BoundingSphere(
							VectorMath.Transform(model.Levels[0].Center, World),
							model.Levels[0].Radius
						);
						if (camera.Frustum.Intersects (bsphere)) {
							Cmp.Draw (rstate, World, lights);
							break;
						}
					}
			} else if (Sph != null) {
				Sph.Draw (rstate, World, lights); //Need to cull this
			}
		}

		public override void Dispose ()
		{
			
		}
	}
}

