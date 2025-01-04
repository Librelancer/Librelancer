using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndCharacterSelect : BlueprintNode
{
    protected override string Name => "On Character Select";

    private string character = string.Empty;
    private string location = string.Empty;
    private string @base = string.Empty;
    public NodeCndCharacterSelect(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry.Count >= 3)
        {
            character = entry[0].ToString();
            location = entry[1].ToString();
            @base = entry[2].ToString();
        }

        Inputs.Add(new NodePin(id++, this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        Controls.InputTextId("Character", ref character);
        Controls.InputTextId("Location", ref location);
        Controls.InputTextId("Base", ref @base);
    }
}
