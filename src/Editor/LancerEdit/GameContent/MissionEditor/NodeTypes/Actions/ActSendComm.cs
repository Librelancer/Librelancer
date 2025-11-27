using System;
using System.Linq;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActSendComm : NodeTriggerEntry
{
    public override string Name => "Send Comm";

    public readonly Act_SendComm Data;
    public ActSendComm(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_SendComm(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        Controls.InputTextId("Source", ref Data.Source);
        Controls.InputTextId("Destination", ref Data.Destination);
        Controls.InputTextId("Line", ref Data.Line);

        if (!ImGui.Button(Icons.Play + " Play Line"))
        {
            return;
        }

        var source = lookups.MissionIni.Ships.FirstOrDefault(x => x.Nickname.Equals(Data.Source, StringComparison.OrdinalIgnoreCase));

        if (source is not null)
        {
            var npc = lookups.MissionIni.NPCs.FirstOrDefault(x => x.Nickname.Equals(source.NPC, StringComparison.OrdinalIgnoreCase));

            if (npc is not null)
            {
                gameData.Sounds.PlayVoiceLine(npc.Voice, FLHash.CreateID(Data.Line));
            }

            return;
        }

        var source2 = lookups.MissionIni.Solars.FirstOrDefault(x => x.Nickname.Equals(Data.Source, StringComparison.OrdinalIgnoreCase));

        if (source2 != null)
        {
            gameData.Sounds.PlayVoiceLine(source2.Voice, FLHash.CreateID(Data.Line));
        }
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
