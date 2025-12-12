using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private SearchDropdown<NodeMissionTrigger> jumpLookup;
    void SetupJumpList()
    {
        jumpLookup = new(
            "JumpTo",
            t => t?.InternalId ?? "",
            x =>
            {
                jumpToNode = x;
            }, null,
            nodes.OfType<NodeMissionTrigger>().OrderBy(x => x.InternalId).ToArray());
    }

    private void RenderLeftSidebar()
    {
        var padding = ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().FrameBorderSize;
        ImGui.BeginChild("NavbarLeft", new Vector2(300f * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().Y - padding * 2), ImGuiChildFlags.None,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse);

        if (ImGuiExt.ToggleButton("Undo History", renderHistory))
        {
            renderHistory = !renderHistory;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Navigate To: ");
        ImGui.SameLine();
        jumpLookup?.Draw();
        Controls.InputTextIdUndo("Node Filter", undoBuffer, () => ref NodeFilter);

        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        if (ImGui.CollapsingHeader("Mission Information", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(1);
            RenderMissionInformation();
            ImGui.PopID();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("NPC Arch Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(2);
            RenderNpcArchManager();
            ImGui.PopID();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("NPC Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(3);
            RenderNpcManagement();
            ImGui.PopID();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("Formation Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(4);
            RenderFormationManagement();
            ImGui.PopID();
        }

        ImGui.PopStyleColor();
        ImGui.EndChild();
    }

    private int selectedFormationIndex = -1;

    private void RenderFormationManagement()
    {
        if (ImGui.Button("Create New Formation"))
        {
            selectedFormationIndex = missionIni.Formations.Count;
            undoBuffer.Commit(new ListAdd<MissionFormation>("Formation", missionIni.Formations, new()));
        }

        ImGui.BeginDisabled(selectedFormationIndex == -1);
        if (ImGui.Button("Delete Formation"))
        {
            win.Confirm("Are you sure you want to delete this formation?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<MissionFormation>("Formation", missionIni.Formations,
                        selectedFormationIndex,
                        missionIni.Formations[selectedFormationIndex]));
                    selectedFormationIndex--;
                });
        }

        ImGui.EndDisabled();
        if (selectedFormationIndex >= missionIni.Formations.Count)
            selectedFormationIndex = -1;

        var selectedFormation = selectedFormationIndex != -1 ? missionIni.Formations[selectedFormationIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("Formations", selectedFormation is not null ? selectedFormation.Nickname : ""))
        {
            for (var index = 0; index < missionIni.Formations.Count; index++)
            {
                var arch = missionIni.Formations[index];
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

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedFormation.Nickname, 150f);

        ImGui.SetNextItemWidth(200f);
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedFormation.Position);

        ImGui.Text("Relative Position:");
        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedFormation.Position.Length() is not 0f);

        Controls.InputTextIdUndo("Obj", undoBuffer, () => ref selectedFormation.RelativePosition.ObjectName, 150f);

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Min Range", undoBuffer, () => ref selectedFormation.RelativePosition.MinRange);

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Max Range", undoBuffer, () => ref selectedFormation.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        ImGui.SetNextItemWidth(200f);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedFormation.Orientation);

        ImGui.Text("Ships");
        for (var index = 0; index < selectedFormation.Ships.Count; index++)
        {
            var str = selectedFormation.Ships[index];
            ImGui.PushID(str);

            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("###", ref str, 32, ImGuiInputTextFlags.ReadOnly);
            MissionEditorHelpers.AlertIfInvalidRef(() =>
                missionIni.Ships.Any(x => x.Nickname.Equals(str, StringComparison.InvariantCultureIgnoreCase)));
            selectedFormation.Ships[index] = str;

            ImGui.SameLine();
            if (ImGui.Button(Icons.X + "##"))
            {
                undoBuffer.Commit(new ListRemove<string>("Ships", selectedFormation.Ships,
                    index, selectedFormation.Ships[index]));
            }

            ImGui.PopID();
        }

        if (missionIni.Ships.Count > 0)
        {
            if (selectedShipIndex >= missionIni.Ships.Count || selectedShipIndex is -1)
            {
                selectedShipIndex = missionIni.Ships.Count - 1;
            }

            ImGui.Combo("Add New Ship", ref selectedShipIndex, missionIni.Ships.Select(x => x.Nickname).ToArray(),
                missionIni.Ships.Count);
            string shipNickname = missionIni.Ships[selectedShipIndex].Nickname;

            ImGui.BeginDisabled(selectedFormation.Ships.Contains(shipNickname));
            if (ImGui.Button($"Add Ship {Icons.PlusCircle}"))
            {
                undoBuffer.Commit(new ListAdd<string>("Ships", selectedFormation.Ships, shipNickname));
            }

            ImGui.EndDisabled();
        }
        else
        {
            ImGui.Text("Cannot add a ship. No ships are setup.  " + Icons.Warning);
        }

        ImGui.PopID();
    }

    private int selectedNpcIndex = -1;

    private void RenderNpcManagement()
    {
        if (ImGui.Button("Create New NPC"))
        {
            selectedNpcIndex = missionIni.NPCs.Count;
            undoBuffer.Commit(new ListAdd<MissionNPC>("NPC", missionIni.NPCs, new()));
        }

        ImGui.BeginDisabled(selectedNpcIndex == -1);
        if (ImGui.Button("Delete NPC"))
        {
            win.Confirm("Are you sure you want to delete this NPC?", () =>
            {
                undoBuffer.Commit(new ListRemove<MissionNPC>("NPC", missionIni.NPCs,
                    selectedNpcIndex,
                    missionIni.NPCs[selectedNpcIndex]));
                selectedNpcIndex--;
            });
        }

        ImGui.EndDisabled();

        if (selectedNpcIndex >= missionIni.NPCs.Count)
            selectedNpcIndex = -1;

        var selectedNpc = selectedNpcIndex != -1 ? missionIni.NPCs[selectedNpcIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("NPCs", selectedNpc is not null ? selectedNpc.Nickname : ""))
        {
            for (var index = 0; index < missionIni.NPCs.Count; index++)
            {
                var arch = missionIni.NPCs[index];
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

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedNpc.Nickname, 150f);
        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedNpc.NpcShipArch, 150f);
        if (missionIni.ShipIni != null)
        {
            MissionEditorHelpers.AlertIfInvalidRef(() =>
                missionIni.ShipIni.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch)
                || gameData.GameData.Ini.NPCShips.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch));
        }
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedNpc.IndividualName,
            inputWidth: 150f);
        Controls.InputTextIdUndo("Affiliation", undoBuffer, () => ref selectedNpc.Affiliation, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Factions.Any(x => x.Nickname == selectedNpc.Affiliation));

        Controls.InputTextIdUndo("Costume Head", undoBuffer, () => ref selectedNpc.SpaceCostume[0], 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[0]) is not null);
        Controls.InputTextIdUndo("Costume Body", undoBuffer, () => ref selectedNpc.SpaceCostume[1], 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[1]) is not null);
        Controls.InputTextIdUndo("Costume Accessory", undoBuffer, () => ref selectedNpc.SpaceCostume[2], 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Ini.Bodyparts.FindAccessory(selectedNpc.SpaceCostume[2]) is not null);

        ImGui.PopID();
    }

    private void RenderNpcArchManager()
    {
        if (missionIni.ShipIni == null)
        {
            ImGui.BulletText("NPCShipIni not defined");
            return;
        }
        if (ImGui.Button("Create New Ship Arch"))
        {
            selectedArchIndex = missionIni.ShipIni.ShipArches.Count;
            undoBuffer.Commit(new ListAdd<NPCShipArch>("Ship Arch", missionIni.ShipIni.ShipArches, new()));
        }

        ImGui.BeginDisabled(selectedArchIndex == -1);
        if (ImGui.Button("Delete Ship Arch"))
        {
            win.Confirm("Are you sure you want to delete this ship arch?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<NPCShipArch>("Ship Arch",
                        missionIni.ShipIni.ShipArches, selectedArchIndex,
                        missionIni.ShipIni.ShipArches[selectedArchIndex]));
                    selectedArchIndex--;
                });
        }
        ImGui.EndDisabled();

        if (selectedArchIndex >= missionIni.ShipIni.ShipArches.Count)
            selectedArchIndex = -1;

        var selectedArch = selectedArchIndex != -1 ? missionIni.ShipIni.ShipArches[selectedArchIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("Ship Archs", selectedArch is not null ? selectedArch.Nickname : ""))
        {
            for (var index = 0; index < missionIni.ShipIni.ShipArches.Count; index++)
            {
                var arch = missionIni.ShipIni.ShipArches[index];
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

        Controls.InputTextIdUndo("Arch Nickname", undoBuffer, () => ref selectedArch.Nickname, 150f);
        Controls.InputTextIdUndo("Loadout", undoBuffer, () => ref selectedArch.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Loadouts.Any(x => x.Nickname == selectedArch.Loadout));

        ImGui.SetNextItemWidth(100f);
        Controls.InputIntUndo("Level", undoBuffer, () => ref selectedArch.Level, 1, 10);
        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedArch.Pilot, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.GetPilot(selectedArch.Pilot) is not null);

        string[] stateGraphs =
            { "NOTHING", "FIGHTER", "FREIGHTER", "GUNBOAT", "CRUISER", "TRANSPORT", "CAPITAL", "MINING" };
        int currentStateGraphIndex = Array.FindIndex(stateGraphs,
            x => selectedArch.StateGraph?.Equals(x, StringComparison.InvariantCultureIgnoreCase) ?? false);
        if (currentStateGraphIndex == -1)
        {
            currentStateGraphIndex = 0;
        }

        ImGui.SetNextItemWidth(150f);
        ImGui.Combo("State Graph", ref currentStateGraphIndex, stateGraphs, stateGraphs.Length);
        if (selectedArch.StateGraph != stateGraphs[currentStateGraphIndex])
        {
            undoBuffer.Set("State Graph", () => ref selectedArch.StateGraph, stateGraphs[currentStateGraphIndex]);
        }
        ImGui.NewLine();
        ImGui.Text("NPC Classes");

        if (selectedArch.NpcClass.Count is not 0)
        {
            for (var i = 0; i < selectedArch.NpcClass.Count; i++)
            {
                var npcClass = selectedArch.NpcClass[i];
                int idx = i;
                ImGui.PushID(idx);
                ImGuiExt.InputTextLogged("##npc-class", ref npcClass,
                    250, (old, upd) => new ListSet<string>(
                        "Npc Class", selectedArch.NpcClass, idx, old, upd), true);
                ImGui.PopID();
                selectedArch.NpcClass[i] = npcClass;
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedArch.NpcClass, undoBuffer);

        ImGui.PopID();
    }

    private int selectedArchIndex = -1;

    private void RenderMissionInformation()
    {
        var info = missionIni.Info;
        Controls.IdsInputStringUndo("Title IDS", gameData, popup, undoBuffer, () => ref info.MissionTitle);
        Controls.IdsInputStringUndo("Offer IDS", gameData, popup, undoBuffer, () => ref info.MissionOffer);

        ImGui.PushItemWidth(150f);

        Controls.InputIntUndo("Reward", undoBuffer, () => ref info.Reward);
        ImGui.InputText("NPC Ship File", ref info.NpcShipFile, 255, ImGuiInputTextFlags.ReadOnly);

        ImGui.PopItemWidth();

        if (ImGui.Button("Change Ship File"))
        {
            popup.OpenPopup(new VfsFileSelector("Change Ship File",
                gameData.GameData.VFS,
                gameData.GameData.Ini.Freelancer.DataPath, x =>
                {
                    undoBuffer.Commit(new SetNpcShipFileAction(
                        missionIni.Info.NpcShipFile,
                        gameData.GameData.Ini.Freelancer.DataPath + x,
                        this));
                }, VfsFileSelector.MakeFilter(".ini")));
        }
    }
}
