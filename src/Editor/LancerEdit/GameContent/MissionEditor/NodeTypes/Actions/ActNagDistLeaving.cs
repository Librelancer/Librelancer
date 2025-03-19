using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActNagDistLeaving : NodeTriggerEntry
{
    public override string Name => "Nag Dist Leaving";

    private int radioIndex = 0;
    public readonly Act_NagDistLeaving Data;
    public ActNagDistLeaving(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_NagDistLeaving(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    private static readonly string[] _nags = Enum.GetValues<NagType>().Select(x => x.ToString()).Append("NULL").ToArray();
    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.RadioButton("Point", ref radioIndex, 0);
        ImGui.RadioButton("Object", ref radioIndex, 1);

        Controls.InputTextId("Nickname", ref Data.Nickname);
        Controls.InputTextId("Nagger", ref Data.Nagger);
        Controls.IdsInputString("Mission Fail IDS", gameData, popup, ref Data.MissionFailIds, (ids) => Data.MissionFailIds = ids);
        ImGui.InputFloat("Distance", ref Data.Distance);

        var index = Data.NagType is null ? _nags.Length - 1 : (int)Data.NagType;
        nodePopups.Combo("Nag Type", index, i =>
        {
            if (i == _nags.Length - 1)
            {
                Data.NagType = null;
            }
            else
            {
                Data.NagType = (NagType)i;
            }
        }, _nags);

        if (radioIndex == 0)
        {
            Data.Target = "";
            ImGui.InputFloat3("Position", ref Data.Position);
        }
        else
        {
            Controls.InputTextId("Target", ref Data.Target);
        }
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
