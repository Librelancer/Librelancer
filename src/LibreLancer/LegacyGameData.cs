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
using LibreLancer.Fx;
using LibreLancer.Utf.Ale;
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
			resource.AddPreload(
				fldata.EffectShapes.Files.Select(txmfile => Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + txmfile))
			);
			foreach (var shape in fldata.EffectShapes.Shapes)
			{
				var s = new TextureShape()
				{
					Texture = shape.Value.TextureName,
					Nickname = shape.Value.ShapeName,
					Dimensions = shape.Value.Dimensions
				};
				resource.AddShape(shape.Key, s);
			}
		}
		public void PopulateCursors()
		{
			resource.LoadTxm(
				Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + fldata.Mouse.TxmFile)
			);
			foreach (var lc in fldata.Mouse.Cursors)
			{
				var shape = fldata.Mouse.Shapes.Where((arg) => arg.Name.Equals(lc.Shape, StringComparison.OrdinalIgnoreCase)).First();
				var cur = new Cursor();
				cur.Nickname = lc.Nickname;
				cur.Scale = lc.Scale;
				cur.Spin = lc.Spin;
				cur.Color = lc.Color;
				cur.Hotspot = lc.Hotspot;
				cur.Dimensions = shape.Dimensions;
				cur.Texture = fldata.Mouse.TextureName;
				resource.AddCursor(cur, cur.Nickname);
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
		public void LoadHardcodedFiles()
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
			sys.FarClip = legacy.SpaceFarClip ?? 20000f;
			if(legacy.BackgroundBasicStarsPath != null)
				sys.StarsBasic = resource.GetDrawable (legacy.BackgroundBasicStarsPath);
			if (legacy.BackgroundComplexStarsPath != null)
				sys.StarsComplex = resource.GetDrawable (legacy.BackgroundComplexStarsPath);
			if (legacy.BackgroundNebulaePath != null)
				sys.StarsNebula = resource.GetDrawable (legacy.BackgroundNebulaePath);
			if (legacy.LightSources != null) {
				foreach (var src in legacy.LightSources) {
					var lt = new RenderLight ();
					lt.Color = src.Color.Value;
					lt.Position = src.Pos.Value;
					lt.Range = src.Range.Value;
					lt.Direction = src.Direction ?? Vector3.UnitZ;
					lt.Kind = ((src.Type ?? Legacy.Universe.LightType.Point) == Legacy.Universe.LightType.Point) ? LightKind.Point : LightKind.Directional;
					lt.Attenuation = new Vector4(src.Attenuation ?? Vector3.UnitY, 0);
					if (src.AttenCurve != null)
					{
						lt.Attenuation = ApproximateCurve.GetCubicFunction(
							fldata.Graphs.FindFloatGraph(src.AttenCurve).Points.ToArray()
						);
					}
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

						var qx = Quaternion.FromEulerAngles(
							MathHelper.DegreesToRadians(r.X),
							MathHelper.DegreesToRadians(r.Y),
							MathHelper.DegreesToRadians(r.Z)
						);
						z.RotationMatrix = Matrix4.CreateFromQuaternion(qx);
						z.RotationAngles = new Vector3(
							MathHelper.DegreesToRadians(r.X),
							MathHelper.DegreesToRadians(r.Y),
							MathHelper.DegreesToRadians(r.Z)
						);
					}
					else
					{
						z.RotationMatrix = Matrix4.Identity;
						z.RotationAngles = Vector3.Zero;
					}
				switch (zne.Shape.Value) {
				case Legacy.Universe.ZoneShape.ELLIPSOID:
					z.Shape = new GameData.ZoneEllipsoid (z,
						zne.Size.Value.X,
						zne.Size.Value.Y,
						zne.Size.Value.Z
					);
					break;
				case Legacy.Universe.ZoneShape.SPHERE:
					z.Shape = new GameData.ZoneSphere (z,
						zne.Size.Value.X
					);
					break;
				case Legacy.Universe.ZoneShape.BOX:
					z.Shape = new GameData.ZoneBox (z,
						zne.Size.Value.X,
						zne.Size.Value.Y,
						zne.Size.Value.Z
					);
					break;
				case Legacy.Universe.ZoneShape.CYLINDER:
					z.Shape = new GameData.ZoneCylinder (z,
						zne.Size.Value.X,
						zne.Size.Value.Y
					);
					break;
				case Legacy.Universe.ZoneShape.RING:
					z.Shape = new GameData.ZoneRing(z,
						zne.Size.Value.X,
						zne.Size.Value.Y,
						zne.Size.Value.Z
					);
					break;
				default:
						Console.WriteLine (zne.Nickname);
						Console.WriteLine (zne.Shape.Value);
						throw new NotImplementedException ();
				}
				sys.Zones.Add (z);
			}
			if (legacy.Asteroids != null)
			{
				foreach (var ast in legacy.Asteroids)
				{
					sys.AsteroidFields.Add (GetAsteroidField (sys, ast));
				}
			}

			if (legacy.Nebulae != null)
			{
				foreach (var nbl in legacy.Nebulae)
				{
					sys.Nebulae.Add(GetNebula(sys, nbl));
				}
			}
			return sys;
		}
		GameData.AsteroidField GetAsteroidField(GameData.StarSystem sys, Legacy.Universe.AsteroidField ast)
		{
			var a = new GameData.AsteroidField();
			a.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == ast.ZoneName.ToLower()).First();
			Legacy.Universe.TexturePanels panels = null;
			if (ast.TexturePanels != null) {
				panels = new Legacy.Universe.TexturePanels (ast.TexturePanels.File);
				foreach (var txmfile in panels.Files)
					resource.LoadTxm (Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + txmfile));
			}
			if (ast.Band != null) {
				a.Band = new GameData.AsteroidBand ();
				a.Band.RenderParts = ast.Band.RenderParts.Value;
				a.Band.Height = ast.Band.Height.Value;
				a.Band.Shape = panels.Shapes [ast.Band.Shape].TextureName;
				a.Band.Fade = new Vector4 (ast.Band.Fade [0], ast.Band.Fade [1], ast.Band.Fade [2], ast.Band.Fade [3]);
				var cs = ast.Band.ColorShift ?? Vector3.One;
				a.Band.ColorShift = new Color4 (cs.X, cs.Y, cs.Z, 1f);
				a.Band.TextureAspect = ast.Band.TextureAspect ?? 1f;
				a.Band.OffsetDistance = ast.Band.OffsetDist ?? 0f;
			}
			a.Cube = new List<GameData.StaticAsteroid> ();
			a.CubeRotation = new GameData.AsteroidCubeRotation();
			a.CubeRotation.AxisX = ast.Cube_RotationX ?? GameData.AsteroidCubeRotation.Default_AxisX;
			a.CubeRotation.AxisY = ast.Cube_RotationY ?? GameData.AsteroidCubeRotation.Default_AxisY;
			a.CubeRotation.AxisZ = ast.Cube_RotationZ ?? GameData.AsteroidCubeRotation.Default_AxisZ;
			a.CubeSize = ast.Field.CubeSize.Value;
			a.SetFillDist(ast.Field.FillDist.Value);
			a.EmptyCubeFrequency = ast.Field.EmptyCubeFrequency ?? 0f;
			foreach (var c in ast.Cube) {
				var sta = new GameData.StaticAsteroid () {
					Rotation = c.Rotation,
					Position = c.Position,
					Info = c.Info
				};
				sta.RotationMatrix =
					Matrix4.CreateRotationX (MathHelper.DegreesToRadians (c.Rotation.X)) *
					Matrix4.CreateRotationY (MathHelper.DegreesToRadians (c.Rotation.Y)) *
					Matrix4.CreateRotationZ (MathHelper.DegreesToRadians (c.Rotation.Z));
				var n = c.Name;
				var arch = fldata.Asteroids.FindAsteroid (c.Name);
				resource.LoadMat (Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + arch.MaterialLibrary));
				sta.Drawable = resource.GetDrawable (Compatibility.VFS.GetPath (fldata.Freelancer.DataPath + arch.DaArchetype));
				a.Cube.Add (sta);
			}
			a.ExclusionZones = new List<GameData.ExclusionZone>();
			if (ast.ExclusionZones != null)
			{
				foreach (var excz in ast.ExclusionZones)
				{
					var e = new GameData.ExclusionZone();
					e.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == excz.Exclusion.Nickname.ToLower()).First();
					//e.FogFar = excz.FogFar ?? n.FogRange.Y;
					if (excz.ZoneShellPath != null)
					{
						var pth = Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + excz.ZoneShellPath);
						e.Shell = resource.GetDrawable(pth);
						e.ShellTint = excz.Tint ?? Color3f.White;
						e.ShellScalar = excz.ShellScalar ?? 1f;
						e.ShellMaxAlpha = excz.MaxAlpha ?? 1f;
					}
					a.ExclusionZones.Add(e);
				}
			}
			a.BillboardCount = ast.AsteroidBillboards == null ? -1 : ast.AsteroidBillboards.Count.Value;
			if (a.BillboardCount != -1) {
				a.BillboardDistance = ast.AsteroidBillboards.StartDist.Value;
				a.BillboardFadePercentage = ast.AsteroidBillboards.FadeDistPercent.Value;
				Compatibility.GameData.Universe.TextureShape sh = null;
				if (panels != null)
					sh = panels.Shapes [ast.AsteroidBillboards.Shape];
				else
					sh = new Legacy.Universe.TextureShape (ast.AsteroidBillboards.Shape, ast.AsteroidBillboards.Shape, new RectangleF (0, 0, 1, 1));
				a.BillboardShape = new TextureShape () {
					Texture = sh.TextureName,
					Dimensions = sh.Dimensions,
					Nickname = ast.AsteroidBillboards.Shape
				};
				a.BillboardSize = ast.AsteroidBillboards.Size.Value;
				a.BillboardTint = new Color3f (ast.AsteroidBillboards.ColorShift ?? Vector3.One);
			}
			return a;
		}
		public GameData.Nebula GetNebula(GameData.StarSystem sys, Legacy.Universe.Nebula nbl)
		{
			var n = new GameData.Nebula();
			n.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == nbl.ZoneName.ToLower()).First();
			var panels = new Legacy.Universe.TexturePanels(nbl.TexturePanels.File);
			foreach (var txmfile in panels.Files)
				resource.LoadTxm(Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + txmfile));
			n.ExteriorFill = nbl.ExteriorFillShape;
			n.ExteriorColor = nbl.ExteriorColor ?? Color4.White;
			n.FogColor = nbl.FogColor ?? Color4.Black;
			n.FogEnabled = (nbl.FogEnabled ?? 0) != 0;
			n.FogRange = new Vector2(nbl.FogNear ?? 0, nbl.FogDistance ?? 0);
			n.SunBurnthroughScale = n.SunBurnthroughIntensity = 1f;
			if (nbl.NebulaLights != null && nbl.NebulaLights.Count > 0)
			{
				n.AmbientColor = nbl.NebulaLights[0].Ambient;
				n.SunBurnthroughScale = nbl.NebulaLights[0].SunBurnthroughScaler ?? 1f;
				n.SunBurnthroughIntensity = nbl.NebulaLights[0].SunBurnthroughIntensity ?? 1f;
			}
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
				n.InteriorCloudMaxAlpha = nbl.CloudsPuffMaxAlpha ?? 1f;
				n.InteriorCloudFadeDistance = nbl.CloudsNearFadeDistance.Value;
				n.InteriorCloudDrift = nbl.CloudsPuffDrift.Value;
			}
			if (nbl.ExteriorShape != null)
			{
				n.HasExteriorBits = true;
				GameData.CloudShape[] shapes = new GameData.CloudShape[nbl.ExteriorShape.Count];
				for (int i = 0; i < shapes.Length; i++)
				{
					var name = nbl.ExteriorShape[i];
					shapes[i].Texture = panels.Shapes[name].TextureName;
					shapes[i].Dimensions = panels.Shapes[name].Dimensions;
				}
				n.ExteriorCloudShapes = new WeightedRandomCollection<GameData.CloudShape>(
					shapes,
					nbl.ExteriorShapeWeights.ToArray()
				);
				n.ExteriorMinBits = nbl.ExteriorMinBits.Value;
				n.ExteriorMaxBits = nbl.ExteriorMaxBits.Value;
				n.ExteriorBitRadius = nbl.ExteriorBitRadius.Value;
				n.ExteriorBitRandomVariation = nbl.ExteriorBitRadiusRandomVariation ?? 0;
				n.ExteriorMoveBitPercent = nbl.ExteriorMoveBitPercent ?? 0;
			}
			if (nbl.ExclusionZones != null)
			{
				n.ExclusionZones = new List<GameData.ExclusionZone>();
				foreach (var excz in nbl.ExclusionZones)
				{
					var e = new GameData.ExclusionZone();
					e.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == excz.Exclusion.Nickname.ToLower()).First();
					e.FogFar = excz.FogFar ?? n.FogRange.Y;
					if (excz.ZoneShellPath != null)
					{
						var pth = Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + excz.ZoneShellPath);
						e.Shell = resource.GetDrawable(pth);
						e.ShellTint = excz.Tint ?? Color3f.White;
						e.ShellScalar = excz.ShellScalar ?? 1f;
						e.ShellMaxAlpha = excz.MaxAlpha ?? 1f;
					}
					n.ExclusionZones.Add(e);
				}
			}
			if (nbl.BackgroundLightningDuration != null)
			{
				n.BackgroundLightning = true;
				n.BackgroundLightningDuration = nbl.BackgroundLightningDuration.Value;
				n.BackgroundLightningColor = nbl.BackgroundLightningColor.Value;
				n.BackgroundLightningGap = nbl.BackgroundLightningGap.Value;
			}
			if (nbl.DynamicLightningDuration != null)
			{
				n.DynamicLightning = true;
				n.DynamicLightningGap = nbl.DynamicLightningGap.Value;
				n.DynamicLightningColor = nbl.DynamicLightningColor.Value;
				n.DynamicLightningDuration = nbl.DynamicLightningDuration.Value;
			}
			if (nbl.CloudsLightningDuration != null)
			{
				n.CloudLightning = true;
				n.CloudLightningDuration = nbl.CloudsLightningDuration.Value;
				n.CloudLightningColor = nbl.CloudsLightningColor.Value;
				n.CloudLightningGap = nbl.CloudsLightningGap.Value;
				n.CloudLightningIntensity = nbl.CloudsLightningIntensity.Value;
			}
			return n;
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
				sun.GlowColorInner = starglow.InnerColor;
				sun.GlowColorOuter = starglow.OuterColor;
				sun.GlowScale = starglow.Scale;
				//center
				if (star.StarCenter != null)
				{
					var centerglow = fldata.Stars.FindStarGlow(star.StarCenter);
					sun.CenterSprite = centerglow.Shape;
					sun.CenterColorInner = centerglow.InnerColor;
					sun.CenterColorOuter = centerglow.OuterColor;
					sun.CenterScale = centerglow.Scale;
				}
				if (star.Spines != null)
				{
					var spines = fldata.Stars.FindSpines(star.Spines);
					sun.SpinesSprite = spines.Shape;
					sun.SpinesScale = spines.RadiusScale;
					sun.Spines = new List<GameData.Spine>(spines.Items.Count);
					foreach (var sp in spines.Items)
						sun.Spines.Add(new GameData.Spine(sp.LengthScale, sp.WidthScale, sp.InnerColor, sp.OuterColor, sp.Alpha));
				}
				obj.Archetype = sun;
			} else {
				obj.Archetype = new GameData.Archetype ();
			}
			obj.Archetype.ArchetypeName = o.Archetype.GetType ().Name;
			obj.Archetype.Drawable = drawable;
			var ld = o.Loadout;
			if (ld != null)
			{
				foreach (var key in ld.Equip.Keys)
				{
					var val = ld.Equip[key];
					if (val == null)
						continue;
					GameData.Items.Equipment equip = null;
					if (val is Legacy.Equipment.Light)
					{
						equip = GetLight((Legacy.Equipment.Light)val);
					}
					else if (val is Legacy.Equipment.InternalFx)
					{
						var eq = new GameData.Items.AnimationEquipment();
						eq.Animation = ((Legacy.Equipment.InternalFx)val).UseAnimation;
						equip = eq;
					}
					if (val is Legacy.Equipment.AttachedFx)
					{
						equip = GetAttachedFx((Legacy.Equipment.AttachedFx)val);
					}
					if (equip != null)
					{
						if (key.StartsWith("__noHardpoint", StringComparison.Ordinal))
							obj.LoadoutNoHardpoint.Add(equip);
						else
							obj.Loadout.Add(key, equip);
					}
				}
			}
			return obj;
		}

		public ParticleEffect GetEffect(string effectName)
		{
			var effect = fldata.Effects.FindEffect(effectName);
			var visfx = fldata.Effects.FindVisEffect(effect.VisEffect);
			foreach (var texfile in visfx.Textures)
			{
				var path = Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + texfile);
				if (path.EndsWith(".txm"))
					resource.LoadTxm(path);
				else if (path.EndsWith(".mat"))
					resource.LoadMat(path);
			}
			var alepath = Compatibility.VFS.GetPath(fldata.Freelancer.DataPath + visfx.AlchemyPath);
			var ale = new AleFile(alepath);
			var lib = new ParticleLibrary(resource, ale);
			return lib.FindEffect((uint)visfx.EffectCrc);
		}

		GameData.Items.EffectEquipment GetAttachedFx(Legacy.Equipment.AttachedFx fx)
		{
			var equip = new GameData.Items.EffectEquipment();
			equip.Particles = GetEffect(fx.Particles);
			return equip;
		}

		GameData.Items.LightEquipment GetLight(Legacy.Equipment.Light lt)
		{
			var equip = new GameData.Items.LightEquipment();
			equip.Color = lt.Color ?? Color3f.White;
			equip.MinColor = lt.MinColor ?? Color3f.Black;
			equip.GlowColor = lt.GlowColor ?? equip.Color;
			equip.BulbSize = lt.BulbSize ?? 1f;
			equip.GlowSize = lt.GlowSize ?? 1f;
			if (lt.AvgDelay != null)
			{
				equip.Animated = true;
				equip.AvgDelay = lt.AvgDelay.Value;
				equip.BlinkDuration = lt.BlinkDuration.Value;
			}
			return equip;
		}
	}
}

