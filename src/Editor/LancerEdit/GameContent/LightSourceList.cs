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
    public List<LightSource> Sources = new List<LightSource>();
    private LightSource[] originals;
    public BitArray512 Visible = new BitArray512();

    public event Action<Vector3> OnSelectionChanged;

    public LightSource Selected;
    private SystemEditorTab tab;

    public bool Dirty { get; private set; }

    public LightSourceList(SystemEditorTab tab)
    {
        this.tab = tab;
    }

    public bool HasLight(string nickname) =>
        Sources.Any(x => x.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

    void Sort() => Sources.Sort((x,y) => string.Compare(x.Nickname, y.Nickname, StringComparison.Ordinal));

    public void SetLights(IEnumerable<LightSource> lights)
    {
        Sources = lights.Select(x => x.Clone()).ToList();
        Visible.SetAllTrue();
        Sort();
        originals = Sources.Select(x => x.Clone()).ToArray();
    }

    public void SaveAndApply(StarSystem system)
    {
        Dirty = false;
        originals = Sources.Select(x => x.Clone()).ToArray();
        system.LightSources = originals.ToList();
    }

    public void CheckDirty()
    {
        Dirty = false;
        if (originals.Length != Sources.Count)
        {
            Dirty = true;
            return;
        }

        for (int i = 0; i < Sources.Count; i++)
        {
            if (Sources[i].Nickname != originals[i].Nickname ||
                Sources[i].AttenuationCurveName != originals[i].AttenuationCurveName)
            {
                Dirty = true;
                return;
            }
            if (!Sources[i].Light.Equals(ref originals[i].Light))
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
        for(int i = 0; i < Sources.Count; i++)
        {
            var lt = Sources[i];
            ImGui.PushID(i);
            bool visible = Visible[i];
            Controls.VisibleButton("##ltvis", ref visible);
            Visible[i] = visible;
            ImGui.SameLine();
            if (ImGui.Selectable(lt.Nickname, Selected == lt, ImGuiSelectableFlags.AllowDoubleClick))
            {
                Selected = lt;
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    OnSelectionChanged(lt.Light.Position);
            }
            if (ImGui.BeginPopupContextItem(lt.Nickname))
            {
                if (ImGui.MenuItem("Delete"))
                {
                    actions.Add(new SysLightRemove(lt, this));
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
