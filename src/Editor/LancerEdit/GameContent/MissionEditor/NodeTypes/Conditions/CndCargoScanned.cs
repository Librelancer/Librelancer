using System;
using ImGuiNET;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class CndCargoScanned : NodeTriggerEntry
{

    public override string Name => "On Cargo Scanned";

    public Cnd_CargoScanned Data;
    public CndCargoScanned(Entry entry): base(NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

    }

    public override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
        ref NodeLookups lookups)
    {
        // TODO: transform this into a combobox of different ships or a object depending on type
        Controls.InputTextId("Scanning Ship", ref Data.scanningShip);
        Controls.InputTextId("Scanned Ship", ref Data.scannedShip);
    }

    public override void WriteEntry(IniBuilder.IniSectionBuilder sectionBuilder)
    {
        Data.Write(sectionBuilder);
    }
}
