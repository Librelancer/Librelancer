using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.World;
using LibreLancer.ImUI;
using LibreLancer.World;

namespace LancerEdit.GameContent;

public enum ZoneDisplayKind
{
    Normal,
    ExclusionZone,
    AsteroidField,
    Nebula
}
public class ZoneList : IDisposable
{
    public List<EditZone> Zones = new List<EditZone>();
    public Dictionary<string, Zone> ZonesByName = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
    public ZoneLookup ZonesByPosition;
    public AsteroidFieldList AsteroidFields = new AsteroidFieldList();
    public List<Nebula> Nebulae = new List<Nebula>();

    public EditZone Selected;
    public EditZone HoveredZone;

    private bool dirtyOrder = false;
    private bool dirtyZones = false;

    public bool Dirty => dirtyOrder || dirtyZones;


    Dictionary<string,ZoneDisplayKind> zoneTypes = new Dictionary<string, ZoneDisplayKind>();


    public void SaveAndApply(StarSystem system, GameDataManager gameData)
    {
        system.Zones = new List<Zone>();
        system.ZoneDict = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
        foreach (var z in Zones)
        {
            var cloned = z.Current.Clone();
            z.Original = cloned;
            system.Zones.Add(cloned);
            system.ZoneDict[cloned.Nickname] = cloned;
        }
        AsteroidFields.SaveAndApply(system, gameData);
        system.Nebulae = Nebulae.Select(x => x.Clone(system.ZoneDict)).ToList();
        dirtyOrder = false;
        dirtyZones = false;
    }

    public bool ZoneExists(string name) =>
        Zones.Any(x => x.Current.Nickname.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void RemoveZone(EditZone z)
    {
        //Remove from Asteroid Fields
        for (int i = 0; i < AsteroidFields.Fields.Count; i++) {
            if (AsteroidFields.Fields[i].Zone == z.Current)
            {
                AsteroidFields.Fields.RemoveAt(i);
                break;
            }
            for (int j = AsteroidFields.Fields[i].ExclusionZones.Count - 1; j >= 0; j--){
                if (AsteroidFields.Fields[i].ExclusionZones[j].Zone == z.Current){
                    AsteroidFields.Fields[i].ExclusionZones.RemoveAt(j);
                }
            }
        }
        //Remove from Nebulae
        for (int i = 0; i < Nebulae.Count; i++) {
            if (Nebulae[i].Zone == z.Current)
            {
                Nebulae.RemoveAt(i);
                break;
            }
            for (int j = Nebulae[i].ExclusionZones.Count - 1; j >= 0; j--){
                if (Nebulae[i].ExclusionZones[j].Zone == z.Current){
                    Nebulae[i].ExclusionZones.RemoveAt(j);
                }
            }
        }
        //Remove from Zones
        Zones.Remove(z);
        ZonesByPosition.RemoveZone(z.Current);
        dirtyOrder = true;
    }

    public EditZone AddZone(Zone z)
    {
        var ez = new EditZone() { Current = z, Visible = true };
        dirtyOrder = true;
        Zones.Add(ez);
        ZonesByPosition.AddZone(ez.Current);
        return ez;
    }


    public void CheckDirty()
    {
        if (dirtyOrder) return;
        dirtyZones = false;
        if (AsteroidFields.CheckDirty()) {
            dirtyZones = true;
            return;
        }
        foreach (var z in Zones)
        {
            if (z.CheckDirty()) {
                dirtyZones = true;
                break;
            }
        }
    }

    public bool HasZone(string nickname) =>
        Zones.Any(x => x.Current.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

    public void ZoneRenamed(Zone z, string oldNick)
    {
        ZonesByName.Remove(oldNick);
        ZonesByName[z.Nickname] = z;
    }

    public void SetZones(List<Zone> zones, List<AsteroidField> asteroidFields, List<Nebula> nebulae)
    {
        ZonesByPosition?.Dispose();
        dirtyOrder = dirtyZones = false;
        Zones = zones.Select(x => new EditZone(x)).ToList();
        ZonesByPosition = new ZoneLookup(Zones.Select(x => x.Current));
        ZonesByName = new Dictionary<string, Zone>(StringComparer.OrdinalIgnoreCase);
        foreach (var z in Zones)
            ZonesByName[z.Current.Nickname] = z.Current;

        AsteroidFields.SetFields(asteroidFields, ZonesByName);
        Nebulae = nebulae.Select(x => x.Clone(ZonesByName)).ToList();


        //asteroid fields
        foreach (var ast in AsteroidFields.Fields)
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
            z.Visible = true;
    }

    public void HideAll()
    {
        foreach (var z in Zones)
            z.Visible = false;
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
            ImGui.PushID(i);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One);
            Controls.VisibleButton(z.Current.Nickname, ref z.Visible);
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            if(ImGuiExt.Button(Icons.ArrowUp, i != 0))
                idxUp = i;
            ImGui.SameLine();
            if (ImGuiExt.Button(Icons.ArrowDown, i != Zones.Count - 1))
                idxDown = i;
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
            ImGui.SameLine();
            if (ImGui.Selectable(z.Current.Nickname, Selected == z)) {
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

    public void Dispose()
    {
        ZonesByPosition?.Dispose();
    }
}
