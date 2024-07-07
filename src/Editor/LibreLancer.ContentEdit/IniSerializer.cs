using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.World;
using System.Text;
using LibreLancer.Data;
using LibreLancer.GameData;
using LibreLancer.GameData.Archetypes;
using LibreLancer.Ini;
using LibreLancer.Render;
using LibreLancer.Data.Missions;

namespace LibreLancer.ContentEdit;

public static class IniSerializer
{
    public static List<Section> SerializeStarSystem(StarSystem sys)
    {
        var ib = new IniBuilder();
        ib.Section("SystemInfo")
            .Entry("space_color", sys.BackgroundColor)
            .Entry("local_faction", sys.LocalFaction.Nickname)
            .OptionalEntry("space_farclip", sys.FarClip, 20000);

        //Archetype
        if (sys.Preloads.Length > 0)
        {
            var section = ib.Section("Archetype");
            foreach (var p in sys.Preloads)
            {
                var name = p.Type switch
                {
                    PreloadType.Ship => "ship",
                    PreloadType.Simple => "simple",
                    PreloadType.Solar => "solar",
                    PreloadType.Sound => "snd",
                    PreloadType.Voice => "voice",
                    PreloadType.Equipment => "equipment",
                    _ => "solar"
                };
                section.Entry(name, p.Values.Select(x => x.String ?? x.Hash.ToString()).ToArray());
            }
        }

        foreach (var ep in sys.EncounterParameters)
            ib.Section("EncounterParameters")
                .Entry("nickname", ep.Nickname)
                .Entry("filename", ep.SourceFile);


        //TexturePanels
        if (sys.TexturePanelsFiles.Count > 0)
        {
            var section = ib.Section("TexturePanels");
            foreach (var f in sys.TexturePanelsFiles)
                section.Entry("file", f);
        }

        //Music
        ib.Section("Music")
            .OptionalEntry("space", sys.MusicSpace)
            .OptionalEntry("danger", sys.MusicDanger)
            .OptionalEntry("battle", sys.MusicBattle)
            .RemoveIfEmpty();

        //Dust
        ib.Section("Dust")
            .OptionalEntry("spacedust", sys.Spacedust)
            .OptionalEntry("spacedust_maxparticles", sys.SpacedustMaxParticles)
            .RemoveIfEmpty();


        foreach (var nebula in sys.Nebulae)
            ib.Section("Nebula")
                .Entry("file", nebula.SourceFile)
                .OptionalEntry("zone", nebula.Zone?.Nickname);

        foreach (var ast in sys.AsteroidFields)
            ib.Section("Asteroids")
                .Entry("file", ast.SourceFile)
                .OptionalEntry("zone", ast.Zone?.Nickname);

        //Ambient Color
        ib.Section("Ambient")
            .Entry("color", sys.AmbientColor);

        //Background
        ib.Section("Background")
            .OptionalEntry("basic_stars", sys.StarsBasic?.SourcePath)
            .OptionalEntry("complex_stars", sys.StarsComplex?.SourcePath)
            .OptionalEntry("nebulae", sys.StarsNebula?.SourcePath)
            .RemoveIfEmpty();

        foreach (var lt in sys.LightSources)
            SerializeLightSource(lt, ib);

        foreach (var zn in sys.Zones)
            SerializeZone(zn, ib);

        foreach (var obj in sys.Objects)
            SerializeSystemObject(obj, ib);

        return ib.Sections;
    }

    static void SerializeLightSource(LightSource lt, IniBuilder builder)
    {
        var sb = builder.Section("LightSource")
            .Entry("nickname", lt.Nickname)
            .Entry("pos", lt.Light.Position)
            .Entry("color", lt.Light.Color)
            .Entry("range", lt.Light.Range);
        sb.Entry("type", lt.Light.Kind switch
        {
            LightKind.Directional => "DIRECTIONAL",
            LightKind.PointAttenCurve => "DIRECTIONAL",
            _ => "POINT"
        });
        if (lt.Light.Kind == LightKind.Directional)
            sb.Entry("direction", lt.Light.Direction);
        if (!string.IsNullOrWhiteSpace(lt.AttenuationCurveName))
            sb.Entry("atten_curve", lt.AttenuationCurveName);
        else
            sb.Entry("attenuation", lt.Light.Attenuation);
    }

    static void SerializeRotation(IniBuilder.IniSectionBuilder sb, Matrix4x4 matrix)
    {
        var euler = matrix.GetEulerDegrees();
        var ln = euler.Length();
        if (!float.IsNaN(ln) && ln > float.Epsilon)
            sb.Entry("rotate", euler);
    }
    static void SerializeRotation(IniBuilder.IniSectionBuilder sb, Quaternion rotation)
    {
        var euler = Matrix4x4.CreateFromQuaternion(rotation).GetEulerDegrees();
        var ln = euler.Length();
        if (!float.IsNaN(ln) && ln > float.Epsilon)
            sb.Entry("rotate", euler);
    }


    static void SerializeZone(Zone z, IniBuilder builder)
    {
        var sb = builder.Section("Zone")
            .Entry("nickname", z.Nickname)
            .OptionalEntry("ids_name", z.IdsName);
        foreach (var info in z.IdsInfo)
            sb.Entry("ids_info", info);
        sb.OptionalEntry("comment", CommentEscaping.Escape(z.Comment));
        sb.Entry("pos", z.Position);
        SerializeRotation(sb, z.RotationMatrix);
        switch (z.Shape)
        {
            case ShapeKind.Box:
                sb.Entry("shape", "BOX")
                    .Entry("size", z.Size);
                break;
            case ShapeKind.Sphere:
                sb.Entry("shape", "SPHERE")
                    .Entry("size", z.Size.X);
                break;
            case ShapeKind.Cylinder:
                sb.Entry("shape", "CYLINDER")
                    .Entry("size", z.Size.X, z.Size.Y);
                break;
            case ShapeKind.Ellipsoid:
                sb.Entry("shape", "ELLIPSOID")
                    .Entry("size", z.Size);
                break;
            case ShapeKind.Ring:
                sb.Entry("shape", "RING")
                    .Entry("size", z.Size.X, z.Size.Z, z.Size.Y);
                break;
        }
        sb.OptionalEntry("property_flags", (uint) z.PropertyFlags);
        if (z.PropertyFogColor != null)
            sb.Entry("property_fog_color", z.PropertyFogColor.Value);
        sb.OptionalEntry("damage", z.Damage)
            .OptionalEntry("visit", (int)z.VisitFlags)
            .OptionalEntry("spacedust", z.Spacedust)
            .OptionalEntry("spacedust_maxparticles", z.SpacedustMaxParticles)
            .OptionalEntry("interference", z.Interference)
            .OptionalEntry("drag_modifier", z.DragModifier)
            .OptionalEntry("power_modifier", z.PowerModifier)
            .OptionalEntry("edge_fraction", z.EdgeFraction, 0.25f)
            .OptionalEntry("attack_ids", z.AttackIds)
            .OptionalEntry("mission_type", z.MissionType)
            .OptionalEntry("lane_id", z.LaneId)
            .OptionalEntry("tradelane_attack", z.TradelaneAttack)
            .OptionalEntry("tradelane_down", z.TradelaneDown)
            .OptionalEntry("sort", z.Sort)
            .OptionalEntry("vignette_type", z.VignetteType)
            .OptionalEntry("toughness", z.Toughness)
            .OptionalEntry("density", z.Density)
            .OptionalEntry("repop_time", z.RepopTime)
            .OptionalEntry("max_battle_size", z.MaxBattleSize)
            .OptionalEntry("pop_type", z.PopType);
        if (z.PopulationAdditive.HasValue)
            sb.Entry("population_additive", z.PopulationAdditive.Value);
        sb.OptionalEntry("relief_time", z.ReliefTime)
            .OptionalEntry("path_label", z.PathLabel)
            .OptionalEntry("usage", z.Usage);
        foreach (var dr in z.DensityRestrictions)
        {
            sb.Entry("density_restriction", dr.Count, dr.Type);
        }

        foreach (var e in z.Encounters)
        {
            sb.Entry("encounter", e.Archetype, e.Difficulty, e.Chance);
            foreach (var f in e.FactionSpawns)
            {
                sb.Entry("faction", f.Faction, f.Chance);
            }
        }

        sb.OptionalEntry("mission_eligible", z.MissionEligible)
            .OptionalEntry("Music", z.Music);
    }

    static void SerializeSystemObject(SystemObject obj, IniBuilder builder)
    {
        var sb = builder.Section("Object")
            .Entry("nickname", obj.Nickname)
            .OptionalEntry("comment", CommentEscaping.Escape(obj.Comment))
            .OptionalEntry("ids_name", obj.IdsName);
        if (obj.Position != Vector3.Zero)
            sb.Entry("pos", obj.Position);
        SerializeRotation(sb, obj.Rotation);
        if (obj.AmbientColor != null)
            sb.Entry("ambient_color", obj.AmbientColor.Value);
        sb.OptionalEntry( "Archetype", obj.Archetype?.Nickname)
            .OptionalEntry("star", obj.Star?.Nickname)
            .OptionalEntry("msg_id_prefix", obj.MsgIdPrefix);
        foreach (var i in obj.IdsInfo)
            sb.Entry("ids_info", i);
        if (obj.Spin != Vector3.Zero)
            sb.Entry("spin", obj.Spin);
        sb.OptionalEntry("atmosphere_range", obj.AtmosphereRange);
        if (obj.BurnColor != null)
            sb.Entry("burn_color", obj.BurnColor.Value);
        sb.OptionalEntry("base", obj.Base?.Nickname);
        if (obj.Dock != null)
        {
            if (obj.Dock.Kind == DockKinds.Base)
            {
                sb.Entry("dock_with", obj.Dock.Target);
            }
            else if (obj.Dock.Kind == DockKinds.Jump)
            {
                sb.Entry("goto", obj.Dock.Target, obj.Dock.Exit, obj.Dock.Tunnel);
            }
            else if (obj.Dock.Kind == DockKinds.Tradelane)
            {
                sb.OptionalEntry("prev_ring", obj.Dock.TargetLeft);
                sb.OptionalEntry("next_ring", obj.Dock.Target);
            }
        }

        sb.OptionalEntry("behavior", obj.Behavior)
        .OptionalEntry("voice", obj.Voice)
        .OptionalEntry("space_costume", obj.SpaceCostume)
        .OptionalEntry("faction", obj.Faction?.Nickname)
        .OptionalEntry("difficulty_level", obj.DifficultyLevel)
        .OptionalEntry("loadout", obj.Loadout?.Nickname)
        .OptionalEntry("pilot", obj.Pilot?.Nickname)
        .OptionalEntry("reputation", obj.Reputation?.Nickname)
        .OptionalEntry("tradelane_space_name", obj.TradelaneSpaceName)
        .OptionalEntry("parent", obj.Parent)
        .OptionalEntry("visit", (int) obj.Visit);
    }

    public static List<Section> SerializeUniverse(IEnumerable<StarSystem> systems, IEnumerable<Base> bases)
    {
        var ib = new IniBuilder();

        ib.Section("Time")
            .Entry("seconds_per_day", 1800);

        foreach (var b in bases)
        {
            ib.Section("Base")
                .Entry("nickname", b.Nickname)
                .Entry("system", b.System)
                .OptionalEntry("strid_name", b.IdsName)
                .Entry("file", b.SourceFile)
                .OptionalEntry("BGCS_base_run_by", b.BaseRunBy)
                .OptionalEntry("terrain_tiny", b.TerrainTiny)
                .OptionalEntry("terrain_sml", b.TerrainSml)
                .OptionalEntry("terrain_lrg", b.TerrainLrg)
                .OptionalEntry("terrain_dyna_01", b.TerrainDyna1)
                .OptionalEntry("terrain_dyna_02", b.TerrainDyna2)
                .OptionalEntry("autosave_forbidden", b.AutosaveForbidden);
        }

        foreach (var s in systems)
        {
            ib.Section("System")
                .Entry("nickname", s.Nickname)
                .Entry("file", s.SourceFile)
                .Entry("pos", s.UniversePosition)
                .OptionalEntry("msg_id_prefix", s.MsgIdPrefix)
                .OptionalEntry("visit", (int)s.Visit)
                .OptionalEntry("strid_name", s.IdsName)
                .OptionalEntry("ids_info", s.IdsInfo)
                .OptionalEntry("NavMapScale", s.NavMapScale);
        }

        return ib.Sections;
    }

    public static List<Section> SerializeRoom(BaseRoom room)
    {
        var ib = new IniBuilder();
        var section = ib.Section("Room_Info")
            .Entry("set_script", room.SetScript?.SourcePath);
        foreach (var scene in room.SceneScripts)
        {
            if (scene.TrafficPriority)
            {
                section.Entry("scene", scene.AllAmbient ? "all" : "ambient", scene.Thn.SourcePath, "TRAFFIC_PRIORITY");
            }
            else
            {
                section.Entry("scene", scene.AllAmbient ? "all" : "ambient", scene.Thn.SourcePath);
            }
        }

        ib.Section("[Room_Sound]");


        if (!string.IsNullOrWhiteSpace(room.PlayerShipPlacement))
            ib.Section("PlayerShipPlacement")
                .Entry("name", room.PlayerShipPlacement);

        if (!string.IsNullOrWhiteSpace(room.Camera))
            ib.Section("Camera")
                .Entry("name", room.Camera);

        foreach (var hotspot in room.Hotspots)
        {
            ib.Section("Hotspot")
                .Entry("name", hotspot.Name)
                .OptionalEntry("behavior", hotspot.Behavior)
                .OptionalEntry("room_switch", hotspot.Room)
                .OptionalEntry("virtual_room", hotspot.VirtualRoom)
                .OptionalEntry("set_virtual_room", hotspot.SetVirtualRoom);
        }
        return ib.Sections;
    }

    public static List<Section> SerializeBase(Base b)
    {
        var ib = new IniBuilder();
        ib.Section("BaseInfo")
            .Entry("nickname", b.Nickname)
            .OptionalEntry("start_room", b.StartRoom.Nickname);

        foreach (var r in b.Rooms)
        {
            ib.Section("Room")
                .Entry("nickname", r.Nickname)
                .Entry("file", r.SourceFile);
        }

        return ib.Sections;
    }

    public static List<Section> SerializeMBases(IEnumerable<Base> bases)
    {
        var ib = new IniBuilder();
        foreach (var b in bases)
        {
            ib.Section("MBase")
                .Entry("nickname", b.Nickname)
                .OptionalEntry("local_faction", b.LocalFaction?.Nickname)
                .OptionalEntry("diff", b.Diff)
                .OptionalEntry("msg_id_prefix", b.MsgIdPrefix);

            if (b.MinMissionOffers != 0 || b.MaxMissionOffers != 0)
                ib.Section("MVendor")
                    .Entry("num_offers", b.MinMissionOffers, b.MaxMissionOffers);

            foreach (var npc in b.Npcs)
            {
                var section = ib.Section("GF_NPC")
                    .Entry("nickname", npc.Nickname)
                    .OptionalEntry("base_appr", npc.BaseAppr)
                    .OptionalEntry("body", npc.Body)
                    .OptionalEntry("head", npc.Head)
                    .OptionalEntry("lefthand", npc.LeftHand)
                    .OptionalEntry("righthand", npc.RightHand)
                    .OptionalEntry("individual_name", npc.IndividualName)
                    .OptionalEntry("affiliation", npc.Affiliation?.Nickname)
                    .OptionalEntry("voice", npc.Voice);
                if (npc.Mission != null)
                    section.Entry("misn", npc.Mission.Kind, npc.Mission.Min, npc.Mission.Max);
                section.OptionalEntry("room", npc.Room);
                foreach (var br in npc.Bribes)
                {
                    section.Entry("bribe", br.Faction, br.Price, br.Ids);
                }
                foreach (var r in npc.Rumors.Where(x => !x.Type2))
                {
                    section.Entry("rumor", r.Start, r.End, r.RepRequired, r.Ids);
                    if (r.Objects != null)
                        section.Entry("rumorknowdb", r.Objects);
                }
                foreach (var r2 in npc.Rumors.Where(x => x.Type2))
                {
                    section.Entry("rumor_type2", r2.Start, r2.End, r2.RepRequired, r2.Ids);
                    if (r2.Objects != null)
                        section.Entry("rumorknowdb", r2.Objects);
                }
                foreach (var k in npc.Know)
                {
                    section.Entry("know", k.Ids1, k.Ids2, k.Price, k.RepRequired);
                    if (k.Objects != null)
                        section.Entry("knowdb", k.Objects);
                }
            }

            foreach (var room in b.Rooms)
            {
                var section = ib.Section("MRoom")
                    .Entry("nickname", room.Nickname)
                    .OptionalEntry("character_density", room.MaxCharacters);
                foreach (var npc in room.FixedNpcs)
                {
                    section.Entry("fixture", npc.Npc.Nickname, npc.Placement, npc.FidgetScript.SourcePath, npc.Action);
                }
            }
        }

        return ib.Sections;
    }

    public static List<Section> SerializeNews(IEnumerable<NewsItem> news, GameDataManager gameData)
    {
        var ib = new IniBuilder();
        foreach (var article in news)
        {
            var section = ib.Section("NewsItem")
                .Entry($"; {gameData.GetString(article.Headline)}")
                .Entry("rank", article.Rank)
                .Entry("icon", article.Icon)
                .Entry("logo", article.Logo)
                .Entry("category", article.Category)
                .Entry("headline", article.Headline)
                .Entry("text", article.Text)
                .OptionalEntry("autoselect", article.Autoselect)
                .OptionalEntry("audio", article.Audio);

            foreach (var b in article.Base)
            {
                section.Entry("base", b);
            }
        }

        return ib.Sections;
    }
}
