using System;
using System.Linq;
using ImGuiNET;
using LibreLancer.ContentEdit;
using LibreLancer.Data.Missions;
using LibreLancer.Dialogs;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private void RenderLeftSidebar()
    {
        ImGuiExt.SeparatorText("Mission Information");
        RenderMissionInformation();

        ImGui.NewLine();
        ImGuiExt.SeparatorText("Ship Archs");

        RenderNpcArchManager();

        ImGui.NewLine();
        ImGuiExt.SeparatorText("NPC Management");

        RenderNpcManagement();
    }

    private int selectedNpcIndex = -1;
    private void RenderNpcManagement()
    {
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New NPC"))
        {
            selectedNpcIndex = ini.NPCs.Count;
            ini.NPCs.Add(new MissionNPC());
        }

        ImGui.BeginDisabled(selectedNpcIndex == -1);
        if (ImGui.Button("Delete NPC"))
        {
            win.Confirm("Are you sure you want to delete this NPC?", () =>
            {
                ini.NPCs.RemoveAt(selectedNpcIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedNpc = selectedNpcIndex != -1 ? ini.NPCs[selectedNpcIndex] : null;
        if (ImGui.BeginCombo("NPCs", selectedNpc is not null ? selectedNpc.Nickname : ""))
        {
            for (var index = 0; index < ini.NPCs.Count; index++)
            {
                var arch = ini.NPCs[index];
                var selected = arch == selectedNpc;
                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedNpcIndex = index;
                selectedNpc = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedNpc is null)
        {
            return;
        }

        ImGui.PushID(selectedNpcIndex);

        Controls.InputTextId("NPC Nickname##ID", ref selectedNpc.Nickname);
        Controls.InputTextId("Archetype##ID", ref selectedNpc.NpcShipArch);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            ini.ShipIni.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch)
            || gameData.GameData.Ini.NPCShips.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch));

        Controls.IdsInputString("Name##ID", gameData, popup, ref selectedNpc.IndividualName, x => selectedNpc.IndividualName = x);
        Controls.InputTextId("Affiliation##ID", ref selectedNpc.Affiliation);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Factions.Any(x => x.Nickname == selectedNpc.Affiliation));

        Controls.InputTextId("Costume Head##ID", ref selectedNpc.SpaceCostume[0]);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[0]) is not null);
        Controls.InputTextId("Costume Body##ID", ref selectedNpc.SpaceCostume[1]);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[1]) is not null);
        Controls.InputTextId("Costume Accessory##ID", ref selectedNpc.SpaceCostume[2]);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Ini.Bodyparts.FindAccessory(selectedNpc.SpaceCostume[2]) is not null);

        ImGui.PopID();
    }

    private void RenderNpcArchManager()
    {
        var ini = missionScript.Ini;
        if (ImGui.Button("Create New Ship Arch"))
        {
            selectedArchIndex = ini.ShipIni.ShipArches.Count;
            ini.ShipIni.ShipArches.Add(new NPCShipArch());
        }

        ImGui.BeginDisabled(selectedArchIndex == -1);
        if (ImGui.Button("Delete Ship Arch"))
        {
            win.Confirm("Are you sure you want to delete this ship arch?", () =>
            {
                ini.ShipIni.ShipArches.RemoveAt(selectedArchIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedArch = selectedArchIndex != -1 ? ini.ShipIni.ShipArches[selectedArchIndex] : null;
        if (ImGui.BeginCombo("Ship Archs", selectedArch is not null ? selectedArch.Nickname : ""))
        {
            for (var index = 0; index < ini.ShipIni.ShipArches.Count; index++)
            {
                var arch = ini.ShipIni.ShipArches[index];
                var selected = arch == selectedArch;
                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedArchIndex = index;
                selectedArch = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedArch is null)
        {
            return;
        }

        ImGui.PushID(selectedArchIndex);

        Controls.InputTextId("Arch Nickname##ID", ref selectedArch.Nickname);
        Controls.InputTextId("Loadout##ID", ref selectedArch.Loadout);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Loadouts.Any(x => x.Nickname == selectedArch.Loadout));

        ImGui.InputInt("Level##ID", ref selectedArch.Level, 1, 10);
        Controls.InputTextId("Pilot##ID", ref selectedArch.Pilot);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.GetPilot(selectedArch.Pilot) is not null);

        string[] stateGraphs = { "NOTHING", "FIGHTER", "FREIGHTER", "GUNBOAT", "CRUISER", "TRANSPORT", "CAPITAL", "MINING" };
        int currentStateGraphIndex = Array.FindIndex(stateGraphs, x => selectedArch.StateGraph?.Equals(x, StringComparison.InvariantCultureIgnoreCase) ?? false);
        if (currentStateGraphIndex == -1)
        {
            currentStateGraphIndex = 0;
        }

        ImGui.Combo("State Graph", ref currentStateGraphIndex, stateGraphs, stateGraphs.Length);
        selectedArch.StateGraph = stateGraphs[currentStateGraphIndex];

        ImGui.NewLine();
        ImGui.Text("NPC Classes##ID");

        if (selectedArch.NpcClass.Count is not 0)
        {
            for (var i = 0; i < selectedArch.NpcClass.Count; i++)
            {
                var npcClass = selectedArch.NpcClass[i];
                ImGui.PushID(npcClass);
                Controls.InputTextId("##ID", ref npcClass);
                ImGui.PopID();
                selectedArch.NpcClass[i] = npcClass;
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedArch.NpcClass);

        ImGui.PopID();
    }

    private int selectedArchIndex = -1;
    private void RenderMissionInformation()
    {
        var ini = missionScript.Ini;
        var info = ini.Info;
        Controls.IdsInputString("Title IDS", gameData, popup, ref info.MissionTitle, x => info.MissionTitle = x);
        Controls.IdsInputString("Offer IDS", gameData, popup, ref info.MissionOffer, x => info.MissionOffer = x);
        ImGui.InputInt("Reward", ref info.Reward);
        ImGui.InputText("NPC Ship File", ref info.NpcShipFile, 255, ImGuiInputTextFlags.ReadOnly);
        if (ImGui.Button("Change Ship File"))
        {
            FileDialog.Open(x =>
            {
                var file = gameData.GameData.TryResolveData(x);
                if (file is null)
                {
                    win.ErrorDialog("The provided file was invalid.");
                    return;
                }

                info.NpcShipFile = file;
                ini.ShipIni = new NPCShipIni(info.NpcShipFile, null);
            }, AppFilters.IniFilters);
        }
    }
}
