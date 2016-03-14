using System;
using OpenTK;
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

		public ModelRenderer (DebugCamera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject,ResourceManager cache)
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
					var bbox = Model.Levels [0].BoundingBox;
					bbox.Max = Vector3.Transform (bbox.Max, World);
					bbox.Min = Vector3.Transform (bbox.Min, World);
					if (camera.Frustum.Intersects (bbox))
						Model.Draw (rstate, World, lights);
				}
			} else if (Cmp != null) {
				foreach (ModelFile model in Cmp.Models.Values)
					if (model.Levels.ContainsKey (0)) {
						var bbox = model.Levels [0].BoundingBox;
						bbox.Max = Vector3.Transform (bbox.Max, World);
						bbox.Min = Vector3.Transform (bbox.Min, World);
						if (camera.Frustum.Intersects (bbox)) {
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

