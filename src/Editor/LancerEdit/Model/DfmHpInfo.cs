using ImGuiNET;
using LibreLancer;
using LibreLancer.ImUI;
using LibreLancer.Utf.Dfm;

namespace LancerEdit;

public class DfmHpInfo : PopupWindow
{
    private DfmHardpointDef hp;
    public DfmHpInfo(DfmHardpointDef hp)
    {
        this.hp = hp;
    }

    public override string Title
    {
        get => $"{hp.Hp.Name} ({hp.Part.objectName})";
        set { }
    }

    public override void Draw()
    {
        ImGui.Text("Hardpoint Information\n---");
        ImGui.Text($"Translation: {hp.Hp.Position}");
        ImGui.Text("Rotation");
        ImGui.Text($"{hp.Hp.Orientation.ToString().Replace("} {", "}\n{")}");
        ImGui.Text("Rotation (Euler)");
        ImGui.Text($"{hp.Hp.Orientation.GetEulerDegrees()}");
        if(ImGui.Button("OK"))
            ImGui.CloseCurrentPopup();
    }
}
