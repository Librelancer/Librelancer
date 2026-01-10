using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent;

public class LightSourceList
{
    public SortedDictionary<string, LightSource> Sources = new(StringComparer.OrdinalIgnoreCase);
    private SortedDictionary<string, LightSource> originals;
    private HashSet<string> hidden = new(StringComparer.OrdinalIgnoreCase);
    public event Action<Vector3> OnSelectionChanged;

    public LightSource Selected;
    private SystemEditorTab tab;

    public bool Dirty { get; private set; }

    public LightSourceList(SystemEditorTab tab)
    {
        this.tab = tab;
    }

    public bool HasLight(string nickname) => Sources.ContainsKey(nickname);


    public void SetLights(IEnumerable<LightSource> lights)
    {
        Sources = new SortedDictionary<string, LightSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var l in lights)
        {
            Sources[l.Nickname] = l.Clone();
        }
        hidden = new(StringComparer.OrdinalIgnoreCase);
        originals = new(StringComparer.OrdinalIgnoreCase);
        foreach (var l in Sources)
            originals[l.Key] = l.Value.Clone();
    }

    public void SaveAndApply(StarSystem system)
    {
        Dirty = false;
        originals = new(StringComparer.OrdinalIgnoreCase);
        foreach (var l in Sources)
            originals[l.Key] = l.Value.Clone();
        system.LightSources = originals.Values.ToList();
    }

    public void CheckDirty()
    {
        Dirty = false;
        if (originals.Count != Sources.Count)
        {
            Dirty = true;
            return;
        }
        foreach (var s in Sources)
        {
            if (!originals.TryGetValue(s.Key, out var original))
            {
                Dirty = true;
                return;
            }

            if (s.Value.AttenuationCurveName != original.AttenuationCurveName ||
                !s.Value.Light.Equals(ref original.Light))
            {
                Dirty = true;
                return;
            }
        }
    }

    public void Draw()
    {
        ImGui.BeginChild("##lightlist");
        var actions = new List<EditorAction>();
        int i = 0;
        foreach(var light in Sources)
        {
            // Use light.Key to not display temporary editing value
            var lt = light.Value;
            ImGui.PushID(light.Key);
            bool vis = !lt.Disabled;
            Controls.VisibleButton("##ltvis", ref vis);
            lt.Disabled = !vis;
            ImGui.SameLine();
            if (ImGui.Selectable(light.Key, Selected == lt, ImGuiSelectableFlags.AllowDoubleClick))
            {
                Selected = lt;
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    OnSelectionChanged(lt.Light.Position);
            }
            if (ImGui.BeginPopupContextItem(lt.Nickname))
            {
                if (ImGui.MenuItem("Delete"))
                {
                    actions.Add(new DictionaryRemove<LightSource>("Light", Sources, lt, () => ref Selected));
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
        foreach(var x in actions)
            tab.UndoBuffer.Commit(x);
        ImGui.EndChild();
    }
}
