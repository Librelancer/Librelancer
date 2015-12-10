using System;
using OpenTK;
using LibreLancer.Utf.Cmp;

using LibreLancer.GameData.Universe;
namespace LibreLancer
{
	public class ModelRenderer : ObjectRenderer
	{
		public ModelFile Model { get; private set; }
		public CmpFile Cmp { get; private set; }

		public ModelRenderer (Camera camera, Matrix4 world, bool useObjectPosAndRotate, SystemObject spaceObject)
			: base(camera, world, useObjectPosAndRotate, spaceObject)
		{
			IDrawable archetype = spaceObject.Archetype.DaArchetype;
			if (archetype is ModelFile) {
				Model = archetype as ModelFile;
				if (Model != null && Model.Levels.ContainsKey (0)) {
					Model.Initialize ();
				}
			} else if (archetype is CmpFile) {
				Cmp = archetype as CmpFile;
				Cmp.Initialize ();
			}
		}

		public override void Update(TimeSpan elapsed)
		{
			if (Model != null) Model.Update(camera);
			else if (Cmp != null) Cmp.Update(camera);
			base.Update(elapsed);
		}

		public override void Draw(Lighting lights)
		{
			if (Model != null) {
				if (Model.Levels.ContainsKey (0)) {
					var bbox = Model.Levels [0].BoundingBox;
					bbox.Max = Vector3.Transform (bbox.Max, World);
					bbox.Min = Vector3.Transform (bbox.Min, World);
					if (camera.Frustum.Intersects (bbox))
						Model.Draw (World, lights);
				}
			}
			else if (Cmp != null) {
				foreach (ModelFile model in Cmp.Models.Values)
					if (model.Levels.ContainsKey(0)) {
						var bbox = model.Levels [0].BoundingBox;
						bbox.Max = Vector3.Transform (bbox.Max, World);
						bbox.Min = Vector3.Transform (bbox.Min, World);
						if (camera.Frustum.Intersects (bbox)) {
							Cmp.Draw (World, lights);
							break;
						}
					}
			}
		}

		public override void Dispose ()
		{
			
		}
	}
}

