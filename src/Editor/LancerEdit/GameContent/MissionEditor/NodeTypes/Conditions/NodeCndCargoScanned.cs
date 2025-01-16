using System;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;
using LibreLancer.ImUI.NodeEditor;
using LibreLancer.Ini;

namespace LancerEdit.GameContent.MissionEditor.NodeTypes.Conditions;

public class NodeCndCargoScanned : BlueprintNode
{

    protected override string Name => "On Cargo Scanned";

    private string scanningShip = string.Empty;
    private string scannedShip = string.Empty;

    public NodeCndCargoScanned(ref int id, Entry entry) : base(ref id, NodeColours.Condition)
    {
        if (entry?.Count >= 2)
        {
            scanningShip = entry[0].ToString();
            scannedShip = entry[1].ToString();
        }

        Inputs.Add(new NodePin(this, LinkType.Condition, PinKind.Input));
    }

    protected override void RenderContent(GameDataContext gameData, PopupManager popup, MissionIni missionIni)
    {
        // TODO: transform this into a combobox of different ships or a object depending on type
        Controls.InputTextId("Scanning Ship", ref scanningShip);
        Controls.InputTextId("Scanned Ship", ref scannedShip);
    }
}
