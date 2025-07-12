using System;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public enum SetActionKind
{
    Commit,
    Preview,
    Revert
}

public class Vector3Popup : PopupWindow
{
    public delegate void SetValueAction(Vector3 value, SetActionKind kind);

    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private Vector3 existing;
    private Vector3 current;
    private SetValueAction set;
    private bool setOk = false;
    private bool angle;

    static void WrapAngle(ref float angle)
    {
        if (angle >= -180 && angle <= 180)
            return;
        angle = (angle + 180) % 360;
        if (angle < 0)
            angle += 360;
        angle -= 180;
    }

    public override void Draw(bool appearing)
    {
        Vector3 last = current;
        ImGui.PushItemWidth(150 * ImGuiHelper.Scale);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("X: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueX", ref current.X);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Y: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueY",  ref current.Y);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Z: ");
        ImGui.SameLine();
        ImGui.InputFloat("##valueZ",  ref current.Z);
        ImGui.PopItemWidth();

        if (angle) {
            WrapAngle(ref current.X);
            WrapAngle(ref current.Y);
            WrapAngle(ref current.Z);
        }
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (last != current)
            set(current, SetActionKind.Preview);
        if(ImGui.Button("Ok"))
        {
            set(current, SetActionKind.Commit);
            setOk = true;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (angle)
        {
            if (ImGui.Button("Zero"))
            {
                setOk = true;
                set(Vector3.Zero, SetActionKind.Commit);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
        }
        if (ImGui.Button("Cancel"))
        {
            setOk = true;
            set(existing, SetActionKind.Revert);
            ImGui.CloseCurrentPopup();
        }
    }

    public override void OnClosed()
    {
        if (!setOk)
            set(existing, SetActionKind.Revert);
    }

    public Vector3Popup(string title, bool angle, Vector3 existing, SetValueAction set)
    {
        Title = title;
        this.existing = this.current = existing;
        this.set = set;
        this.angle = angle;
    }

}
