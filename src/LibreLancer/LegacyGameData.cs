using System;
using OpenTK;
using OpenTK.Graphics;
using Legacy = LibreLancer.Compatibility.GameData;
namespace LibreLancer
{
	public class LegacyGameData
	{
		Legacy.FreelancerData fldata;
		ResourceManager resource;
		public LegacyGameData (string path, ResourceManager resman)
		{
			resource = resman;
			Compatibility.VFS.Init (path);
			var flini = new Legacy.FreelancerIni ();
			fldata = new Legacy.FreelancerData (flini);
			fldata.LoadData ();
		}
		public void LoadInterfaceVms()
		{
			resource.LoadVms (Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/interface.generic.vms"));
		}
		public IDrawable GetMenuButton()
		{
			return resource.GetDrawable(Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/INTRO/OBJECTS/front_button.cmp"));
		}
		public GameData.StarSystem GetSystem(string id)
		{
			var legacy = fldata.Universe.FindSystem (id);
			var sys = new GameData.StarSystem ();
			sys.AmbientColor = legacy.AmbientColor ?? Color4.White;
			sys.Name = legacy.StridName;
			sys.BackgroundColor = legacy.SpaceColor ?? Color4.Black;
			if(legacy.BackgroundBasicStarsPath != null)
				sys.StarsBasic = resource.GetDrawable (legacy.BackgroundBasicStarsPath);
			if (legacy.BackgroundComplexStarsPath != null)
				sys.StarsComplex = resource.GetDrawable (legacy.BackgroundComplexStarsPath);
			if (legacy.BackgroundNebulaePath != null)
				sys.StarsNebula = resource.GetDrawable (legacy.BackgroundNebulaePath);
			if (legacy.LightSources != null) {
				foreach (var src in legacy.LightSources) {
					var lt = new RenderLight ();
					lt.Attenuation = src.Attenuation ?? new Vector3 (1, 0, 0);
					lt.Color = src.Color.Value;
					lt.Position = src.Pos.Value;
					lt.Range = src.Range.Value;
					lt.Rotation = src.Rotate ?? Vector3.Zero;
					sys.LightSources.Add (lt);
				}
			}
			foreach (var obj in legacy.Objects) {
				sys.Objects.Add (GetSystemObject (obj));
			}
			return sys;
		}
		public GameData.Ship GetShip(string nickname)
		{
			var legacy = fldata.Ships.GetShip (nickname);
			var ship = new GameData.Ship ();
			foreach (var matlib in legacy.MaterialLibraries)
				resource.LoadMat (matlib);
			ship.Drawable = resource.GetDrawable (legacy.DaArchetypeName);
			return ship;
		}
		public GameData.SystemObject GetSystemObject(Legacy.Universe.SystemObject o)
		{
			var drawable = resource.GetDrawable (o.Archetype.DaArchetypeName);
			var obj = new GameData.SystemObject ();
			obj.Position = o.Pos.Value;
			if (o.Rotate != null) {
				obj.Rotation = 
					Matrix4.CreateRotationX (MathHelper.DegreesToRadians (o.Rotate.Value.X)) *
					Matrix4.CreateRotationY (MathHelper.DegreesToRadians (o.Rotate.Value.Y)) *
					Matrix4.CreateRotationZ (MathHelper.DegreesToRadians (o.Rotate.Value.Z));
			}
			//Load archetype references
			foreach (var path in o.Archetype.TexturePaths)
				resource.LoadTxm (path);
			foreach (var path in o.Archetype.MaterialPaths)
				resource.LoadMat (path);
			//Construct archetype
			if (o.Archetype is Legacy.Solar.Sun) {
				obj.Archetype = new GameData.Archetypes.Sun ();
			} else {
				obj.Archetype = new GameData.Archetype ();
			}
			obj.Archetype.ArchetypeName = o.Archetype.GetType ().Name;
			Console.WriteLine (obj.Archetype.ArchetypeName);
			obj.Archetype.Drawable = drawable;
			return obj;
		}
	}
}

