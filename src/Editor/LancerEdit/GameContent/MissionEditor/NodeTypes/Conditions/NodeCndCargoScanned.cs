using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;
using LibreLancer.Missions.Conditions;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndCargoScanned : TriggerEntryNode
{

    protected override string Name => "On Cargo Scanned";

    public Cnd_CargoScanned Data;
    public NodeCndCargoScanned(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        Data = entry is null ? new() : new(entry);

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, ref NodePopups nodePopups,
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
