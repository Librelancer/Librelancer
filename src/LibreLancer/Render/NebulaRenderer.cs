using System;
using LibreLancer.GameData.Universe;
namespace LibreLancer
{
	public class NebulaRenderer
	{
		Nebula nebula;
		public NebulaRenderer (Nebula n)
		{
			nebula = n;

		}

		void RenderExterior()
		{
			if (!nebula.Zone.Shape.HasValue)
				return;
			if (nebula.Zone.Shape.Value != ZoneShape.ELLIPSOID
			   && nebula.Zone.Shape.Value != ZoneShape.SPHERE)
				return;
			
		}
	}
}

