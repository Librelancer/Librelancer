using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        ImGui.BeginChild("NavbarLeft", new Vector2(300f, ImGui.GetContentRegionMax().Y), ImGuiChildFlags.None,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        if (ImGui.CollapsingHeader("Mission Information", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderMissionInformation();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("NPC Arch Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderNpcArchManager();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("NPC Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderNpcManagement();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("Formation Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderFormationManagement();
        }

        ImGui.PopStyleColor();
        ImGui.EndChild();
    }

    private int selectedFormationIndex = -1;
    private void RenderFormationManagement()
    {
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New Formation"))
        {
            selectedFormationIndex = ini.Formations.Count;
            ini.Formations.Add(new MissionFormation());
        }

        ImGui.BeginDisabled(selectedFormationIndex == -1);
        if (ImGui.Button("Delete Formation"))
        {
            win.Confirm("Are you sure you want to delete this formation?", () =>
            {
                ini.Formations.RemoveAt(selectedFormationIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedFormation = selectedFormationIndex != -1 ? ini.Formations[selectedFormationIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("Formations", selectedFormation is not null ? selectedFormation.Nickname : ""))
        {
            for (var index = 0; index < ini.Formations.Count; index++)
            {
                var arch = ini.Formations[index];
                var selected = arch == selectedFormation;
                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedFormationIndex = index;
                selectedFormation = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedFormation is null)
        {
            return;
        }

        ImGui.PushID(selectedFormationIndex);

        Controls.InputTextId("Nickname##Formation", ref selectedFormation.Nickname, 150f);

        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat3("Position##Formation", ref selectedFormation.Position);

        ImGui.Text("Relative Position:");
        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedFormation.Position.Length() is not 0f);

        Controls.InputTextId("Obj##Ship", ref selectedFormation.RelativePosition.ObjectName, 150f);

        ImGui.SetNextItemWidth(150f);
        ImGui.InputFloat("Min Range##Ship", ref selectedFormation.RelativePosition.MinRange);

        ImGui.SetNextItemWidth(150f);
        ImGui.InputFloat("Max Range##Ship", ref selectedFormation.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        ImGui.SetNextItemWidth(200f);
        Controls.InputFlQuaternion("Oritentation##Formation", ref selectedFormation.Orientation);

        ImGui.Text("Ships");
        for (var index = 0; index < selectedFormation.Ships.Count; index++)
        {
            var str = selectedFormation.Ships[index];
            ImGui.PushID(str);

            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("###", ref str, 32, ImGuiInputTextFlags.ReadOnly);
            MissionEditorHelpers.AlertIfInvalidRef(() => ini.Ships.Any(x => x.Nickname.Equals(str, StringComparison.InvariantCultureIgnoreCase)));
            selectedFormation.Ships[index] = str;

            ImGui.SameLine();
            if (ImGui.Button(Icons.X + "##"))
            {
                selectedFormation.Ships.RemoveAt(index);
            }

            ImGui.PopID();
        }

        if (ini.Ships.Count > 0)
        {
            if (selectedShipIndex >= ini.Ships.Count || selectedShipIndex is -1)
            {
                selectedShipIndex = ini.Ships.Count - 1;
            }

            ImGui.Combo("Add New Ship##Formation", ref selectedShipIndex, ini.Ships.Select(x => x.Nickname).ToArray(), ini.Ships.Count);
            string shipNickname = ini.Ships[selectedShipIndex].Nickname;

            ImGui.BeginDisabled(selectedFormation.Ships.Contains(shipNickname));
            if (ImGui.Button($"Add Ship {Icons.PlusCircle}##FormationShips"))
            {
                selectedFormation.Ships.Add(shipNickname);
            }
            ImGui.EndDisabled();
        }
        else
        {
            ImGui.Text("Cannot add a ship. No ships are setup.  " + Icons.Warning);
        }
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
        ImGui.SetNextItemWidth(150f);
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

        Controls.InputTextId("Nickname##NPC", ref selectedNpc.Nickname, 150f);
        Controls.InputTextId("Archetype##NPC", ref selectedNpc.NpcShipArch, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            ini.ShipIni.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch)
            || gameData.GameData.Ini.NPCShips.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch));

        Controls.IdsInputString("Name##NPC", gameData, popup, ref selectedNpc.IndividualName, x => selectedNpc.IndividualName = x, inputWidth: 150f);
        Controls.InputTextId("Affiliation##NPC", ref selectedNpc.Affiliation, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Factions.Any(x => x.Nickname == selectedNpc.Affiliation));

        Controls.InputTextId("Costume Head##NPC", ref selectedNpc.SpaceCostume[0], 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[0]) is not null);
        Controls.InputTextId("Costume Body##NPC", ref selectedNpc.SpaceCostume[1], 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[1]) is not null);
        Controls.InputTextId("Costume Accessory##NPC", ref selectedNpc.SpaceCostume[2], 150f);
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
        ImGui.SetNextItemWidth(150f);
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

        Controls.InputTextId("Arch Nickname##ID", ref selectedArch.Nickname, 150f);
        Controls.InputTextId("Loadout##ID", ref selectedArch.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Loadouts.Any(x => x.Nickname == selectedArch.Loadout));

        ImGui.SetNextItemWidth(100f);
        ImGui.InputInt("Level##ID", ref selectedArch.Level, 1, 10);
        Controls.InputTextId("Pilot##ID", ref selectedArch.Pilot, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.GetPilot(selectedArch.Pilot) is not null);

        string[] stateGraphs = { "NOTHING", "FIGHTER", "FREIGHTER", "GUNBOAT", "CRUISER", "TRANSPORT", "CAPITAL", "MINING" };
        int currentStateGraphIndex = Array.FindIndex(stateGraphs, x => selectedArch.StateGraph?.Equals(x, StringComparison.InvariantCultureIgnoreCase) ?? false);
        if (currentStateGraphIndex == -1)
        {
            currentStateGraphIndex = 0;
        }

        ImGui.SetNextItemWidth(150f);
        ImGui.Combo("State Graph", ref currentStateGraphIndex, stateGraphs, stateGraphs.Length);
        selectedArch.StateGraph = stateGraphs[currentStateGraphIndex];

        ImGui.NewLine();
        ImGui.Text("NPC Classes");

        if (selectedArch.NpcClass.Count is not 0)
        {
            for (var i = 0; i < selectedArch.NpcClass.Count; i++)
            {
                var npcClass = selectedArch.NpcClass[i];
                ImGui.PushID(npcClass);
                Controls.InputTextId("##ID", ref npcClass, 150f);
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

        ImGui.PushItemWidth(150f);

        ImGui.InputInt("Reward", ref info.Reward);
        ImGui.InputText("NPC Ship File", ref info.NpcShipFile, 255, ImGuiInputTextFlags.ReadOnly);

        ImGui.PopItemWidth();

        popup.OpenPopup(new VfsFileSelector("Change Ship File",
            gameData.GameData.VFS,
            gameData.GameData.Ini.Freelancer.DataPath, x =>
            {
                info.NpcShipFile = x;
                ini.ShipIni = new NPCShipIni(info.NpcShipFile, null);
            }, VfsFileSelector.MakeFilter("ini")));
    }
}
