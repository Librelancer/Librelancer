// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Fx;
using LibreLancer.Utf.Ale;
namespace LibreLancer
{
	public class GameDataManager
	{
        public Data.FreelancerData Ini => fldata;
        Data.FreelancerData fldata;
		ResourceManager resource;
		List<GameData.IntroScene> IntroScenes;

		public GameDataManager(string path, ResourceManager resman)
		{
			resource = resman;
			Data.VFS.Init(path);
			var flini = new Data.FreelancerIni();
			fldata = new Data.FreelancerData(flini);
		}
        public string DataVersion => fldata.DataVersion;
        public string GetInterfaceXml(string id)
        {
            if (fldata.Freelancer.XInterfacePath == null)
            {
                using (var reader = new StreamReader(typeof(GameDataManager).Assembly.GetManifestResourceStream("LibreLancer.Interface.Default." + id + ".xml")))
                {
                    return reader.ReadToEnd();
                }
            } else {
                using(var reader = new StreamReader(fldata.Freelancer.XInterfacePath + id + ".xml")) {
                    return reader.ReadToEnd();
                }
            }
        }
		public string ResolveDataPath(string input)
		{
			return Data.VFS.GetPath(fldata.Freelancer.DataPath + input);
		}

		public Dictionary<string, string> GetBaseNavbarIcons()
		{
			return fldata.BaseNavBar.Navbar;
		}

		public List<string> GetIntroMovies()
		{
			var movies = new List<string>();
			foreach (var file in fldata.Freelancer.StartupMovies)
			{
				movies.Add(Data.VFS.GetPath(fldata.Freelancer.DataPath + file));
			}
			return movies;
		}
		public List<Data.RichFont> GetRichFonts()
		{
			return fldata.RichFonts.Fonts;
		}
		public GameData.Base GetBase(string id)
		{
			var inibase = fldata.Universe.FindBase(id);
			var mbase = fldata.MBases.FindBase(id);
			var b = new GameData.Base();
            b.System = inibase.System;
			foreach (var room in inibase.Rooms)
			{
				var nr = new GameData.BaseRoom();
				var mroom = mbase.FindRoom(room.Nickname);
				nr.Music = room.Music;
				nr.ThnPaths = new List<string>();
				nr.PlayerShipPlacement = room.PlayerShipPlacement;
				foreach (var path in room.SceneScripts)
					nr.ThnPaths.Add(Data.VFS.GetPath(fldata.Freelancer.DataPath + path));
                if (room.LandingScript != null)
                    nr.LandScript = Data.VFS.GetPath(fldata.Freelancer.DataPath + room.LandingScript);
                if (room.StartScript != null)
                    nr.StartScript = Data.VFS.GetPath(fldata.Freelancer.DataPath + room.StartScript);
                if (room.LaunchingScript != null)
                    nr.LaunchScript = Data.VFS.GetPath(fldata.Freelancer.DataPath + room.LaunchingScript);
                if (room.GoodscartScript != null)
                    nr.GoodscartScript = Data.VFS.GetPath(fldata.Freelancer.DataPath + room.GoodscartScript);
				nr.Hotspots = new List<GameData.BaseHotspot>();
				foreach (var hp in room.Hotspots)
					nr.Hotspots.Add(new GameData.BaseHotspot()
					{
						Name = hp.Name,
						Behavior = hp.Behavior,
						Room = hp.RoomSwitch,
						SetVirtualRoom = hp.VirtualRoom
					});
				nr.Nickname = room.Nickname;
				if (room.Nickname == inibase.StartRoom) b.StartRoom = nr;
				nr.Camera = room.Camera;
				nr.Npcs = new List<GameData.BaseNpc>();
				if (mroom != null)
				{
					foreach (var npc in mroom.NPCs)
					{
						/*var newnpc = new GameData.BaseNpc();
						newnpc.StandingPlace = npc.StandMarker;
						var gfnpc = mbase.FindNpc(npc.Npc);
						newnpc.HeadMesh = fldata.Bodyparts.FindBodypart(gfnpc.Head).MeshPath;
						newnpc.BodyMesh = fldata.Bodyparts.FindBodypart(gfnpc.Body).MeshPath;
						newnpc.LeftHandMesh = fldata.Bodyparts.FindBodypart(gfnpc.LeftHand).MeshPath;
						newnpc.RightHandMesh = fldata.Bodyparts.FindBodypart(gfnpc.RightHand).MeshPath;
						nr.Npcs.Add(newnpc);*/
					}
				}
				b.Rooms.Add(nr);
			}
			return b;
		}
		public void LoadData()
		{
			fldata.LoadData();
			IntroScenes = new List<GameData.IntroScene>();
			foreach (var b in fldata.Universe.Bases)
			{
				if (b.Nickname.StartsWith("intro", StringComparison.InvariantCultureIgnoreCase))
				{
					foreach (var room in b.Rooms)
					{
						if (room.Nickname == b.StartRoom)
						{
							var isc = new GameData.IntroScene();
							isc.Scripts = new List<ThnScript>();
							foreach (var p in room.SceneScripts)
							{
								var path = Data.VFS.GetPath(fldata.Freelancer.DataPath + p);
								isc.Scripts.Add(new ThnScript(path));
							}
							isc.Music = room.Music;
							IntroScenes.Add(isc);
						} 
					}
				}
			}
			if (resource != null)
			{
				resource.AddPreload(
					fldata.EffectShapes.Files.Select(txmfile => Data.VFS.GetPath(fldata.Freelancer.DataPath + txmfile))
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
		}
		public void PopulateCursors()
		{
			resource.LoadResourceFile(
				Data.VFS.GetPath(fldata.Freelancer.DataPath + fldata.Mouse.TxmFile)
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
        public Data.Audio.AudioEntry GetAudioEntry(string id)
        {
            return fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
        }
        public string GetAudioPath(string id)
		{
			var audio = fldata.Audio.Entries.Where((arg) => arg.Nickname.ToLowerInvariant() == id.ToLowerInvariant()).First();
			return Data.VFS.GetPath(fldata.Freelancer.DataPath + audio.File);
		}
        public string GetVoicePath(string id)
        {
            return Data.VFS.GetPath(fldata.Freelancer.DataPath + "\\AUDIO\\" + id + ".utf");
        }
        public Infocards.Infocard GetInfocard(int id)
		{
			return Infocards.RDLParse.Parse(fldata.Infocards.GetXmlResource(id));
		}
		public string GetString(int id)
		{
			return fldata.Infocards.GetStringResource(id);
		}
		public GameData.IntroScene GetIntroScene()
		{
			var rand = new Random();
			return IntroScenes[rand.Next(0, IntroScenes.Count)];
		}
#if DEBUG
		public GameData.IntroScene GetIntroSceneSpecific(int i)
		{
			if (i > IntroScenes.Count)
				return null;
			return IntroScenes[i];
		}
#endif
		public void LoadHardcodedFiles()
		{
			resource.LoadResourceFile (Data.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/interface.generic.vms"));
		}
		public IDrawable GetMenuButton()
		{
			return resource.GetDrawable(Data.VFS.GetPath (fldata.Freelancer.DataPath + "INTERFACE/INTRO/OBJECTS/front_button.cmp"));
		}
		public Texture2D GetSplashScreen()
		{
			if (!resource.TextureExists("__startupscreen_1280.tga"))
			{
				resource.AddTexture(
					"__startupscreen_1280.tga",
					Data.VFS.GetPath(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/startupscreen_1280.tga")
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
					Data.VFS.GetPath(fldata.Freelancer.DataPath + "INTERFACE/INTRO/IMAGES/front_freelancerlogo.tga")
				);
			}
			return (Texture2D)resource.FindTexture("__freelancerlogo.tga");
		}
		public IEnumerable<Maneuver> GetManeuvers()
		{
			foreach (var m in fldata.Hud.Maneuvers)
			{
				yield return new Maneuver()
				{
					Action = m.Action,
					InfocardA = fldata.Infocards.GetStringResource(m.InfocardA),
					InfocardB = fldata.Infocards.GetStringResource(m.InfocardB),
					ActiveModel = m.ActiveModel,
					InactiveModel = m.InactiveModel
				};
			}
		}
		public bool SystemExists(string id)
		{
			return fldata.Universe.FindSystem(id) != null;
		}
		public IEnumerable<string> ListSystems()
		{
			foreach (var sys in fldata.Universe.Systems.Where((x) => !x.MultiUniverse)) yield return sys.Nickname;
		}
		public IEnumerable<string> ListBases()
		{
			foreach (var bse in fldata.Universe.Bases) yield return bse.Nickname;
		}

        public IEnumerator<object> FillSystem(string id, GameData.StarSystem sys)
        {
            var inisys = fldata.Universe.FindSystem(id);
            if (fldata.Stars != null)
            {
                foreach (var txmfile in fldata.Stars.TextureFiles)
                    resource.LoadResourceFile(Data.VFS.GetPath(fldata.Freelancer.DataPath + txmfile));
            }
            yield return null;
            sys.AmbientColor = inisys.AmbientColor ?? Color4.White;
            sys.Name = GetString(inisys.IdsName);
            sys.Id = inisys.Nickname;
            sys.BackgroundColor = inisys.SpaceColor ?? Color4.Black;
            sys.MusicSpace = inisys.MusicSpace;
            sys.FarClip = inisys.SpaceFarClip ?? 20000f;
            if (inisys.BackgroundBasicStarsPath != null)
            {
                try
                {
                    sys.StarsBasic = resource.GetDrawable(inisys.BackgroundBasicStarsPath);
                }
                catch (Exception)
                {
                    sys.StarsBasic = null;
                    FLLog.Error("System", "Failed to load starsphere " + inisys.BackgroundBasicStarsPath);
                }
            }
            yield return null;
            if (inisys.BackgroundComplexStarsPath != null)
            {
                //try {
                sys.StarsComplex = resource.GetDrawable(inisys.BackgroundComplexStarsPath);
                //} catch (Exception) {
                //sys.StarsComplex = null;
                //FLLog.Error ("System", "Failed to load starsphere " + Data.BackgroundComplexStarsPath);
                //}

            }

            if (inisys.BackgroundNebulaePath != null)
            {
                //try {
                sys.StarsNebula = resource.GetDrawable(inisys.BackgroundNebulaePath);
                //} catch (Exception) {
                //sys.StarsNebula = null;
                //FLLog.Error ("System", "Failed to load starsphere " + Data.BackgroundNebulaePath);
                //}
            }
            yield return null;
            if (inisys.LightSources != null)
            {
                foreach (var src in inisys.LightSources)
                {
                    var lt = new RenderLight();
                    var srcCol = src.Color.Value;
                    lt.Color = new Color3f(srcCol.R, srcCol.G, srcCol.B);
                    lt.Position = src.Pos.Value;
                    lt.Range = src.Range.Value;
                    lt.Direction = src.Direction ?? new Vector3(0, 0, 1);
                    lt.Kind = ((src.Type ?? Data.Universe.LightType.Point) == Data.Universe.LightType.Point) ? LightKind.Point : LightKind.Directional;
                    lt.Attenuation = src.Attenuation ?? Vector3.UnitY;
                    if (src.AttenCurve != null)
                    {
                        lt.Kind = LightKind.PointAttenCurve;
                        lt.Attenuation = ApproximateCurve.GetQuadraticFunction(
                            fldata.Graphs.FindFloatGraph(src.AttenCurve).Points.ToArray()
                        );
                    }
                    sys.LightSources.Add(lt);
                }
            }
            foreach (var obj in inisys.Objects)
            {
                sys.Objects.Add(GetSystemObject(obj));
                yield return null;
            }
            if (inisys.Zones != null)
                foreach (var zne in inisys.Zones)
                {
                    var z = new GameData.Zone();
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
                    switch (zne.Shape.Value)
                    {
                        case Data.Universe.ZoneShape.ELLIPSOID:
                            z.Shape = new GameData.ZoneEllipsoid(z,
                                zne.Size.Value.X,
                                zne.Size.Value.Y,
                                zne.Size.Value.Z
                            );
                            break;
                        case Data.Universe.ZoneShape.SPHERE:
                            z.Shape = new GameData.ZoneSphere(z,
                                zne.Size.Value.X
                            );
                            break;
                        case Data.Universe.ZoneShape.BOX:
                            z.Shape = new GameData.ZoneBox(z,
                                zne.Size.Value.X,
                                zne.Size.Value.Y,
                                zne.Size.Value.Z
                            );
                            break;
                        case Data.Universe.ZoneShape.CYLINDER:
                            z.Shape = new GameData.ZoneCylinder(z,
                                zne.Size.Value.X,
                                zne.Size.Value.Y
                            );
                            break;
                        case Data.Universe.ZoneShape.RING:
                            z.Shape = new GameData.ZoneRing(z,
                                zne.Size.Value.X,
                                zne.Size.Value.Y,
                                zne.Size.Value.Z
                            );
                            break;
                        default:
                            Console.WriteLine(zne.Nickname);
                            Console.WriteLine(zne.Shape.Value);
                            throw new NotImplementedException();
                    }
                    sys.Zones.Add(z);
                }
            if (inisys.Asteroids != null)
            {
                foreach (var ast in inisys.Asteroids)
                {
                    sys.AsteroidFields.Add(GetAsteroidField(sys, ast));
                    yield return null;
                }
            }

            if (inisys.Nebulae != null)
            {
                foreach (var nbl in inisys.Nebulae)
                {
                    sys.Nebulae.Add(GetNebula(sys, nbl));
                    yield return null;
                }
            }
        }
        public GameData.StarSystem GetSystem(string id)
		{
            var sys = new GameData.StarSystem();
            var iterator = FillSystem(id, sys);
            while (iterator.MoveNext()) {  }
            return sys;
		}
		GameData.AsteroidField GetAsteroidField(GameData.StarSystem sys, Data.Universe.AsteroidField ast)
		{
			var a = new GameData.AsteroidField();
			a.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == ast.ZoneName.ToLower()).First();
			Data.Universe.TexturePanels panels = null;
			if (ast.TexturePanels != null) {
				foreach (var f in ast.TexturePanels.Files) {
					panels = new Data.Universe.TexturePanels (f);
					foreach (var txmfile in panels.Files)
						resource.LoadResourceFile (Data.VFS.GetPath (fldata.Freelancer.DataPath + txmfile));
				}
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
			a.CubeSize = ast.Field.CubeSize ?? 100; //HACK: Actually handle null cube correctly
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
				resource.LoadResourceFile (Data.VFS.GetPath (fldata.Freelancer.DataPath + arch.MaterialLibrary));
				sta.Drawable = resource.GetDrawable (Data.VFS.GetPath (fldata.Freelancer.DataPath + arch.DaArchetype));
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
						var pth = Data.VFS.GetPath(fldata.Freelancer.DataPath + excz.ZoneShellPath);
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
				Data.Universe.TextureShape sh = null;
                if (panels != null)
                {
                    if (!panels.Shapes.TryGetValue(ast.AsteroidBillboards.Shape, out sh))
                    {
                        a.BillboardCount = -1;
                        FLLog.Error("Asteroids", "Field " + ast.ZoneName + " can't find billboard shape " + ast.AsteroidBillboards.Shape);
                        return a;
                    }
                    else
                    {
                        sh = panels.Shapes[ast.AsteroidBillboards.Shape];
                    }
                }
				else
					sh = new Data.Universe.TextureShape (ast.AsteroidBillboards.Shape, ast.AsteroidBillboards.Shape, new RectangleF (0, 0, 1, 1));
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
		public GameData.Nebula GetNebula(GameData.StarSystem sys, Data.Universe.Nebula nbl)
		{
			var n = new GameData.Nebula();
			n.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == nbl.ZoneName.ToLower()).First();
			var panels = new Data.Universe.TexturePanels(nbl.TexturePanels.Files[0]);
			foreach (var txmfile in panels.Files)
				resource.LoadResourceFile(Data.VFS.GetPath(fldata.Freelancer.DataPath + txmfile));
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
					if (!panels.Shapes.ContainsKey(name))
					{
						FLLog.Error("Nebula", "Shape " + name + " does not exist in " + nbl.TexturePanels.Files[0]);
						shapes[i].Texture = ResourceManager.NullTextureName;
						shapes[i].Dimensions = new RectangleF(0, 0, 1, 1);
					}
					else
					{
						shapes[i].Texture = panels.Shapes[name].TextureName;
						shapes[i].Dimensions = panels.Shapes[name].Dimensions;
					}
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
					if (!panels.Shapes.ContainsKey(name))
					{
						FLLog.Error("Nebula", "Shape " + name + " does not exist in " + nbl.TexturePanels.Files[0]);
						shapes[i].Texture = ResourceManager.NullTextureName;
						shapes[i].Dimensions = new RectangleF(0, 0, 1, 1);
					}
					else
					{
						shapes[i].Texture = panels.Shapes[name].TextureName;
						shapes[i].Dimensions = panels.Shapes[name].Dimensions;
					}
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
					if (excz.Exclusion == null) continue;
					var e = new GameData.ExclusionZone();
					e.Zone = sys.Zones.Where((z) => z.Nickname.ToLower() == excz.Exclusion.Nickname.ToLower()).First();
					e.FogFar = excz.FogFar ?? n.FogRange.Y;
					if (excz.ZoneShellPath != null)
					{
						var pth = Data.VFS.GetPath(fldata.Freelancer.DataPath + excz.ZoneShellPath);
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
        public GameData.Ship GetShip(int crc)
        {
            return GetShip(fldata.Ships.Ships.FirstOrDefault((x) => CrcTool.FLModelCrc(x.Nickname) == crc));
        }
        public GameData.Ship GetShip(string nickname)
		{
            return GetShip(fldata.Ships.GetShip(nickname));
		}
        GameData.Ship GetShip(Data.Ships.Ship Data)
        {
            var ship = new GameData.Ship();
            foreach (var matlib in Data.MaterialLibraries)
                resource.LoadResourceFile(ResolveDataPath(matlib));
            ship.Drawable = resource.GetDrawable(ResolveDataPath(Data.DaArchetypeName));
            ship.Mass = Data.Mass;
            ship.AngularDrag = Data.AngularDrag;
            ship.RotationInertia = Data.RotationInertia;
            ship.SteeringTorque = Data.SteeringTorque;
            ship.CruiseSpeed = 300;
            ship.StrafeForce = Data.StrafeForce;
            ship.ChaseOffset = Data.CameraOffset;
            return ship;
        }

        public IDrawable GetSolar(string solar)
		{
			var archetype = fldata.Solar.FindSolar(solar);
			//Load archetype references
			foreach (var path in archetype.MaterialPaths)
				resource.LoadResourceFile(ResolveDataPath(path));
			//Get drawable
			return resource.GetDrawable(ResolveDataPath(archetype.DaArchetypeName));
		}

		public IDrawable GetAsteroid(string asteroid)
		{
			var ast = fldata.Asteroids.FindAsteroid(asteroid);
			resource.LoadResourceFile(ResolveDataPath(ast.MaterialLibrary));
			return resource.GetDrawable(ResolveDataPath(ast.DaArchetype));
		}

		public IDrawable GetProp(string prop)
		{
			return resource.GetDrawable(ResolveDataPath(fldata.PetalDb.Props[prop]));
		}

		public IDrawable GetCart(string cart)
		{
			return resource.GetDrawable(ResolveDataPath(fldata.PetalDb.Carts[cart]));
		}

		public IDrawable GetRoom(string room)
		{
			return resource.GetDrawable(ResolveDataPath(fldata.PetalDb.Rooms[room]));
		}

		public GameData.SystemObject GetSystemObject(Data.Universe.SystemObject o)
		{
			var drawable = resource.GetDrawable (ResolveDataPath(o.Archetype.DaArchetypeName));
			var obj = new GameData.SystemObject ();
			obj.Nickname = o.Nickname;
			obj.DisplayName = GetString(o.IdsName);
			obj.Position = o.Pos.Value;
			if (o.DockWith != null)
			{
				obj.Dock = new DockAction() { Kind = DockKinds.Base, Target = o.DockWith };
			}
			else if (o.Goto != null)
			{
				obj.Dock = new DockAction() { Kind = DockKinds.Jump, Target = o.Goto.System, Exit = o.Goto.Exit, Tunnel = o.Goto.TunnelEffect };
			}
			if (o.Rotate != null) {
				obj.Rotation = 
					Matrix4.CreateRotationX (MathHelper.DegreesToRadians (o.Rotate.Value.X)) *
					Matrix4.CreateRotationY (MathHelper.DegreesToRadians (o.Rotate.Value.Y)) *
					Matrix4.CreateRotationZ (MathHelper.DegreesToRadians (o.Rotate.Value.Z));
			}
			//Load archetype references
			foreach (var path in o.Archetype.MaterialPaths)
				resource.LoadResourceFile (ResolveDataPath(path));
			//Construct archetype
			if (o.Archetype.Type == Data.Solar.ArchetypeType.sun) {
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
                    if (spines != null)
                    {
                        sun.SpinesSprite = spines.Shape;
                        sun.SpinesScale = spines.RadiusScale;
                        sun.Spines = new List<GameData.Spine>(spines.Items.Count);
                        foreach (var sp in spines.Items)
                            sun.Spines.Add(new GameData.Spine(sp.LengthScale, sp.WidthScale, sp.InnerColor, sp.OuterColor, sp.Alpha));
                    } else
                        FLLog.Error("Stararch", "Could not find spines " + star.Spines);
				}
				obj.Archetype = sun;
			} else {
				obj.Archetype = new GameData.Archetype ();
				foreach (var dockSphere in o.Archetype.DockingSpheres)
				{
					obj.Archetype.DockSpheres.Add(new GameData.DockSphere()
					{
						Name = dockSphere.Name,
						Hardpoint = dockSphere.Hardpoint,
						Radius = dockSphere.Radius,
						Script = dockSphere.Script
					});
				}
				if (o.Archetype.OpenAnim != null)
				{
					foreach (var sph in obj.Archetype.DockSpheres)
						sph.Script =  sph.Script ?? o.Archetype.OpenAnim;
				}
				if (o.Archetype.Type == Data.Solar.ArchetypeType.tradelane_ring)
				{
					obj.Archetype.DockSpheres.Add(new GameData.DockSphere()
					{
						Name = "tradelane",
						Hardpoint = "HpRightLane",
						Radius = 30
					});
					obj.Archetype.DockSpheres.Add(new GameData.DockSphere()
					{
						Name = "tradelane",
						Hardpoint = "HpLeftLane",
						Radius = 30
					});
					obj.Dock = new DockAction()
					{
						Kind = DockKinds.Tradelane,
						Target = o.NextRing,
						TargetLeft = o.PrevRing
					};
				}
			}
			obj.Archetype.ArchetypeName = o.Archetype.GetType ().Name;
			obj.Archetype.Drawable = drawable;
			obj.Archetype.LODRanges = o.Archetype.LODRanges;
			var ld = fldata.Loadouts.FindLoadout(o.LoadoutName);
			var archld = fldata.Loadouts.FindLoadout(o.Archetype.LoadoutName);
			if(ld != null) ProcessLoadout(ld, obj);
			if (archld != null) ProcessLoadout(archld, obj);
			return obj;
		}

        public GameData.Archetype GetSolarArchetype(string id)
        {
            var fl = fldata.Solar.FindSolar(id);
            foreach (var path in fl.MaterialPaths)
                resource.LoadResourceFile(ResolveDataPath(path));
            var arch = new GameData.Archetype();
            arch.Drawable  = resource.GetDrawable(ResolveDataPath(fl.DaArchetypeName));
            arch.LODRanges = fl.LODRanges;
            arch.ArchetypeName = fl.GetType().Name;
            return arch;
        }

        public GameData.Items.Equipment GetEquipment(string id)
		{
            var eq = fldata.Equipment.FindEquipment(id);
            if (eq == null)
            {
                FLLog.Error("Equipment", "Not found " + id);
                return null;
            }
            return GetEquipment(fldata.Equipment.FindEquipment(id));
        }

        GameData.Items.Equipment GetEquipment(Data.Equipment.AbstractEquipment val)
		{
            GameData.Items.Equipment equip = null;
			if (val is Data.Equipment.Light)
			{
				equip = GetLight((Data.Equipment.Light)val);
			}
			else if (val is Data.Equipment.InternalFx)
			{
				var eq = new GameData.Items.AnimationEquipment();
				eq.Animation = ((Data.Equipment.InternalFx)val).UseAnimation;
				equip = eq;
			}
			if (val is Data.Equipment.AttachedFx)
			{
				equip = GetAttachedFx((Data.Equipment.AttachedFx)val);
			}
			if (val is Data.Equipment.PowerCore)
			{
				var pc = (val as Data.Equipment.PowerCore);
				if(pc.MaterialLibrary != null)
					resource.LoadResourceFile(ResolveDataPath(pc.MaterialLibrary));
				var drawable = resource.GetDrawable(ResolveDataPath(pc.DaArchetype));
				equip = new GameData.Items.PowerEquipment()
				{
					Model = drawable
				};
			}
            if (val is Data.Equipment.Gun)
            {
                var gn = (val as Data.Equipment.Gun);
                if(gn.MaterialLibrary != null)
                    resource.LoadResourceFile(ResolveDataPath(gn.MaterialLibrary));
                var drawable = resource.GetDrawable(ResolveDataPath(gn.DaArchetype));
                var mn = fldata.Equipment.Munitions.FirstOrDefault((x) => x.Nickname.Equals(gn.ProjectileArchetype, StringComparison.OrdinalIgnoreCase));
                var effect = fldata.Effects.FindEffect(mn.ConstEffect);
                string visbeam;
                if (effect == null) visbeam = "";
                else visbeam = effect.VisBeam ?? "";
                var mequip = new GameData.Items.MunitionEquip()
                {
                    Def = mn,
                    ConstEffect_Beam = fldata.Effects.BeamSpears.FirstOrDefault((x) => x.Nickname.Equals(visbeam,StringComparison.OrdinalIgnoreCase)),
                    ConstEffect_Bolt = fldata.Effects.BeamBolts.FirstOrDefault((x) => x.Nickname.Equals(visbeam, StringComparison.OrdinalIgnoreCase))
                };
                equip = new GameData.Items.GunEquipment()
                {
                    Model = drawable,
                    Munition = mequip,
                    Def = gn                
                };
            }
			if (val is Data.Equipment.Thruster)
			{
				var th = (val as Data.Equipment.Thruster);
				resource.LoadResourceFile(ResolveDataPath(th.MaterialLibrary));
				var drawable = resource.GetDrawable(ResolveDataPath(th.DaArchetype));
				equip = new GameData.Items.ThrusterEquipment()
				{
					Drain = th.PowerUsage,
					Force = th.MaxForce,
					Model = drawable,
					HpParticles = th.HpParticles,
					Particles = GetEffect(th.Particles)
				};
			}
            equip.Nickname = val.Nickname;
            equip.HPChild = val.HPChild;
            equip.LODRanges = val.LODRanges;
			return equip;
		}
		void ProcessLoadout(Data.Solar.Loadout ld, GameData.SystemObject obj)
		{
			foreach (var key in ld.Equip.Keys)
			{
				var val = ld.Equip[key];
				if (val == null)
					continue;
				GameData.Items.Equipment equip = GetEquipment(val);
                //if (equip is GameData.Items.GunEquipment) continue;
				if (equip != null)
				{
					if (key.StartsWith("__noHardpoint", StringComparison.Ordinal))
						obj.LoadoutNoHardpoint.Add(equip);
					else
					{
						if(!obj.Loadout.ContainsKey(key)) obj.Loadout.Add(key, equip);
					}
				}
			}
		}

		public bool HasEffect(string effectName)
		{
			return fldata.Effects.FindEffect(effectName) != null || fldata.Effects.FindVisEffect(effectName) != null;
		}

		public ParticleEffect GetEffect(string effectName)
		{
			var effect = fldata.Effects.FindEffect(effectName);
			Data.Effects.VisEffect visfx;
			if (effect == null)
				visfx = fldata.Effects.FindVisEffect(effectName);
			else
				visfx = fldata.Effects.FindVisEffect(effect.VisEffect);
            if(effect == null && visfx == null) {
                FLLog.Error("Fx", "Can't find fx " + effectName);
                return null;
            }
            foreach (var texfile in visfx.Textures)
			{
				var path = Data.VFS.GetPath(fldata.Freelancer.DataPath + texfile);
				resource.LoadResourceFile(path);
			}
			var alepath = Data.VFS.GetPath(fldata.Freelancer.DataPath + visfx.AlchemyPath);
            var lib = resource.GetParticleLibrary(alepath);
			return lib.FindEffect((uint)visfx.EffectCrc);
		}

		GameData.Items.EffectEquipment GetAttachedFx(Data.Equipment.AttachedFx fx)
		{
			var equip = new GameData.Items.EffectEquipment();
			equip.Particles = GetEffect(fx.Particles);
			return equip;
		}

		GameData.Items.LightEquipment GetLight(Data.Equipment.Light lt)
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

