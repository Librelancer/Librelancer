using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActPlayerEnemyClamp : NodeTriggerEntry
{
    public override string Name => "Clamp Amount of Enemies Attacking Player";

    public readonly Act_PlayerEnemyClamp Data;
    public ActPlayerEnemyClamp(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayerEnemyClamp(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.InputInt("Min", ref Data.Min, 1, 10);
        ImGui.InputInt("Max", ref Data.Max, 1, 10);

        Data.Min = Math.Clamp(Data.Min, 0, Data.Max);
        Data.Max = Math.Clamp(Data.Max, Data.Min, 100);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
