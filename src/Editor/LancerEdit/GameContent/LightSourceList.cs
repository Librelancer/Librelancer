using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.GameData.World;

namespace LancerEdit.GameContent;

public class LightSourceList
{
    public List<LightSource> Sources = new List<LightSource>();

    public event Action<Vector3> OnMoveCamera;

    public LightSource Selected;
    private SystemEditorTab tab;

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
        Sort();
    }

    public void Draw()
    {
        ImGui.BeginChild("##lightlist");
        int i = 0;
        var actions = new List<EditorAction>();
        foreach (var lt in Sources)
        {
            ImGui.PushID(i++);
            if (ImGui.Selectable(lt.Nickname, Selected == lt, ImGuiSelectableFlags.AllowDoubleClick))
            {
                Selected = lt;
                if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    OnMoveCamera(lt.Light.Position);
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
