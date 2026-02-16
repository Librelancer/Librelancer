using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private NodeMissionTrigger[] jumpOptions;
    private Dictionary<string, NodeMissionTrigger> triggersByName;
    void SetupLookups()
    {
        jumpOptions = nodes.OfType<NodeMissionTrigger>().OrderBy(x => x.InternalId).ToArray();
        triggersByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (var n in jumpOptions)
            triggersByName[n.InternalId] = n;
    }

    public NodeMissionTrigger GetTrigger(string name)
    {
        triggersByName.TryGetValue(name, out var ret);
        return ret;
    }

    private void RenderLeftSidebar()
    {
        var padding = ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().FrameBorderSize;
        ImGui.BeginChild("NavbarLeft",
            new Vector2(300f * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().Y - padding * 2), ImGuiChildFlags.None,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse);

        if (ImGuiExt.ToggleButton("Undo History", renderHistory))
        {
            renderHistory = !renderHistory;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Navigate To: ");
        ImGui.SameLine();
        SearchDropdown<NodeMissionTrigger>.Draw("JumpTo",
            ref jumpToNode, jumpOptions, t => t?.InternalId ?? "");
        Controls.InputTextIdUndo("Node Filter", undoBuffer, () => ref NodeFilter);

        if (SidebarHeader("Mission Information"))
        {
            ImGui.PushID(1);
            RenderMissionInformation();
            ImGui.PopID();
        }
        ImGui.NewLine();

        if (SidebarHeader("NPC Arch Management"))
        {
            ImGui.PushID(2);
            RenderNpcArchManager();
            ImGui.PopID();
        }
        ImGui.NewLine();

        if (SidebarHeader("NPC Management"))
        {
            ImGui.PushID(3);
            RenderNpcManagement();
            ImGui.PopID();
        }
        ImGui.NewLine();

        if (SidebarHeader("Formation Management"))
        {
            ImGui.PushID(4);
            RenderFormationManagement();
            ImGui.PopID();
        }
        ImGui.NewLine();

        if (SidebarHeader("Objective Management"))
        {
            ImGui.PushID(5);
            RenderObjectiveManagement();
            ImGui.PopID();
        }

        ImGui.EndChild();
    }

    private ScriptFormation selectedFormation;

    private void RenderFormationManagement()
    {
        DictionaryRemove<ScriptFormation> Delete()
        {
            return new(
                "Formation",
                missionIni.Formations, selectedFormation,
                () => ref selectedFormation);
        }
        ItemList("Formation", missionIni.Formations, () => ref selectedFormation, Delete);

        if (selectedFormation is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("formation"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Formations, selectedFormation);

        Controls.TableSeparatorText("Absolute Position");
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedFormation.Position);

        Controls.TableSeparatorText("Relative Position");
        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedFormation.Position.Length() is not 0f);

        Controls.InputTextIdUndo("Obj", undoBuffer, () => ref selectedFormation.RelativePosition.ObjectName);
        Controls.InputFloatUndo("Min Range", undoBuffer, () => ref selectedFormation.RelativePosition.MinRange);
        Controls.InputFloatUndo("Max Range", undoBuffer, () => ref selectedFormation.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        Controls.TableSeparator();
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedFormation.Orientation);
        Controls.TableSeparator();

        Controls.EditControlSetup("Ships", 0);
        for (var index = 0; index < selectedFormation.Ships.Count; index++)
        {
            var str = selectedFormation.Ships[index];
            ImGui.PushID(index);

            ImGui.SetNextItemWidth(150f);
            ImGui.InputText("###", ref str.Nickname, 32, ImGuiInputTextFlags.ReadOnly);
            selectedFormation.Ships[index] = str;

            ImGui.SameLine();
            if (ImGui.Button(Icons.X + "##"))
            {
                undoBuffer.Commit(new ListRemove<ScriptShip>("Ships", selectedFormation.Ships,
                    index, selectedFormation.Ships[index]));
            }

            ImGui.PopID();
        }


        if (missionIni.Ships.Count > 0)
        {
            if (ImGui.Button("Add New Ship"))
            {
                ImGui.OpenPopup("##NewShipPopup");
            }
            if (ImGui.BeginPopup("##NewShipPopup"))
            {
                foreach (var s in missionIni.Ships)
                {
                    if (ImGui.Selectable(s.Key))
                    {
                        undoBuffer.Commit(new ListAdd<ScriptShip>("Ships", selectedFormation.Ships, s.Value));
                    }
                }
                ImGui.EndPopup();
            }
        }
        else
        {
            ImGuiExt.Button("Add New Ship", false);
            ImGui.SetItemTooltip("Cannot add a ship. No ships are setup.  " + Icons.Warning);
        }
        Controls.EndEditorTable();
    }

    private ScriptNPC selectedNpc;

    private void RenderNpcManagement()
    {
        DictionaryRemove<ScriptNPC> Delete()
        {
            return new(
                "NPC",
                missionIni.Npcs, selectedNpc,
                () => ref selectedNpc);
        }
        ItemList("NPC", missionIni.Npcs, () => ref selectedNpc, Delete);

        if (selectedNpc is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("npc"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Npcs, selectedNpc);
        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedNpc.NpcShipArch);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            missionIni.NpcShips.ContainsKey(selectedNpc.NpcShipArch)
            || gameData.GameData.Items.NpcShips.Contains(selectedNpc.NpcShipArch));
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedNpc.IndividualName,
            inputWidth: 150f);
        gameData.Factions.DrawUndo("Affiliation", undoBuffer, () => ref selectedNpc.Affiliation);
        Controls.TableSeparatorText("Costume");
        gameData.Costumes.Draw("Costume", undoBuffer,
            () => ref selectedNpc.SpaceCostume.Head,
            () => ref selectedNpc.SpaceCostume.Body,
            () => ref selectedNpc.SpaceCostume.Accessory);
        Controls.EndEditorTable();
    }

    private ShipArch selectedArch;

    private void RenderNpcArchManager()
    {
        if (string.IsNullOrWhiteSpace(missionIni.Info.NpcShipFile))
        {
            ImGui.BulletText("NPCShipIni not defined");
            return;
        }

        DictionaryRemove<ShipArch> Delete()
        {
            return new(
                "Ship Arch",
                missionIni.NpcShips, selectedArch,
                () => ref selectedArch);
        }
        ItemList("Npc Ship Arch", missionIni.NpcShips, () => ref selectedArch, Delete);

        if (selectedArch is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("shiparch"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.NpcShips, selectedArch);
        gameData.Ships.DrawUndo("Ship", undoBuffer, () => ref selectedArch.Ship);
        Controls.InputTextIdUndo("Loadout", undoBuffer, () => ref selectedArch.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Loadouts.Any(x => x.Nickname == selectedArch.Loadout));

        ImGui.SetNextItemWidth(100f);
        Controls.InputIntUndo("Level", undoBuffer, () => ref selectedArch.Level, 1, 10);
        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedArch.Pilot, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Items.GetPilot(selectedArch.Pilot) is not null);

        string[] stateGraphs =
            { "NOTHING", "FIGHTER", "FREIGHTER", "GUNBOAT", "CRUISER", "TRANSPORT", "CAPITAL", "MINING" };
        int currentStateGraphIndex = Array.FindIndex(stateGraphs,
            x => selectedArch.StateGraph?.Equals(x, StringComparison.InvariantCultureIgnoreCase) ?? false);
        if (currentStateGraphIndex == -1)
        {
            currentStateGraphIndex = 0;
        }

        Controls.EditControlSetup("State Graph", 0);

        ImGui.Combo("##stategraph", ref currentStateGraphIndex, stateGraphs, stateGraphs.Length);
        if (selectedArch.StateGraph != stateGraphs[currentStateGraphIndex])
        {
            undoBuffer.Set("State Graph", () => ref selectedArch.StateGraph, stateGraphs[currentStateGraphIndex]);
        }

        Controls.EditControlSetup("NPC Classes", 0);

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

        Controls.EndEditorTable();
    }

    private int selectedArchIndex = -1;

    private void RenderMissionInformation()
    {
        var info = missionIni.Info;
        if (!Controls.BeginEditorTable("info"))
            return;
        Controls.IdsInputStringUndo("Title IDS", gameData, popup, undoBuffer, () => ref info.MissionTitle);
        Controls.IdsInputStringUndo("Offer IDS", gameData, popup, undoBuffer, () => ref info.MissionOffer);


        Controls.InputIntUndo("Reward", undoBuffer, () => ref info.Reward);
        Controls.DisabledInputTextId("NPC Ship File", info.NpcShipFile);


        if (ImGuiExt.Button("Set Ship File", string.IsNullOrWhiteSpace(info.NpcShipFile)))
        {
            popup.OpenPopup(new VfsFileSelector("Set Ship File",
                gameData.GameData.VFS,
                gameData.GameData.Items.Ini.Freelancer.DataPath, x =>
                {
                    undoBuffer.Commit(new SetNpcShipFileAction(
                        missionIni.Info.NpcShipFile,
                        gameData.GameData.Items.Ini.Freelancer.DataPath + x,
                        this));
                }, VfsFileSelector.MakeFilter(".ini")));
        }
        Controls.EndEditorTable();
    }

    private DocumentObjective selectedObjective;

    private void RenderObjectiveManagement()
    {
        DictionaryRemove<DocumentObjective> Delete()
        {
            return new(
                "Objective",
                missionIni.Objectives, selectedObjective,
                () => ref selectedObjective);
        }
        ItemList("Objective", missionIni.Objectives, () => ref selectedObjective, Delete);

        if (selectedObjective is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("objective"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Objectives, selectedObjective);
        Controls.EditControlSetup("Type", 0);
        if (ImGui.BeginCombo("##type", selectedObjective.Data.Type.ToString()))
        {
            if (ImGui.Selectable("ids", selectedObjective.Data.Type == NNObjectiveType.ids) &&
                selectedObjective.Data.Type != NNObjectiveType.ids)
            {
                undoBuffer.Set("Type", () => ref selectedObjective.Data.Type, NNObjectiveType.ids);
            }
            if (ImGui.Selectable("navmarker", selectedObjective.Data.Type == NNObjectiveType.navmarker) &&
                selectedObjective.Data.Type != NNObjectiveType.navmarker)
            {
                undoBuffer.Set("Type", () => ref selectedObjective.Data.Type, NNObjectiveType.navmarker);

            }
            if (ImGui.Selectable("rep_inst", selectedObjective.Data.Type == NNObjectiveType.rep_inst) &&
                selectedObjective.Data.Type != NNObjectiveType.rep_inst)
            {
                undoBuffer.Set("Type", () => ref selectedObjective.Data.Type, NNObjectiveType.rep_inst);
            }
            ImGui.EndCombo();
        }
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer, () => ref selectedObjective.Data.NameIds);
        if (selectedObjective.Data.Type != NNObjectiveType.ids)
        {
            Controls.IdsInputStringUndo("Explanation", gameData, popup, undoBuffer, () => ref selectedObjective.Data.ExplanationIds);
            Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedObjective.Data.System);
        }
        if (selectedObjective.Data.Type == NNObjectiveType.navmarker)
        {
            Controls.InputFloat3Undo("Position", undoBuffer,  () => ref selectedObjective.Data.Position);
        }
        if (selectedObjective.Data.Type == NNObjectiveType.rep_inst)
        {
            Controls.InputTextIdUndo("Object", undoBuffer, () => ref selectedObjective.Data.SolarNickname);
        }
        Controls.EndEditorTable();
    }
}
