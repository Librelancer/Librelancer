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
        Head.Draw("Head");
        Body.Draw("Body");
        Accessory.Draw("Accessory");
    }
}
