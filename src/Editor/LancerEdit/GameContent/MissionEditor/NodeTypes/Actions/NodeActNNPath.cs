using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class NodeActNNPath : TriggerEntryNode
{
    protected override string Name => "Set NN Path";

    public readonly Act_NNPath Data;
    public NodeActNNPath(ref int id, MissionAction action) : base(ref id, NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NNPath(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Object", ref Data.ObjectId);
        nodePopups.StringCombo("System", Data.SystemId, s => Data.SystemId = s, gameData.SystemsByName);
        Controls.IdsInputString("IDS 1", gameData, popup, ref Data.Ids1, (ids) => Data.Ids1 = ids);
        Controls.IdsInputString("IDS 2", gameData, popup, ref Data.Ids2, (ids) => Data.Ids2 = ids);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
