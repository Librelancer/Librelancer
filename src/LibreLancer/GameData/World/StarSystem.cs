// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Render;
using Microsoft.VisualBasic;

namespace LibreLancer.GameData.World
{
	public class StarSystem
	{
		public string Nickname;
        public Vector2 UniversePosition;
        public int Infocard;
		public string Name;
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
        public Color4 AmbientColor = Color4.Black;
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
        //Resource files to load
        public UniqueList<string> ResourceFiles = new UniqueList<string>();

        public StarSystem ()
		{
		}

        public string Serialize()
        {
            var sb = new StringBuilder();
            
            sb.AppendSection("SystemInfo")
                .AppendEntry("space_color", BackgroundColor)
                .AppendEntry("local_faction", LocalFaction.Nickname)
                .AppendLine();

            foreach (var ep in EncounterParameters)
                sb.AppendSection("EncounterParameters")
                    .AppendEntry("nickname", ep.Nickname)
                    .AppendEntry("file", ep.SourceFile)
                    .AppendLine();
            
            //TexturePanels
            if (TexturePanelsFiles.Count > 0)
            {
                sb.AppendSection("TexturePanels");
                foreach (var f in TexturePanelsFiles)
                    sb.AppendEntry("file", f);
                sb.AppendLine();
            }
            //Dust
            sb.AppendSection("Dust")
                .AppendEntry("spacedust", Spacedust)
                .AppendEntry("spacedust_maxparticles", SpacedustMaxParticles)
                .AppendLine();
            //Music
            sb.AppendSection("Music")
                .AppendEntry("music_space", MusicSpace)
                .AppendEntry("music_danger", MusicDanger)
                .AppendEntry("music_battle", MusicBattle)
                .AppendLine();
            
            foreach (var nebula in Nebulae)
                sb.AppendSection("Nebula")
                    .AppendEntry("file", nebula.SourceFile)
                    .AppendEntry("zone", nebula.Zone?.Nickname)
                    .AppendLine();

            foreach (var ast in AsteroidFields)
                sb.AppendSection("Asteroids")
                    .AppendEntry("file", ast.SourceFile)
                    .AppendEntry("zone", ast.Zone?.Nickname)
                    .AppendLine();

            sb.AppendSection("Ambient")
                .AppendEntry("color", AmbientColor)
                .AppendLine();

            //Background
            sb.AppendSection("Background")
                .AppendEntry("basic_stars", StarsBasic?.SourcePath)
                .AppendEntry("complex_stars", StarsComplex?.SourcePath)
                .AppendEntry("nebulae", StarsNebula?.SourcePath)
                .AppendLine();

            foreach (var zn in Zones)
                sb.AppendLine(zn.Serialize());

            foreach (var lt in LightSources)
                sb.AppendLine(lt.Serialize());
            
            foreach (var obj in Objects)
                sb.AppendLine(obj.Serialize());
            return sb.ToString();
        }
    }
}

