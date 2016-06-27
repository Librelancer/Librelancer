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
using System.Collections.Generic;
using System.Linq;
using Legacy = LibreLancer.Compatibility.GameData;
namespace LibreLancer
{
	public class LegacyGameData
	{
		Legacy.FreelancerData fldata;
		ResourceManager resource;
		List<GameData.IntroScene> IntroScenes;
		public LegacyGameData (string path, ResourceManager resman)
		{
			resource = resman;
			Compatibility.VFS.Init (path);
			var flini = new Legacy.FreelancerIni ();
			fldata = new Legacy.FreelancerData (flini);

		}
		public List<string> GetIntroMovies()
		{
			var movies = new List<string>();
			foreach (var file in fldata.Freelancer.StartupMovies)
			{
				movies.Add(Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + file));
			}
			return movies;
		}
		public void LoadData()
		{
			fldata.LoadData();
			IntroScenes = new List<GameData.IntroScene>();
			foreach(var b in fldata.Universe.Bases) {
				if (b.Nickname.StartsWith("intro", StringComparison.InvariantCultureIgnoreCase))
				{
					foreach (var room in b.Rooms)
					{
						if (room.Nickname == b.StartRoom)
						{
							var isc = new GameData.IntroScene();
							var path = Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + room.SceneScript);
							isc.ThnName = System.IO.Path.GetFileName(path);
							isc.Music = room.Music;
							isc.Script = new ThnScript(path);
							IntroScenes.Add(isc);
						}
					}
				}
			}
		}
		public string GetMusicPath(string id)
		{
			var audio = fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
			return Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + audio.File);
		}
		public GameData.IntroScene GetIntroScene()
		{
			//var rand = new Random();
			//return IntroScenes[rand.Next(0, IntroScenes.Count)];
			return IntroScenes[0];
		}
		public void LoadInterfaceVms()
		{
			resource.LoadVms (Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/interface.generic.vms"));
		}
		public IDrawable GetMenuButton()
		{
			return resource.GetDrawable(Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/INTRO/OBJECTS/front_button.cmp"));
		}
		public Texture2D GetSplashScreen()
		{
			if (!resource.TextureExists("__startupscreen_1280.tga"))
			{
				resource.AddTexture(
					"__startupscreen_1280.tga",
					Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen_1280.tga")
				);
			}
			return (Texture2D)resource.FindTexture("__startupscreen_1280.tga");
		}
		public Texture2D GetFreelancerLogo()
		{
			if (!resource.TextureExists("__freelancerlogo.tga"))
			{
				resource.AddTexture(
					"__freelancerlogo.tga",
					Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/front_freelancerlogo.tga")
				);
			}
			return (Texture2D)resource.FindTexture("__freelancerlogo.tga");
		}
		public GameData.StarSystem GetSystem(string id)
		{
			var legacy = fldata.Universe.FindSystem (id);
			foreach (var txmfile in fldata.Stars.TextureFiles)
				resource.LoadTxm(Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + txmfile));
			var sys = new GameData.StarSystem ();
			sys.AmbientColor = legacy.AmbientColor ?? Color4.White;
			sys.Name = legacy.StridName;
			sys.Id = legacy.Nickname;
			sys.BackgroundColor = legacy.SpaceColor ?? Color4.Black;
			sys.MusicSpace = legacy.MusicSpace;
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
			if(legacy.Zones != null)
			foreach (var zne in legacy.Zones) {
				var z = new GameData.Zone ();
				z.Nickname = zne.Nickname;
				z.EdgeFraction = zne.EdgeFraction ?? 0.25f;
				z.Position = zne.Pos.Value;
				if (zne.Rotate != null)
				{
					var r = zne.Rotate.Value;
					z.Rotation = 
							Matrix4.CreateRotationX(MathHelper.DegreesToRadians(r.X)) * 
							Matrix4.CreateRotationY(MathHelper.DegreesToRadians(r.Y)) * 
							Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(r.Z));
				}
				else
					z.Rotation = Matrix4.Identity;
				switch (zne.Shape.Value) {
				case Legacy.Universe.ZoneShape.ELLIPSOID:
					z.Shape = new GameData.ZoneEllipsoid (
						zne.Size.Value.X,
						zne.Size.Value.Y,
						zne.Size.Value.Z
					);
					break;
				case Legacy.Universe.ZoneShape.SPHERE:
					z.Shape = new GameData.ZoneSphere(
						zne.Size.Value.X
					);
					break;
				}
				sys.Zones.Add (z);
			}
			if (legacy.Nebulae != null)
			{
				foreach (var nbl in legacy.Nebulae)
				{
					var n = new GameData.Nebula();
					n.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == nbl.ZoneName.ToLower()).First();
					var panels = new Legacy.Universe.TexturePanels(nbl.TexturePanels.File);
					foreach(var txmfile in panels.Files)
						resource.LoadTxm(Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + txmfile));
					n.ExteriorFill = nbl.ExteriorFillShape;
					n.ExteriorColor = nbl.ExteriorColor ?? Color4.White;
					n.FogColor = nbl.FogColor ?? Color4.Black;
					if (nbl.CloudsPuffShape != null)
					{
						n.HasInteriorClouds = true;
						GameData.CloudShape[] shapes = new GameData.CloudShape[nbl.CloudsPuffShape.Count];
						for (int i = 0; i < shapes.Length; i++)
						{
							var name = nbl.CloudsPuffShape[i];
							shapes[i].Texture = panels.Shapes[name].TextureName;
							shapes[i].Dimensions = panels.Shapes[name].Dimensions;
						}
						n.InteriorCloudShapes = new WeightedRandomCollection<GameData.CloudShape>(
							shapes,
							nbl.CloudsPuffWeights.ToArray()
						);
						n.InteriorCloudColorA = nbl.CloudsPuffColorA.Value;
						n.InteriorCloudColorB = nbl.CloudsPuffColorB.Value;
						n.InteriorCloudRadius = nbl.CloudsPuffRadius.Value;
						n.InteriorCloudCount = nbl.CloudsPuffCount.Value;
						n.InteriorCloudMaxDistance = nbl.CloudsMaxDistance.Value;
						n.InteriorCloudMaxAlpha = nbl.CloudsPuffMaxAlpha.Value;
						n.InteriorCloudFadeDistance = nbl.CloudsNearFadeDistance.Value;
						n.InteriorCloudDrift = nbl.CloudsPuffDrift.Value;
					}
					sys.Nebulae.Add(n);
			}
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
			obj.Nickname = o.Nickname;
			obj.DisplayName = o.IdsName;
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
				var sun = new GameData.Archetypes.Sun();
				var star = fldata.Stars.FindStar(o.Star);
				//general
				sun.Radius = star.Radius.Value;
				//glow
				var starglow = fldata.Stars.FindStarGlow(star.StarGlow);
				sun.GlowSprite = starglow.Shape;
				sun.GlowColorInner = new Color4(starglow.InnerColor, 1);
				sun.GlowColorOuter = new Color4(starglow.OuterColor, 1);
				//center
				if (star.StarCenter != null)
				{
					var centerglow = fldata.Stars.FindStarGlow(star.StarCenter);
					sun.CenterSprite = centerglow.Shape;
					sun.CenterColorInner = new Color4(centerglow.InnerColor, 1);
					sun.CenterColorOuter = new Color4(centerglow.OuterColor, 1);
				}
				obj.Archetype = sun;
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

