// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using LibreLancer.Ini;

using LibreLancer.Data.Solar;

namespace LibreLancer.Data.Universe
{
	public class StarSystem : UniverseElement
	{
        public bool MultiUniverse { get; private set; }
		public Vector2? Pos { get; private set; }
		public string MsgIdPrefix { get; private set; }
		public int? Visit { get; private set; }
		public int IdsInfo { get; private set; }
		public float? NavMapScale { get; private set; }

		public Color4? SpaceColor { get; private set; }
		public string LocalFaction { get; private set; }
		public bool? RpopSolarDetection { get; private set; }

		public string MusicSpace { get; private set; }
		public string MusicDanger { get; private set; }
		public string MusicBattle { get; private set; }

		public List<string> ArchetypeShip { get; private set; }
		public List<string> ArchetypeSimple { get; private set; }
		public List<string> ArchetypeSolar { get; private set; }
		public List<string> ArchetypeEquipment { get; private set; }
		public List<string> ArchetypeSnd { get; private set; }
		public List<List<string>> ArchetypeVoice { get; private set; }

		public string Spacedust { get; private set; }

		public List<Nebula> Nebulae { get; private set; }
		public List<AsteroidField> Asteroids { get; private set; }

		public Color4? AmbientColor { get; private set; }

		public List<LightSource> LightSources { get; private set; }
		public List<SystemObject> Objects { get; private set; }
		public List<EncounterParameter> EncounterParameters { get; private set; }

		public TexturePanelsRef TexturePanels { get; private set; }

		public string BackgroundBasicStarsPath;
		public string BackgroundComplexStarsPath;
		public string BackgroundNebulaePath;


		public List<Zone> Zones { get; private set; }
		public Field Field { get; private set; }
		public AsteroidBillboards AsteroidBillboards { get; private set; }

		public float? SpaceFarClip { get; private set; }

		public StarSystem(UniverseIni universe, Section section, FreelancerData data)
			: base(data)
		{
			if (universe == null) throw new ArgumentNullException("universe");
			if (section == null) throw new ArgumentNullException("section");
			string file = null;

			foreach (Entry e in section)
			{
				switch (e.Name.ToLowerInvariant())
				{
				case "nickname":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Nickname != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Nickname = e[0].ToString();
					break;
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (file != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					file = e[0].ToString();
					break;
				case "pos":
					if (e.Count != 2) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Pos != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Pos = new Vector2(e[0].ToSingle(), e[1].ToSingle());
					break;
				case "msg_id_prefix":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (MsgIdPrefix != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					MsgIdPrefix = e[0].ToString();
					break;
				case "visit":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (Visit != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Visit = e[0].ToInt32();
					break;
				case "strid_name":
					if (e.Count == 0) break;
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (IdsName != 0) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                    IdsName = e[0].ToInt32();
					break;
				case "ids_info":
					if (e.Count == 0) break;
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (IdsInfo != 0) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
                    IdsInfo = e[0].ToInt32();
					break;
				case "navmapscale":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (NavMapScale != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					NavMapScale = e[0].ToSingle();
					break;
				default:
					throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}

			if (file == null) { //TODO: MultiUniverse
				FLLog.Warning("Ini", "Unimplemented: Possible MultiUniverse system " + Nickname);
                MultiUniverse = true;
				return;
			}
			
			foreach (Section s in ParseFile(GameData.Freelancer.DataPath + "universe\\" + file))
			{
				switch (s.Name.ToLowerInvariant())
				{
				case "systeminfo":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "name":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (Name != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							Name = e[0].ToString();
							break;
						case "space_color":
							if (e.Count != 3) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (SpaceColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							SpaceColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
							break;
						case "local_faction":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (LocalFaction != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							LocalFaction = e[0].ToString();
							break;
						case "rpop_solar_detection":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (RpopSolarDetection != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							RpopSolarDetection = e[0].ToBoolean();
							break;
						case "space_farclip":
							if (SpaceFarClip != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							SpaceFarClip = e[0].ToSingle();
							break;
						default:
							throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
						}
					}
					break;
				case "music":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "space":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (MusicSpace != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							MusicSpace = e[0].ToString();
							break;
						case "danger":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (MusicDanger != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							MusicDanger = e[0].ToString();
							break;
						case "battle":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (MusicBattle != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							MusicBattle = e[0].ToString();
							break;
						default:
							throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
						}
					}
					break;
				case "archetype":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "ship":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ArchetypeShip == null) ArchetypeShip = new List<string>();
							ArchetypeShip.Add(e[0].ToString());
							break;
						case "simple":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ArchetypeSimple == null) ArchetypeSimple = new List<string>();
							ArchetypeSimple.Add(e[0].ToString());
							break;
						case "solar":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ArchetypeSolar == null) ArchetypeSolar = new List<string>();
                            ArchetypeSolar.Add(e[0].ToString());
							break;
						case "equipment":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ArchetypeEquipment == null) ArchetypeEquipment = new List<string>();
							ArchetypeEquipment.Add(e[0].ToString());
							break;
						case "snd":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (ArchetypeSnd == null) ArchetypeSnd = new List<string>();
							ArchetypeSnd.Add(e[0].ToString());
							break;
						case "voice":
							if (ArchetypeVoice == null) ArchetypeVoice = new List<List<string>>();
							ArchetypeVoice.Add(new List<string>());
							foreach (IValue i in e) ArchetypeVoice[ArchetypeVoice.Count - 1].Add(i.ToString());
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "dust":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "spacedust":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (Spacedust != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							Spacedust = e[0].ToString();
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "nebula":
					if (Nebulae == null) Nebulae = new List<Nebula>();
					Nebulae.Add(new Nebula(this, s, GameData));
					break;
				case "asteroids":
					if (Asteroids == null) Asteroids = new List<AsteroidField>();
					Asteroids.Add(new AsteroidField(this, s, GameData));
					break;
				case "ambient":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "color":
							if (e.Count != 3) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (AmbientColor != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							AmbientColor = new Color4(e[0].ToInt32() / 255f, e[1].ToInt32() / 255f, e[2].ToInt32() / 255f, 1f);
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "lightsource":
					if (LightSources == null) LightSources = new List<LightSource>();
					LightSources.Add(new LightSource(s, GameData));
					break;
				case "object":
					if (Objects == null) Objects = new List<SystemObject>();
					Objects.Add(new SystemObject(universe, this, s, GameData));
					break;
				case "encounterparameters":
					if (EncounterParameters == null) EncounterParameters = new List<EncounterParameter>();
					EncounterParameters.Add(new EncounterParameter(s));
					break;
				case "texturepanels":
					TexturePanels = new TexturePanelsRef(s, GameData);
					break;
				case "background":
					foreach (Entry e in s)
					{
						switch (e.Name.ToLowerInvariant())
						{
						case "basic_stars":
							if (e.Count != 1)
								throw new Exception ("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (BackgroundBasicStarsPath != null)
								throw new Exception ("Duplicate " + e.Name + " Entry in " + s.Name);
							BackgroundBasicStarsPath = VFS.GetPath (GameData.Freelancer.DataPath + e [0].ToString ());
							break;
						case "complex_stars":
							if (e.Count != 1) throw new Exception("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							if (BackgroundComplexStarsPath != null) throw new Exception("Duplicate " + e.Name + " Entry in " + s.Name);
							BackgroundComplexStarsPath = VFS.GetPath (GameData.Freelancer.DataPath + e [0].ToString ());
							break;
						case "nebulae":
							if (e.Count != 1)
								throw new Exception ("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
							string temp = VFS.GetPath (GameData.Freelancer.DataPath + e[0].ToString(), false);
							if (BackgroundNebulaePath != null && BackgroundNebulaePath != temp)
								throw new Exception ("Duplicate " + e.Name + " Entry in " + s.Name);
							BackgroundNebulaePath = temp;
							break;
						default:
							throw new Exception("Invalid Entry in " + s.Name + ": " + e.Name);
						}
					}
					break;
				case "zone":
					if (Zones == null) Zones = new List<Zone>();
					Zones.Add(new Zone(s, GameData));
					break;
				case "field":
					Field = new Field(s);
					break;
				case "asteroidbillboards":
					AsteroidBillboards = new AsteroidBillboards(s);
					break;
				default:
					throw new Exception("Invalid Section in " + file + ": " + s.Name);
				}
			}
		}

		public SystemObject FindObject(string nickname)
		{
			return (from SystemObject o in Objects where o.Nickname.ToLowerInvariant() == nickname.ToLowerInvariant() select o).First<SystemObject>();
		}

		/*public SystemObject FindJumpGateTo(StarSystem system)
		{
			return (from SystemObject o in Objects where o.Archetype is JumpGate && o.Goto.System == system select o).First<SystemObject>();
		}

		public SystemObject FindJumpHoleTo(StarSystem system)
		{
			return (from SystemObject o in Objects where o.Archetype is JumpHole && o.Goto.System == system select o).First<SystemObject>();
		}*/

		public Zone FindZone(string nickname)
		{
			var res = (from Zone z in Zones where z.Nickname.ToLowerInvariant() == nickname.ToLowerInvariant() select z);
			if (res.Count() == 0) return null;
			return res.First();
		}
	}
}