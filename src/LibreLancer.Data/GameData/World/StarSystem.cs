// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LibreLancer.Data.GameData.World;

public class StarSystem : NamedItem
{
    //Comes from universe.ini
    public Vector2 UniversePosition;
    public string? MsgIdPrefix;
    public VisitFlags Visit;

    public required string SourceFile;

    //System Info - is this used?
    public Faction? LocalFaction;

    //Background
    public Color4 BackgroundColor;

    //Starsphere
    public ResolvedModel? StarsBasic;
    public ResolvedModel? StarsComplex;

    public ResolvedModel? StarsNebula;

    //Encounter Parameters
    public List<EncounterParameters> EncounterParameters = [];

    //Texture Panels
    public List<string> TexturePanelsFiles = [];

    //Lighting
    public Color3f AmbientColor = Color3f.Black;

    public List<LightSource> LightSources = [];

    //Objects
    public List<SystemObject> Objects = [];

    //Nebulae
    public List<Nebula> Nebulae = [];

    //Asteroid Fields
    public List<AsteroidField> AsteroidFields = [];

    //Zones
    public List<Zone> Zones = [];

    public Dictionary<string, Zone> ZoneDict = new(StringComparer.OrdinalIgnoreCase);

    //Music
    public string? MusicSpace;
    public string? MusicDanger;

    public string? MusicBattle;

    //Clipping
    public float FarClip;

    //Navmap
    public float NavMapScale;

    //Dust
    public string? Spacedust;

    public int SpacedustMaxParticles;

    //Preloads
    public PreloadObject[]? Preloads;

    //Resource files to load
    public UniqueList<string> ResourceFiles = [];

    //Calculated
    public Dictionary<StarSystem, List<StarSystem>> ShortestPathsLegal = new();
    public Dictionary<StarSystem, List<StarSystem>> ShortestPathsIllegal = new();
    public Dictionary<StarSystem, List<StarSystem>> ShortestPathsAny = new();

    public StarSystem()
    {
    }

    public void CopyTo(StarSystem other)
    {
        other.Nickname = Nickname;
        other.IdsName = IdsName;
        other.IdsInfo = IdsInfo;
        other.UniversePosition = UniversePosition;
        other.MsgIdPrefix = MsgIdPrefix;
        other.Visit = Visit;
        other.SourceFile = SourceFile;
        other.LocalFaction = LocalFaction;
        other.BackgroundColor = BackgroundColor;
        other.StarsBasic = StarsBasic;
        other.StarsComplex = StarsComplex;
        other.StarsNebula = StarsNebula;
        other.AmbientColor = AmbientColor;
        other.MusicSpace = MusicSpace;
        other.MusicDanger = MusicDanger;
        other.MusicBattle = MusicBattle;
        other.FarClip = FarClip;
        other.NavMapScale = NavMapScale;
        other.Spacedust = Spacedust;
        other.SpacedustMaxParticles = SpacedustMaxParticles;

        foreach (var z in Zones)
        {
            var cloned = z.Clone();
            other.Zones.Add(cloned);
            other.ZoneDict[cloned.Nickname] = cloned;
        }

        foreach (var a in AsteroidFields)
        {
            other.AsteroidFields.Add(a.Clone(other.ZoneDict));
        }

        foreach (var n in Nebulae)
        {
            other.Nebulae.Add(n.Clone(other.ZoneDict));
        }

        foreach (var o in Objects)
        {
            other.Objects.Add(o.Clone());
        }

        foreach (var lt in LightSources)
        {
            other.LightSources.Add(lt.Clone());
        }

        foreach (var ep in EncounterParameters)
        {
            other.EncounterParameters.Add(ep);
        }

        other.TexturePanelsFiles = TexturePanelsFiles.ToList();
        foreach (var rf in ResourceFiles)
            other.ResourceFiles.Add(rf);
        if (Preloads is { Length: > 0 })
            other.Preloads = Preloads.ToArray();
    }

    public StarSystem Clone()
    {
        var s = new StarSystem()
        {
            SourceFile = ""
        };

        CopyTo(s);
        return s;
    }
}
