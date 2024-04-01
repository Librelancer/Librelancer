using System;
using ImGuiNET;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.Popups;

public class FloatPopup : PopupWindow
{
    public delegate void SetValue(float old, float updated);

    public delegate void PreviewValue(float value);

    public override string Title { get; set; }
    public override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.AlwaysAutoResize;

    private float existing;
    private float current;
    private float min;
    private SetValue set;
    private PreviewValue preview;
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
            preview?.Invoke(current);
        if(ImGui.Button("Ok"))
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(existing != current)
                set(existing, current);
            setOk = true;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            preview?.Invoke(existing);
            ImGui.CloseCurrentPopup();
        }
    }

    public override void OnClosed()
    {
        if (!setOk)
            preview?.Invoke(existing);
    }

    public FloatPopup(string title, float existing, SetValue set, PreviewValue preview = null, float min = float.MinValue)
    {
        Title = title;
        this.existing = this.current = existing;
        this.preview = preview;
        this.min = min;
        this.set = set;
    }

}
