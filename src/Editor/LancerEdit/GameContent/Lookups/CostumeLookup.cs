using ImGuiNET;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.Lookups;

public class CostumeLookup
{
    public BodypartLookup Head;
    public BodypartLookup Body;
    public AccessoryLookup Accessory;

    public CostumeLookup(string id, GameDataContext gd, CostumeEntry initial)
    {
        Head = new($"{id}.HEAD", gd, initial?.Head);
        Body = new($"{id}.BODY", gd, initial?.Body);
        Accessory = new($"{id}.ACC", gd, initial?.Accessory);
    }

    public void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Costume Head");
        ImGui.SameLine();
        Head.Draw();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Costume Body");
        ImGui.SameLine();
        Body.Draw();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Costume Accessory");
        ImGui.SameLine();
        Accessory.Draw();
    }
}
