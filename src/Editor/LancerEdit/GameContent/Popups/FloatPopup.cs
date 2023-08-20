using System;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit;

public class FloatPopup : PopupWindow
{
    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private float existing;
    private float current;
    private float min;
    private Action<float, bool> set;
    private bool setOk = false;

    public override void Draw()
    {
        float last = current;
        ImGui.PushItemWidth(150 * ImGuiHelper.Scale);
        ImGui.InputFloat("##value",  ref current);
        if (current < min) current = min;
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

    public FloatPopup(string title, float existing, Action<float, bool> set, float min = float.MinValue)
    {
        Title = title;
        this.existing = this.current = existing;
        this.min = min;
        this.set = set;
    }
    
}