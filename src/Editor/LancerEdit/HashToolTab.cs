using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;


namespace LancerEdit;

public class HashToolTab : EditorTab
{
    private string outputs = "";
    public HashToolTab()
    {
        Title = "Hash Tool";
    }

    private int selected = 0;
    private string inputText = "";
    public override void Draw(double elapsed)
    {
        ImGui.PushItemWidth(-1);
        if (ImGui.InputText("##input", ref inputText, 250, ImGuiInputTextFlags.EnterReturnsTrue))
            AddHash();
        ImGui.PopItemWidth();
        ImGui.BeginGroup();
        ImGui.RadioButton("CreateID", ref selected, 0);
        ImGui.SameLine();
        ImGui.RadioButton("FLFacHash", ref selected, 1);
        ImGui.SameLine();
        ImGui.RadioButton("FLModelCrc", ref selected, 2);
        ImGui.SameLine();
        ImGui.RadioButton("FLAleCrc", ref selected, 3);
        ImGui.EndGroup();
        if(ImGui.Button("Hash"))
            AddHash();
        var sz = Vector2.Max(ImGui.GetContentRegionAvail() - ImGui.GetStyle().FramePadding * 3, Vector2.Zero);
        ImGui.InputTextMultiline("##outputs", ref outputs, 4U * 1024 * 1024, sz,
            ImGuiInputTextFlags.ReadOnly);
    }

    void AddHash()
    {
        outputs += $"'{inputText}'\n";
        switch (selected)
        {
            case 0:
            {
                var h = FLHash.CreateID(inputText);
                outputs += $"CreateID: {(int)h}\nUnsigned: {h}\nHex: {h:X8}\n\n";
                break;
            }
            case 1:
            {
                var h = FLHash.FLFacHash(inputText);
                outputs += $"FLFacHash: {(int)h}\nUnsigned: {h}\nHex: {h:X8}\n\n";
                break;
            }
            case 2:
            {
                var h = CrcTool.FLModelCrc(inputText);
                outputs += $"FLModelCrc: {(int)h}\nUnsigned: {h}\nHex: {h:X8}\n\n";
                break;
            }
            case 3:
            {
                var h = CrcTool.FLAleCrc(inputText);
                outputs += $"FLAleCrc: {(int)h}\nUnsigned: {h}\nHex: {h:X8}\n\n";
                break;
            }
        }
    }
}
