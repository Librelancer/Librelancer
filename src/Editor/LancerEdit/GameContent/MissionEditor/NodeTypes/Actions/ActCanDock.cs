﻿using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions;
using LibreLancer.Missions.Actions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Actions;

public sealed class ActCanDock : NodeTriggerEntry
{
    public override string Name => "Toggle Player Docking Ability";

    public readonly Act_PlayerCanDock Data;
    public ActCanDock(MissionAction action): base( NodeColours.Action)
    {
        Data = action is null ? new() : new Act_PlayerCanDock(action);

        Inputs.Add(new NodePin(this, LinkType.Action, PinKind.Input));
    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        ImGui.Checkbox("Can Dock", ref Data.CanDock);
        Controls.InputStringList("Exceptions", Data.Exceptions);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
