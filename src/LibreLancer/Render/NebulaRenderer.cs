using System;
using LibreLancer.GameData.Universe;
using LibreLancer.GameData;
namespace LibreLancer
{
	public class NebulaRenderer : IDisposable
	{
		Nebula nebula;
		Camera camera;
		ResourceCache cache;

		TexturePanels panels;
		public NebulaRenderer (Nebula n, ResourceCache cache, Camera c, FreelancerData data)
		{
			nebula = n;
			this.cache = cache;
			camera = c;

			panels = new TexturePanels (data.Freelancer.DataPath + nebula.TexturePanels.File);
		}

		public void Update(TimeSpan elapsed)
		{

		}

		public void Draw(Lighting lights)
		{

		}

		void RenderExterior()
		{
			if (!nebula.Zone.Shape.HasValue)
				return;
			if (nebula.Zone.Shape.Value != ZoneShape.ELLIPSOID
			   && nebula.Zone.Shape.Value != ZoneShape.SPHERE)
				return;
			
		}

		public void Dispose()
		{

		}
	}
}

