using ImGuiNET;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.ImUI;

namespace LancerEdit.Utf.Popups;

public class HexEditorPopup : PopupWindow
{
    public override string Title { get; set; } = "Hex Editor";

    private byte[] data;
    private MemoryEditor mem;
    private LUtfNode node;

    public HexEditorPopup(LUtfNode node)
    {
        this.node = node;
        data = node.Data.ShallowCopy();
        mem = new MemoryEditor();
    }

    public override void Draw(bool appearing)
    {
        ImGui.SameLine(ImGui.GetWindowWidth() - 90 * ImGuiHelper.Scale);
        if (ImGui.Button("Ok"))
        {
            node.Data = data;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine(ImGui.GetWindowWidth() - 60 * ImGuiHelper.Scale);
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.PushFont(ImGuiHelper.SystemMonospace);
        mem.DrawContents(data, data.Length);
        ImGui.PopFont();
    }

    public override void OnClosed()
    {
        mem.Dispose();
    }
}
