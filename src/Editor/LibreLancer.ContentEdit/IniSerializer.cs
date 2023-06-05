using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.World;
using System.Text;
using LibreLancer.Data;
using LibreLancer.GameData.Archetypes;
using LibreLancer.Render;

namespace LibreLancer.ContentEdit;

public static class IniSerializer
{
    public static string SerializeStarSystem(StarSystem sys)
    {
        var sb = new StringBuilder();
        sb.AppendSection("SystemInfo")
            .AppendEntry("space_color", sys.BackgroundColor)
            .AppendEntry("local_faction", sys.LocalFaction.Nickname);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (sys.FarClip != 20000)
            sb.AppendEntry("space_farclip", sys.FarClip);
        sb.AppendLine();
        foreach (var ep in sys.EncounterParameters)
            sb.AppendSection("EncounterParameters")
                .AppendEntry("nickname", ep.Nickname)
                .AppendEntry("file", ep.SourceFile)
                .AppendLine();

        //TexturePanels
        if (sys.TexturePanelsFiles.Count > 0)
        {
            sb.AppendSection("TexturePanels");
            foreach (var f in sys.TexturePanelsFiles)
                sb.AppendEntry("file", f);
            sb.AppendLine();
        }

        //Music
        sb.AppendSection("Music")
            .AppendEntry("music_space", sys.MusicSpace)
            .AppendEntry("music_danger", sys.MusicDanger)
            .AppendEntry("music_battle", sys.MusicBattle)
            .AppendLine();

        //Dust
        sb.AppendSection("Dust")
            .AppendEntry("spacedust", sys.Spacedust)
            .AppendEntry("spacedust_maxparticles", sys.SpacedustMaxParticles)
            .AppendLine();


        foreach (var nebula in sys.Nebulae)
            sb.AppendSection("Nebula")
                .AppendEntry("file", nebula.SourceFile)
                .AppendEntry("zone", nebula.Zone?.Nickname)
                .AppendLine();

        foreach (var ast in sys.AsteroidFields)
            sb.AppendSection("Asteroids")
                .AppendEntry("file", ast.SourceFile)
                .AppendEntry("zone", ast.Zone?.Nickname)
                .AppendLine();

        //Ambient Color
        sb.AppendSection("Ambient")
            .AppendEntry("color", sys.AmbientColor)
            .AppendLine();

        //Background
        sb.AppendSection("Background")
            .AppendEntry("basic_stars", sys.StarsBasic?.SourcePath)
            .AppendEntry("complex_stars", sys.StarsComplex?.SourcePath)
            .AppendEntry("nebulae", sys.StarsNebula?.SourcePath)
            .AppendLine();

        foreach (var lt in sys.LightSources)
            sb.AppendLine(SerializeLightSource(lt));

        foreach (var zn in sys.Zones)
            sb.AppendLine(SerializeZone(zn));

        foreach (var obj in sys.Objects)
            sb.AppendLine(SerializeSystemObject(obj));

        return sb.ToString();
    }

    public static string SerializeLightSource(LightSource lt)
    {
        var sb = new StringBuilder();
        sb.AppendSection("LightSource")
            .AppendEntry("nickname", lt.Nickname)
            .AppendEntry("pos", lt.Light.Position)
            .AppendEntry("color", lt.Light.Color)
            .AppendEntry("range", lt.Light.Range);
        if (lt.Light.Direction != Vector3.UnitZ)
            sb.AppendEntry("direction", lt.Light.Direction);
        sb.AppendEntry("type", lt.Light.Kind == LightKind.Directional ? "DIRECTIONAL" : "POINT");
        if (!string.IsNullOrWhiteSpace(lt.AttenuationCurveName))
            sb.AppendEntry("atten_curve", lt.AttenuationCurveName);
        else
            sb.AppendEntry("attenuation", lt.Light.Attenuation);
        return sb.ToString();
    }

    public static string SerializeZone(Zone z)
    {
        var sb = new StringBuilder();
        sb.AppendSection("Zone");
        sb.AppendEntry("nickname", z.Nickname);
        sb.AppendEntry("ids_name", z.IdsName);
        foreach (var info in z.IdsInfo)
            sb.AppendEntry("ids_info", info);
        if (z.Comment != null)
            foreach (var c in z.Comment)
                sb.AppendEntry("comment", c);
        sb.AppendEntry("pos", z.Position);
        var rot = z.RotationMatrix.GetEulerDegrees();
        var ln = rot.Length();
        if (!float.IsNaN(ln) && ln > 0)
            sb.AppendEntry("rotate", new Vector3(rot.Y, rot.X, rot.Z));
        switch (z.Shape)
        {
            case ZoneBox box:
                sb.AppendEntry("shape", "BOX")
                    .AppendEntry("size", box.Size);
                break;
            case ZoneSphere sphere:
                sb.AppendEntry("shape", "SPHERE")
                    .AppendEntry("size", sphere.Radius);
                break;
            case ZoneCylinder cylinder:
                sb.AppendEntry("shape", "CYLINDER")
                    .AppendEntry("size", new Vector2(cylinder.Radius, cylinder.Height));
                break;
            case ZoneEllipsoid ellipsoid:
                sb.AppendEntry("shape", "ELLIPSOID")
                    .AppendEntry("size", ellipsoid.Size);
                break;
            case ZoneRing ring:
                sb.AppendEntry("shape", "RING")
                    .AppendEntry("size", new Vector3(ring.OuterRadius, ring.InnerRadius, ring.Height));
                break;
        }

        sb.AppendEntry("property_flags", (uint) z.PropertyFlags, false);
        if (z.PropertyFogColor != null)
            sb.AppendEntry("property_fog_color", z.PropertyFogColor.Value);
        sb.AppendEntry("damage", z.Damage, false);
        sb.AppendEntry("visit", (int) z.VisitFlags, false);
        sb.AppendEntry("spacedust", z.Spacedust);
        sb.AppendEntry("spacedust_maxparticles", z.SpacedustMaxParticles, false);
        sb.AppendEntry("interference", z.Interference, false);
        sb.AppendEntry("drag_modifier", z.DragModifier, false);
        sb.AppendEntry("power_modifier", z.PowerModifier, false);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (z.EdgeFraction != 0.25f)
            sb.AppendEntry("edge_fraction", z.EdgeFraction);
        sb.AppendEntry("attack_ids", z.AttackIds);
        sb.AppendEntry("mission_type", z.MissionType);
        sb.AppendEntry("lane_id", z.LaneId, false);
        sb.AppendEntry("tradelane_attack", z.TradelaneAttack, false);
        sb.AppendEntry("tradelane_down", z.TradelaneDown, false);
        sb.AppendEntry("sort", z.Sort);
        sb.AppendEntry("vignette_type", z.VignetteType);
        sb.AppendEntry("toughness", z.Toughness, false);
        sb.AppendEntry("density", z.Density, false);
        sb.AppendEntry("repop_time", z.RepopTime, false);
        sb.AppendEntry("max_battle_size", z.MaxBattleSize, false);
        if (z.PopulationAdditive.HasValue)
            sb.AppendEntry("population_additive", z.PopulationAdditive.Value);
        sb.AppendEntry("relief_time", z.ReliefTime, false);
        //population_additive
        sb.AppendEntry("path_label", z.PathLabel);
        sb.AppendEntry("usage", z.Usage);
        foreach (var dr in z.DensityRestrictions)
        {
            sb.AppendEntry("density_restriction", new[] {dr.Count.ToString(), dr.Type});
        }

        foreach (var e in z.Encounters)
        {
            sb.AppendEntry("encounter", new[] {e.Archetype, e.Difficulty.ToString(), e.Chance.ToStringInvariant()});
            foreach (var f in e.FactionSpawns)
            {
                sb.AppendEntry("faction", new[] {f.Faction, f.Chance.ToStringInvariant()});
            }
        }

        if (z.MissionEligible)
            sb.AppendLine("mission_eligible = true");
        sb.AppendEntry("Music", z.Music);
        return sb.ToString();
    }

    public static string SerializeSystemObject(SystemObject obj)
    {
        var sb = new StringBuilder();
        sb.AppendSection("Object")
            .AppendEntry("nickname", obj.Nickname)
            .AppendEntry("ids_name", obj.IdsName, false);
        if (obj.Position != Vector3.Zero)
            sb.AppendEntry("pos", obj.Position);
        if (obj.Rotation != null)
        {
            var rot = obj.Rotation.Value.GetEulerDegrees();
            var ln = rot.Length();
            if (!float.IsNaN(ln) && ln > 0)
                sb.AppendEntry("rotate", new Vector3(rot.Y, rot.X, rot.Z));
        }

        if (obj.AmbientColor != null)
            sb.AppendEntry("ambient_color", obj.AmbientColor.Value);
        sb.AppendEntry(obj.Archetype is Sun ? "star" : "Archetype", obj.Archetype?.Nickname);
        sb.AppendEntry("msg_id_prefix", obj.MsgIdPrefix);
        foreach (var i in obj.IdsInfo)
            sb.AppendEntry("ids_info", i);
        if (obj.Spin != Vector3.Zero)
            sb.AppendEntry("spin", obj.Spin);
        sb.AppendEntry("atmosphere_range", obj.AtmosphereRange, false);
        if (obj.BurnColor != null)
            sb.AppendEntry("burn_color", obj.BurnColor.Value);
        sb.AppendEntry("base", obj.Base);
        if (obj.Dock != null)
        {
            if (obj.Dock.Kind == DockKinds.Base)
            {
                sb.AppendEntry("dock_with", obj.Dock.Target);
            }
            else if (obj.Dock.Kind == DockKinds.Jump)
            {
                sb.Append("goto = ")
                    .Append(obj.Dock.Target)
                    .Append(", ")
                    .Append(obj.Dock.Exit)
                    .Append(", ")
                    .AppendLine(obj.Dock.Tunnel);
            }
            else if (obj.Dock.Kind == DockKinds.Tradelane)
            {
                sb.AppendEntry("prev_ring", obj.Dock.TargetLeft);
                sb.AppendEntry("next_ring", obj.Dock.Target);
            }
        }

        sb.AppendEntry("behavior", obj.Behavior);
        sb.AppendEntry("voice", obj.Voice);
        sb.AppendEntry("space_costume", obj.SpaceCostume);
        sb.AppendEntry("faction", obj.Faction?.Nickname);
        sb.AppendEntry("difficulty_level", obj.DifficultyLevel, false);
        sb.AppendEntry("loadout", obj.Loadout?.Nickname);
        sb.AppendEntry("pilot", obj.Pilot?.Nickname);
        sb.AppendEntry("reputation", obj.Reputation?.Nickname);
        sb.AppendEntry("tradelane_space_name", obj.TradelaneSpaceName, false);
        sb.AppendEntry("parent", obj.Parent);
        sb.AppendEntry("visit", (int) obj.Visit, false);
        return sb.ToString();
    }

    public static string SerializeUniverse(IEnumerable<StarSystem> systems, IEnumerable<Base> bases)
    {
        var sb = new StringBuilder();
        sb.AppendSection("Time")
            .AppendEntry("seconds_per_day", 1800)
            .AppendLine();
        foreach (var b in bases)
        {
            sb.AppendSection("Base")
                .AppendEntry("nickname", b.Nickname)
                .AppendEntry("system", b.System)
                .AppendEntry("strid_name", b.IdsName)
                .AppendEntry("file", b.SourceFile)
                .AppendEntry("BGCS_base_run_by", b.BaseRunBy)
                .AppendEntry("terrain_tiny", b.TerrainTiny)
                .AppendEntry("terrain_sml", b.TerrainSml)
                .AppendEntry("terrain_mdm", b.TerrainMdm)
                .AppendEntry("terrain_lrg", b.TerrainLrg)
                .AppendEntry("terrain_dyna_01", b.TerrainDyna1)
                .AppendEntry("terrain_dyna_02", b.TerrainDyna2);
            if (b.AutosaveForbidden)
                sb.AppendEntry("autosave_forbidden", true);
            sb.AppendLine();
        }
        foreach (var s in systems)
        {
            sb.AppendSection("system")
                .AppendEntry("nickname", s.Nickname)
                .AppendEntry("file", s.SourceFile)
                .AppendEntry("pos", s.UniversePosition)
                .AppendEntry("msg_id_prefix", s.MsgIdPrefix)
                .AppendEntry("visit", (int) s.Visit)
                .AppendEntry("strid_name", s.IdsName)
                .AppendEntry("ids_info", s.IdsInfo)
                .AppendEntry("NavMapScale", s.NavMapScale, false)
                .AppendLine();
        }
        return sb.ToString();
    }

    public static string SerializeRoom(BaseRoom room)
    {
        var sb = new StringBuilder();
        sb.AppendSection("Room_Info")
            .AppendEntry("set_script", room.SetScript?.SourcePath);

        if (!string.IsNullOrWhiteSpace(room.PlayerShipPlacement))
            sb.AppendSection("PlayerShipPlacement")
                .AppendEntry("name", room.PlayerShipPlacement);
        
        if (!string.IsNullOrWhiteSpace(room.Camera))
            sb.AppendSection("Camera")
                .AppendEntry("name", room.Camera)
                .AppendLine();

        foreach (var hotspot in room.Hotspots)
        {
            sb.AppendSection("Hotspot")
                .AppendEntry("name", hotspot.Name)
                .AppendEntry("behavior", hotspot.Behavior)
                .AppendEntry("room_switch", hotspot.Room)
                .AppendEntry("virtual_room", hotspot.VirtualRoom)
                .AppendEntry("set_virtual_room", hotspot.SetVirtualRoom);
        }

        return sb.ToString();
    }

    public static string SerializeBase(Base b)
    {
        var sb = new StringBuilder();
        sb.AppendSection("BaseInfo")
            .AppendEntry("nickname", b.Nickname)
            .AppendEntry("start_room", b.StartRoom.Nickname)
            .AppendLine();

        foreach (var r in b.Rooms)
        {
            sb.AppendSection("Room")
                .AppendEntry("nickname", r.Nickname)
                .AppendEntry("file", r.SourceFile)
                .AppendLine();
        }
        return sb.ToString();
    }
    
    public static string SerializeMBases(IEnumerable<Base> bases)
    {
        var sb = new StringBuilder();
        foreach (var b in bases)
        {
            sb.AppendSection("MBase")
                .AppendEntry("nickname", b.Nickname)
                .AppendEntry("local_faction", b.LocalFaction?.Nickname)
                .AppendEntry("diff", b.Diff)
                .AppendEntry("msg_id_prefix", b.MsgIdPrefix)
                .AppendLine();

            if (b.MinMissionOffers != 0 || b.MaxMissionOffers != 0)
                sb.AppendSection("MVendor")
                    .AppendEntry("num_offers", new[] {b.MinMissionOffers.ToString(), b.MaxMissionOffers.ToString()})
                    .AppendLine();

            foreach (var npc in b.Npcs)
            {
                sb.AppendSection("GF_NPC")
                    .AppendEntry("nickname", npc.Nickname)
                    .AppendEntry("base_appr", npc.BaseAppr)
                    .AppendEntry("body", npc.Body)
                    .AppendEntry("head", npc.Head)
                    .AppendEntry("lefthand", npc.LeftHand)
                    .AppendEntry("righthand", npc.RightHand)
                    .AppendEntry("individual_name", npc.IndividualName)
                    .AppendEntry("affiliation", npc.Affiliation?.Nickname)
                    .AppendEntry("voice", npc.Voice);
                if (npc.Mission != null)
                    sb.AppendEntry("misn", new[]
                    {
                        npc.Mission.Kind,
                        npc.Mission.Min.ToStringInvariant(),
                        npc.Mission.Max.ToStringInvariant()
                    });
                sb.AppendEntry("room", npc.Room);
                foreach (var br in npc.Bribes)
                {
                    sb.AppendEntry("bribe", new[]
                    {
                        br.Faction, br.Ids1.ToString(), br.Ids2.ToString()
                    });
                }
                foreach (var r in npc.Rumors.Where(x => !x.Type2))
                {
                    sb.AppendEntry("rumor", new[]
                    {
                        r.Start, r.End, r.Unknown.ToString(), r.Ids.ToString()
                    });
                    if (r.Objects != null)
                        sb.AppendEntry("rumorknowdb", r.Objects);
                }
                foreach (var r2 in npc.Rumors.Where(x => x.Type2))
                {
                    sb.AppendEntry("rumor_type2", new[]
                    {
                        r2.Start, r2.End, r2.Unknown.ToString(), r2.Ids.ToString()
                    });
                    if (r2.Objects != null)
                        sb.AppendEntry("rumorknowdb", r2.Objects);
                }
                foreach (var k in npc.Know)
                {
                    sb.AppendEntry("know", new[]
                    {
                        k.Ids1.ToString(), k.Ids2.ToString(), k.Price.ToString(), k.Unknown.ToString()
                    });
                    if (k.Objects != null)
                        sb.AppendEntry("knowdb", k.Objects);
                }
                sb.AppendLine();
            }

            foreach (var room in b.Rooms)
            {
                sb.AppendSection("MRoom")
                    .AppendEntry("nickname", room.Nickname)
                    .AppendEntry("character_density", room.MaxCharacters);
                foreach (var npc in room.FixedNpcs)
                {
                    sb.AppendEntry("fixture", new[]
                    {
                        npc.Npc.Nickname, npc.Placement, npc.FidgetScript.SourcePath, npc.Action
                    });
                }
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }
}