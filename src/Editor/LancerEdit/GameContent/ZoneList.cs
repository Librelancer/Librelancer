using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.GameData.World;
using LibreLancer.ImUI;

namespace LancerEdit;

public enum ZoneDisplayKind
{
    Normal,
    ExclusionZone,
    AsteroidField,
    Nebula
}
public class ZoneList
{
    public List<Zone> Zones = new List<Zone>();
    public List<AsteroidField> AsteroidFields = new List<AsteroidField>();
    public List<Nebula> Nebulae = new List<Nebula>();

    private Dictionary<string, Zone> zoneDict;

    public HashSet<string> VisibleZones = new HashSet<string>();

    public Zone Selected;
    public Zone HoveredZone;

    private Dictionary<Zone, bool> dirtyZones = new Dictionary<Zone, bool>();

    private bool dirtyOrder = false;

    public bool Dirty => dirtyOrder || dirtyZones.Count > 0;


    Dictionary<string,ZoneDisplayKind> zoneTypes = new Dictionary<string, ZoneDisplayKind>();

    public void ApplyZones(StarSystem system)
    {
        system.Zones = Zones.Select(x => x.Clone()).ToList();
        system.ZoneDict = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
        foreach (var z in Zones)
            system.ZoneDict[z.Nickname] = z;
        system.AsteroidFields = AsteroidFields.Select(x => x.Clone(system.ZoneDict)).ToList();
        system.Nebulae = Nebulae.Select(x => x.Clone(system.ZoneDict)).ToList();
        
        dirtyOrder = false;
        dirtyZones = new Dictionary<Zone, bool>();
    }

    public void ResetZone(Zone z)
    {
        
    }

    public void RenameZone(Zone z, string newNickname)
    {
        if (VisibleZones.Contains(z.Nickname))
        {
            VisibleZones.Remove(z.Nickname);
            VisibleZones.Add(newNickname);
        }
        zoneDict.Remove(z.Nickname);
        zoneDict[newNickname] = z;
        z.Nickname = newNickname;
        SetZoneDirty(z);
    }

    public bool HasZone(string nickname) => zoneDict.ContainsKey(nickname);

    public void SetZones(List<Zone> zones, List<AsteroidField> asteroidFields, List<Nebula> nebulae)
    {
        dirtyZones = new Dictionary<Zone, bool>();
        Zones = zones.Select(x => x.Clone()).ToList();
        zoneDict = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
        foreach (var z in Zones)
            zoneDict[z.Nickname] = z;

        AsteroidFields = asteroidFields.Select(x => x.Clone(zoneDict)).ToList();
        Nebulae = nebulae.Select(x => x.Clone(zoneDict)).ToList();
        
        
        //asteroid fields
        foreach (var ast in AsteroidFields)
        {
            zoneTypes[ast.Zone.Nickname] = ZoneDisplayKind.AsteroidField;
            foreach (var ex in ast.ExclusionZones)
            {
                zoneTypes[ex.Zone.Nickname] = ZoneDisplayKind.ExclusionZone;
            }
        }

        //nebulae
        foreach (var neb in Nebulae)
        {
            zoneTypes[neb.Zone.Nickname] = ZoneDisplayKind.Nebula;
            foreach (var ex in neb.ExclusionZones)
            {
                zoneTypes[ex.Zone.Nickname] = ZoneDisplayKind.ExclusionZone;
            }
        }
    }

    public bool IsZoneDirty(Zone z) => dirtyZones.TryGetValue(z, out _);

    public void SetZoneDirty(Zone z) => dirtyZones[z] = true;

    public ZoneDisplayKind GetZoneType(string nickname)
    {
        if (zoneTypes.TryGetValue(nickname, out var d))
        {
            return d;
        }
        return ZoneDisplayKind.Normal;
    }


    public void ShowAll()
    {
        foreach (var z in Zones)
            VisibleZones.Add(z.Nickname);
    }

    public void HideAll()
    {
        VisibleZones = new HashSet<string>();
    }

    public void Draw()
    {
        ImGui.BeginChild("##zones");
        HoveredZone = null;
        int idxUp = -1;
        int idxDown = -1;
        for (int i = 0; i < Zones.Count; i++)
        {
            var z = Zones[i];
            ImGui.PushID(z.Nickname);
            var contains = VisibleZones.Contains(z.Nickname);
            var v = contains;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One);
            Controls.VisibleButton(z.Nickname, ref v);
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            if(ImGuiExt.Button(Icons.ArrowUp.ToString(), i != 0))
                idxUp = i;
            ImGui.SameLine();
            if (ImGuiExt.Button(Icons.ArrowDown.ToString(), i != Zones.Count - 1))
                idxDown = i;
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.SameLine();
            if (ImGui.Selectable(z.Nickname, Selected == z)) {
                Selected = z;
            }
            if (ImGui.IsItemHovered())
                HoveredZone = z;
            if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
            {
                int n_next = i + (ImGui.GetMouseDragDelta(0).Y < 0 ? -1 : 1);
                if (n_next >= 0 && n_next < Zones.Count) {
                    Zones[i] = Zones[n_next];
                    Zones[n_next] = z;
                    ImGui.ResetMouseDragDelta();
                    dirtyOrder = true;
                }
            }
            if (v != contains)
            {
                if (contains) VisibleZones.Remove(z.Nickname);
                else VisibleZones.Add(z.Nickname);
            }
            ImGui.PopID();
        }
        ImGui.EndChild();
        if (idxUp != -1)
        {
            (Zones[idxUp], Zones[idxUp - 1]) = (Zones[idxUp - 1], Zones[idxUp]);
            dirtyOrder = true;
        }
        if (idxDown != -1)
        {
            (Zones[idxDown], Zones[idxDown + 1]) = (Zones[idxDown + 1], Zones[idxDown]);
            dirtyOrder = true;
        }
    }
    
}