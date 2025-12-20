// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;

namespace LibreLancer.Data.GameData.World
{
	public class StarSystem : IdentifiableItem
	{
        //Comes from universe.ini
        public Vector2 UniversePosition;
        public int IdsName;
        public int IdsInfo;
        public string MsgIdPrefix;
        public VisitFlags Visit;
        public string SourceFile;
        //System Info - is this used?
        public Faction LocalFaction;
		//Background
		public Color4 BackgroundColor;
		//Starsphere
        public ResolvedModel StarsBasic;
        public ResolvedModel StarsComplex;
        public ResolvedModel StarsNebula;
        //Encounter Parameters
        public List<EncounterParameters> EncounterParameters = new List<EncounterParameters>();
        //Texture Panels
        public List<string> TexturePanelsFiles = new List<string>();
		//Lighting
        public Color3f AmbientColor = Color3f.Black;
		public List<LightSource> LightSources = new List<LightSource>();
		//Objects
		public List<SystemObject> Objects = new List<SystemObject>();
		//Nebulae
		public List<Nebula> Nebulae = new List<Nebula>();
		//Asteroid Fields
		public List<AsteroidField> AsteroidFields = new List<AsteroidField>();
		//Zones
		public List<Zone> Zones = new List<Zone>();
        public Dictionary<string, Zone> ZoneDict = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
		//Music
		public string MusicSpace;
        public string MusicDanger;
        public string MusicBattle;
		//Clipping
		public float FarClip;
        //Navmap
        public float NavMapScale;
        //Dust
        public string Spacedust;
        public int SpacedustMaxParticles;
        //Preloads
        public PreloadObject[] Preloads;
        //Resource files to load
        public UniqueList<string> ResourceFiles = new UniqueList<string>();

        //Calculated
        public Dictionary<StarSystem, List<StarSystem>> ShortestPathsLegal = new();
        public Dictionary<StarSystem, List<StarSystem>> ShortestPathsIllegal = new();
        public Dictionary<StarSystem, List<StarSystem>> ShortestPathsAny = new();

        public StarSystem ()
		{
        }
    }
}

