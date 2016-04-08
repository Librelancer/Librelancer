using System;
using OpenTK;
using LibreLancer.GameData;
using LibreLancer.Vertices;
namespace LibreLancer
{
	public class NebulaRenderer : IDisposable
	{
		Nebula nebula;
		ICamera camera;

		public NebulaRenderer (Nebula n, ICamera c)
		{
			nebula = n;
			camera = c;
		}

		public void Update(TimeSpan elapsed)
		{

		}

		public void Draw(Lighting lights)
		{

		}

		void RenderExterior()
		{
				
		}

		public void Dispose()
		{

		}
	}
}

