using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class Vector3Popup : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Vector3 existing;
    private Vector3 current;
    private Action<Vector3, bool> set;
    private bool setOk = false;

    public override void Draw()
    {
        Vector3 last = current;
        ImGui.PushItemWidth(150 * ImGuiHelper.Scale);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("X: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueX",  ref current.X);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Y: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueY",  ref current.Y);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Z: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueZ",  ref current.Z);
        ImGui.PopItemWidth();
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (last != current)
            set(current, false);
        if(ImGui.Button("Ok"))
        {
            set(current, true);
            setOk = true;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            set(existing, false);
            ImGui.CloseCurrentPopup();
        }
    }

    public override void OnClosed()
    {
        if (!setOk)
            set(existing, false);
    }

    public Vector3Popup(string title, Vector3 existing, Action<Vector3, bool> set)
    {
        Title = title;
        this.existing = this.current = existing;
        this.set = set;
    }
    
}