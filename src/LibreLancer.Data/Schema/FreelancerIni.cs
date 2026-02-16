// MIT License - Copyright (c) Malte Rupprecht, Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Data.Dll;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema;

public class FreelancerIni
{
    public bool IsLibrelancer { get; private set; }
    public List<ResourceDll>? Resources { get; private set; } = [];
    public List<string> StartupMovies { get; private set; }

    public string DataPath { get; private set; } = null!;
    public List<string> SolarPaths { get; private set; }
    public string? UniversePath { get; private set; }
    public string? HudPath { get; private set; }
    public string? XInterfacePath { get; private set; }
    public string? DataVersion { get; private set; }

    public List<string> EquipmentPaths { get; private set; }
    public List<string> LoadoutPaths { get; private set; }
    public List<string> ShiparchPaths { get; private set; }
    public List<string> GoodsPaths { get; private set; }
    public List<string> MarketsPaths { get; private set; }
    public List<string> SoundPaths { get; private set; }
    public List<string> GraphPaths { get; private set; }
    public List<string> EffectPaths { get; private set; }

    public List<string> ExplosionPaths { get; private set; }
    public List<string> AsteroidPaths { get; private set; }
    public List<string> RichFontPaths { get; private set; }
    public List<string> FontPaths { get; private set;  }
    public List<string> PetalDbPaths { get; private set; }
    public List<string> FusePaths { get; private set;  }
    public List<string> NewCharDBPaths { get; private set;  }

    public List<string> VoicePaths { get; private set; }

    public string? StarsPath { get; private set; }
    public string? BodypartsPath { get; private set; }
    public string? CostumesPath { get; private set; }
    public string? EffectShapesPath { get; private set; }
    //Extended. Not in vanilla
    public string? DacomPath { get; private set; } = @"EXE\dacom.ini";

    public string NewPlayerPath { get; private set; } = @"EXE\newplayer.fl";

    public string MpNewCharacterPath { get; private set; } = @"EXE\mpnewcharacter.fl";

    public List<string>? MBasesPaths { get; private set; } = [];

    public string MousePath { get; private set; }
    public string CamerasPath { get; private set; }
    public string ConstantsPath { get; private set; }

    public string? NavmapPath { get; private set; }

    public List<string> NoNavmapSystems { get; private set; }

    private static readonly string[] NoNavmaps =
    [
        "St02c",
        "St03b",
        "St03",
        "St02"
    ];
    public List<string?> HiddenFactions { get; private set;  }

    private static readonly string[] NoShowFactions =
    [
        "fc_uk_grp",
        "fc_ouk_grp",
        "fc_q_grp",
        "fc_f_grp",
        "fc_or_grp",
        "fc_n_grp",
        "fc_rn_grp",
        "fc_kn_grp",
        "fc_ln_grp"
    ];

    private static string FindIni(FileSystem vfs) => vfs.FileExists("librelancer.ini") ? "librelancer.ini" : @"EXE\freelancer.ini";

    private static string EndInSep(string path)
    {
        if (path[path.Length - 1] == '/' || path[path.Length - 1] == '\\')
            return path;
        return path + Path.DirectorySeparatorChar;
    }

    public FreelancerIni(FileSystem vfs) : this(FindIni(vfs), vfs) { }

    public FreelancerIni(string path, FileSystem vfs)
    {
        IsLibrelancer = path.EndsWith("librelancer.ini", StringComparison.OrdinalIgnoreCase);
        if (IsLibrelancer)
        {
            DacomPath = null;
        }

        EquipmentPaths = [];
        LoadoutPaths = [];
        ShiparchPaths = [];
        SoundPaths = [];
        GraphPaths = [];
        EffectPaths = [];
        ExplosionPaths = [];
        AsteroidPaths = [];
        RichFontPaths = [];
        FontPaths = [];
        PetalDbPaths = [];
        StartupMovies = [];
        GoodsPaths = [];
        MarketsPaths = [];
        FusePaths = [];
        NewCharDBPaths = [];
        VoicePaths = [];
        SolarPaths = [];

        bool extNoNavmaps = false;
        bool extHideFac = false;
        NoNavmapSystems = [..NoNavmaps];
        HiddenFactions = [..NoShowFactions];

        foreach (Section s in IniFile.ParseFile(path, vfs)) {
            switch (s.Name.ToLowerInvariant ()) {
                case "freelancer":
                    foreach (Entry e in s) {
                        if (e.Name.ToLowerInvariant () == "data path") {
                            if (e.Count != 1)
                                throw new Exception ("Invalid number of values in " + s.Name + " Entry " + e.Name + ": " + e.Count);
                            if (DataPath != null)
                                throw new Exception ("Duplicate " + e.Name + " Entry in " + s.Name);
                            if (IsLibrelancer)
                                DataPath = EndInSep(e[0].ToString());
                            else
                                DataPath = "EXE\\" + EndInSep(e[0].ToString());
                        }
                        if (e.Name.ToLowerInvariant() == "dacom path")
                        {
                            DacomPath = e[0].ToString();
                        }
                    }
                    break;
                case "resources":
                    Resources = [];
                    //NOTE: Freelancer hardcodes resources.dll
                    //Not hardcoded for librelancer.ini as it will break
                    string start = IsLibrelancer ? "" : "EXE\\";
                    string? resdll = start + "resources.dll";
                    if (!IsLibrelancer) {
                        if (vfs.FileExists(resdll))
                        {
                            using var stream = vfs.Open(resdll);
                            Resources.Add(ResourceDll.FromStream(stream, vfs.GetBackingFileName(resdll)));
                        }
                        else
                        {
                            FLLog.Warning("Dll", "resources.dll not found");
                        }
                    }
                    foreach (Entry e in s)
                    {
                        if (e.Name.ToLowerInvariant () != "dll")
                            continue;
                        string dllname = start + e[0];
                        if (vfs.FileExists(dllname))
                        {
                            using var stream = vfs.Open(dllname);
                            Resources.Add(ResourceDll.FromStream(stream, vfs.GetBackingFileName(dllname)));
                        }
                        else
                        {
                            FLLog.Warning("Dll", e[0].ToString());
                        }
                    }
                    break;
                case "startup":
                    foreach (Entry e in s) {
                        if (e.Name.ToLowerInvariant () != "movie_file")
                            continue;
                        StartupMovies.Add (e [0].ToString());
                    }
                    break;
                case "extended":
                    foreach(Entry e in s) {
                        switch(e.Name.ToLowerInvariant())
                        {
                            case "xinterface":
                                if (Directory.Exists(e[0].ToString()))
                                    XInterfacePath = e[0].ToString();
                                else
                                    XInterfacePath = DataPath + e[0];
                                if (!XInterfacePath!.EndsWith("\\",StringComparison.InvariantCulture) &&
                                    !XInterfacePath.EndsWith("/",StringComparison.InvariantCulture))
                                    XInterfacePath += "/";
                                break;
                            case "dataversion":
                                DataVersion = e[0].ToString();
                                break;
                            case "nonavmap":
                                if (!extNoNavmaps) { NoNavmapSystems = []; extNoNavmaps = true; }
                                NoNavmapSystems.Add(e[0].ToString());
                                break;
                            case "hidefaction":
                                if (!extHideFac) { HiddenFactions = [];  extHideFac = true; };
                                HiddenFactions.Add(e[0].ToString());
                                break;
                        }
                    }
                    break;
                case "data":
                    foreach (Entry e in s) {
                        switch (e.Name.ToLowerInvariant ()) {
                            case "solar":
                                SolarPaths.Add(DataPath + e[0]);
                                break;
                            case "universe":
                                UniversePath = DataPath + e [0];
                                break;
                            case "equipment":
                                EquipmentPaths.Add(DataPath + e [0]);
                                break;
                            case "loadouts":
                                LoadoutPaths.Add(DataPath + e [0]);
                                break;
                            case "stars":
                                StarsPath = DataPath + e [0];
                                break;
                            case "bodyparts":
                                BodypartsPath = DataPath + e [0];
                                break;
                            case "costumes":
                                CostumesPath = DataPath + e [0];
                                break;
                            case "sounds":
                                SoundPaths.Add(DataPath + e[0]);
                                break;
                            case "ships":
                                ShiparchPaths.Add (DataPath + e [0]);
                                break;
                            case "rich_fonts":
                                RichFontPaths.Add(DataPath + e[0]);
                                break;
                            case "fonts":
                                FontPaths.Add(DataPath + e[0]);
                                break;
                            case "igraph":
                                GraphPaths.Add(DataPath + e[0]);
                                break;
                            case "effect_shapes":
                                EffectShapesPath = DataPath + e[0];
                                break;
                            case "effects":
                                EffectPaths.Add(DataPath + e[0]);
                                break;
                            case "explosions":
                                ExplosionPaths.Add(DataPath + e[0]);
                                break;
                            case "asteroids":
                                AsteroidPaths.Add (DataPath + e [0]);
                                break;
                            case "petaldb":
                                PetalDbPaths.Add(DataPath + e[0]);
                                break;
                            case "hud":
                                HudPath = DataPath + e[0];
                                break;
                            case "goods":
                                GoodsPaths.Add(DataPath + e[0]);
                                break;
                            case "markets":
                                MarketsPaths.Add(DataPath + e[0]);
                                break;
                            case "fuses":
                                FusePaths.Add(DataPath + e[0]);
                                break;
                            case "newchardb":
                                NewCharDBPaths.Add(DataPath + e[0]);
                                break;
                            case "voices":
                                VoicePaths.Add(DataPath + e[0]);
                                break;
                            //extended
                            case "newplayer":
                                NewPlayerPath = DataPath + e[0];
                                break;
                            case "mpnewcharacter":
                                MpNewCharacterPath = DataPath + e[0];
                                break;
                            case "mbases":
                                if (MBasesPaths == null) MBasesPaths = [];
                                MBasesPaths.Add(DataPath + e[0]);
                                break;
                            case "mouse":
                                MousePath = DataPath + e[0];
                                break;
                            case "cameras":
                                CamerasPath = DataPath + e[0];
                                break;
                            case "constants":
                                ConstantsPath = DataPath + e[0];
                                break;
                            case "navmap":
                                NavmapPath = DataPath + e[0];
                                break;
                        }
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(MousePath)) MousePath = DataPath + "mouse.ini";
        if (string.IsNullOrEmpty(CamerasPath)) CamerasPath = DataPath + "cameras.ini";
        if (string.IsNullOrEmpty(ConstantsPath)) ConstantsPath = DataPath + "constants.ini";
    }
}
