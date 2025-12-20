using System.IO;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Schema.Save;
using LibreLancer.Data.Schema.Save;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent;

public class SaveGameTab : EditorTab
{
    private string saveGameText;

    public SaveGameTab(string filename)
    {
        using (var reader = new StreamReader(new MemoryStream(FlCodec.ReadFile(filename))))
        {
            saveGameText = reader.ReadToEnd();
        }

        Title = Path.GetFileName(filename);
    }

    public override void Draw(double elapsed)
    {
        ImGui.PushFont(ImGuiHelper.SystemMonospace, 0);
        var avail = ImGui.GetContentRegionAvail();
        var sz = new Vector2(avail.X - ImGuiHelper.Scale * 8, avail.Y - ImGuiHelper.Scale * 16);
        ImGui.InputTextMultiline("##text", ref saveGameText, (uint)saveGameText.Length, sz,
            ImGuiInputTextFlags.ReadOnly);
        ImGui.PopFont();
    }
}
