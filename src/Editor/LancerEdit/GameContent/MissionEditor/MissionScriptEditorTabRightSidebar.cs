using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.ImUI;
using LibreLancer.Missions;
using LibreLancer.Missions.Directives;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private void RenderRightSidebar()
    {
        var padding = ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().FrameBorderSize;
        ImGui.BeginChild("NavbarRight",
            new Vector2(300f * ImGuiHelper.Scale, ImGui.GetContentRegionAvail().Y - padding), ImGuiChildFlags.None,
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse);

        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));

        if (ImGui.CollapsingHeader("Ship Manager"))
        {
            ImGui.PushID(1);
            RenderMissionShipManager();
            ImGui.PopID();
            ImGui.NewLine();
        }

        if (ImGui.CollapsingHeader("Solar Manager"))
        {
            ImGui.PushID(2);
            RenderMissionSolarManager();
            ImGui.PopID();
            ImGui.NewLine();
        }


        if (ImGui.CollapsingHeader("Loot Manager"))
        {
            ImGui.PushID(3);
            RenderLootManager();
            ImGui.PopID();
            ImGui.NewLine();
        }


        if (ImGui.CollapsingHeader("Dialog Manager"))
        {
            ImGui.PushID(4);
            RenderDialogManager();
            ImGui.PopID();
            ImGui.NewLine();
        }


        if (ImGui.CollapsingHeader("Objective List"))
        {
            ImGui.PushID(5);
            RenderObjectiveListManager();
            ImGui.PopID();
        }

        ImGui.PopStyleColor();

        ImGui.EndChild();
    }

    private int selectedDialogIndex = -1;

    private void RenderDialogManager()
    {
        if (selectedDialogIndex >= missionIni.Dialogs.Count)
            selectedDialogIndex = -1;

        var selectedDialog = selectedDialogIndex != -1 ? missionIni.Dialogs[selectedDialogIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##Dialogs", selectedDialog is not null ? selectedDialog.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.Dialogs.Count; index++)
            {
                var arch = missionIni.Dialogs[index];
                var selected = arch == selectedDialog;

                var id = String.IsNullOrWhiteSpace(selectedDialog?.Nickname) ? $"Untitled_{index.ToString()}" : selectedDialog?.Nickname;

                if (!ImGui.Selectable(id, selected))
                    continue;

                selectedDialogIndex = index;
                selectedDialog = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedDialogIndex = missionIni.Dialogs.Count;
            undoBuffer.Commit(new ListAdd<MissionDialog>("Dialog", missionIni.Dialogs, new()));
        }
        ImGui.SetItemTooltip("Create New Dialog");
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedDialogIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this dialog?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<MissionDialog>("Dialog", missionIni.Dialogs,
                        selectedDialogIndex,
                        missionIni.Dialogs[selectedDialogIndex]));
                    selectedDialogIndex--;
                });
        }
        ImGui.SetItemTooltip("Delete Dialog");
        ImGui.EndDisabled();

        if (selectedDialog is null)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushID(selectedDialogIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedDialog.Nickname, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => !String.IsNullOrWhiteSpace(selectedDialog.Nickname), "Nickname cannot be empty");

        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedDialog.System, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedDialog.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname == selectedDialog.System));
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        for (var index = 0; index < selectedDialog.Lines.Count; index++)
        {
            var line = selectedDialog.Lines[index];
            ImGui.PushID(line.GetHashCode());
            Controls.InputTextIdUndo("Source", undoBuffer, () => ref line.Source, labelWidth: 100f, width: -1f);
            Controls.InputTextIdUndo("Target", undoBuffer, () => ref line.Target, labelWidth: 100f, width: -1f);
            Controls.InputTextIdUndo("Line", undoBuffer, () => ref line.Line, labelWidth: 100f, width: 160f);
            ImGui.SameLine();

            if (ImGui.Button(Icons.Play))
            {
                var src = missionIni.Ships.FirstOrDefault(x =>
                    x.Nickname.Equals(line.Source, StringComparison.OrdinalIgnoreCase));

                if (src is not null)
                {
                    var npc = missionIni.NPCs.FirstOrDefault(x =>
                        x.Nickname.Equals(src.NPC, StringComparison.OrdinalIgnoreCase));

                    if (npc is not null)
                    {
                        gameData.Sounds.PlayVoiceLine(npc.Voice, FLHash.CreateID(line.Line));
                    }
                }
                else
                {
                    var source2 = missionIni.Solars.FirstOrDefault(x =>
                        x.Nickname.Equals(line.Source, StringComparison.OrdinalIgnoreCase));

                    if (source2 != null)
                    {
                        gameData.Sounds.PlayVoiceLine(source2.Voice, FLHash.CreateID(line.Line));
                    }
                }
            }

            ImGui.PopID();
            ImGui.Spacing();

            ImGui.Separator();
            ImGui.Spacing();

        }

        MissionEditorHelpers.AddRemoveListButtons(selectedDialog.Lines, undoBuffer);

        ImGui.PopID();
    }

    private int selectedLootIndex = -1;

    private void RenderLootManager()
    {
        ImGui.Spacing();
        if (selectedLootIndex >= missionIni.Loots.Count)
            selectedLootIndex = -1;

        var selectedLoot = selectedLootIndex != -1 ? missionIni.Loots[selectedLootIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##Loots", selectedLoot is not null ? selectedLoot.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.Loots.Count; index++)
            {
                var arch = missionIni.Loots[index];
                var selected = arch == selectedLoot;

                var id = String.IsNullOrWhiteSpace(arch?.Nickname) ? $"Untitled_{index.ToString()}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                    continue;

                selectedLootIndex = index;
                selectedLoot = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedLootIndex = missionIni.Loots.Count;
            undoBuffer.Commit(new ListAdd<MissionLoot>("Loot", missionIni.Loots, new()));
        }
        ImGui.SetItemTooltip("Create New Loot");
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedLootIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this loot?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<MissionLoot>("Loot", missionIni.Loots,
                        selectedLootIndex,
                        missionIni.Loots[selectedLootIndex]));
                    selectedLootIndex--;
                });
        }
        ImGui.SetItemTooltip("Delete Loot");
        ImGui.EndDisabled();

        if (selectedLoot is null)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushID(selectedLootIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedLoot.Nickname, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => !String.IsNullOrWhiteSpace(selectedLoot.Nickname), "Nickname cannot be empty");
        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedLoot.Archetype, 165f, 100f);
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedLoot.StringId,
            false, 75f, 100f, -1f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGuiExt.CenterText("Transform Settings");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.BeginDisabled(!string.IsNullOrEmpty(selectedLoot.RelPosObj) && selectedLoot.RelPosOffset != Vector3.Zero);
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedLoot.Position, labelWidth: 100f);
        ImGui.EndDisabled();
        ImGui.Spacing();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Relative Position");
        ImGui.Spacing();

        ImGui.BeginDisabled(selectedLoot.Position != Vector3.Zero);
        Controls.InputTextIdUndo("Object", undoBuffer, () => ref selectedLoot.RelPosObj, 0f, 100f);
        Controls.InputFloat3Undo("Offset", undoBuffer, () => ref selectedLoot.RelPosOffset, labelWidth: 100f);
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGuiExt.CenterText("Loot Settings");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Controls.InputIntUndo("Equip Amount", undoBuffer, () => ref selectedLoot.EquipAmount, labelWidth: 100f);

        Controls.SliderFloatUndo("Health", undoBuffer, () => ref selectedLoot.Health, 0f, 1f, labelWidth: 100f, inputWidth:-1f);

        Controls.CheckboxUndo("Can Jettison", undoBuffer, () => ref selectedLoot.CanJettison);

        ImGui.PopID();
    }

    private int selectedSolarIndex = -1;

    private void RenderMissionSolarManager()
    {
        ImGui.Spacing();
        if (selectedSolarIndex >= missionIni.Solars.Count)
            selectedSolarIndex = -1;
        var selectedSolar = selectedSolarIndex != -1 ? missionIni.Solars[selectedSolarIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##Solars", selectedSolar is not null ? selectedSolar.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.Solars.Count; index++)
            {
                var arch = missionIni.Solars[index];
                var selected = arch == selectedSolar;

                var id = String.IsNullOrWhiteSpace(arch?.Nickname) ? $"Untitled_{index.ToString()}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                    continue;

                selectedSolarIndex = index;
                selectedSolar = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();

        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedSolarIndex = missionIni.Solars.Count;
            undoBuffer.Commit(new ListAdd<MissionSolar>("Solar", missionIni.Solars, new()));
        }
        ImGui.SetItemTooltip("Create New Solar");
        ImGui.SameLine();

        ImGui.BeginDisabled(selectedSolarIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this solar?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<MissionSolar>("Solar", missionIni.Solars,
                        selectedSolarIndex,
                        missionIni.Solars[selectedSolarIndex]));
                    selectedSolarIndex--;
                });
        }
        ImGui.SetItemTooltip("Delete Solar");
        ImGui.EndDisabled();

        if (selectedSolar is null)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushID(selectedSolarIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedSolar.Nickname, 0f, 100f);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedSolar.System, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.System,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Faction", undoBuffer, () => ref selectedSolar.Faction, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Faction.Length is 0 ||
                                                     gameData.GameData.Items.Factions.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Faction,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedSolar.Archetype, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Archetype.Length is 0 ||
                                                     gameData.GameData.Items.Archetypes.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Archetype,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Base", undoBuffer, () => ref selectedSolar.Base, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Base.Length is 0 ||
                                                     gameData.GameData.Items.Bases.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Base,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Loadout", undoBuffer, () => ref selectedSolar.Loadout, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Loadout.Length is 0 ||
                                                     gameData.GameData.Items.Loadouts.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Loadout,
                                                             StringComparison.InvariantCultureIgnoreCase)));
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        Controls.InputTextIdUndo("Voice", undoBuffer, () => ref selectedSolar.Voice, 0f, 100f);
        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedSolar.Pilot, 0f, 100f);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGuiExt.CenterText("Consume Settings");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        Controls.InputTextIdUndo("Head", undoBuffer, () => ref selectedSolar.Costume[0], 0f, 100f);
        Controls.InputTextIdUndo("Body", undoBuffer, () => ref selectedSolar.Costume[1], 0f, 100f);
        Controls.InputTextIdUndo("Accessory", undoBuffer, () => ref selectedSolar.Costume[2], 0f, 100f);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        Controls.InputTextIdUndo("Visit", undoBuffer, () => ref selectedSolar.Visit, 0f, 100f);

        Controls.IdsInputStringUndo("String ID", gameData, popup, undoBuffer, () => ref selectedSolar.StringId, false, 0f, 100f);

        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedSolar.Radius, inputWidth: 0f, labelWidth: 100f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Controls.InputStringList("Labels", undoBuffer, selectedSolar.Labels, labelWidth: 100f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedSolar.Position, inputWidth: 0f, labelWidth: 100f);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedSolar.Orientation, inputWidth: 0f, labelWidth: 100f);

        ImGui.PopID();
    }

    private int selectedShipIndex = -1;

    private void RenderMissionShipManager()
    {
        ImGui.Spacing();
        if (selectedShipIndex >= missionIni.Ships.Count)
            selectedShipIndex = -1;

        var selectedShip = selectedShipIndex != -1 ? missionIni.Ships[selectedShipIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##Ships", selectedShip is not null ? selectedShip.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.Ships.Count; index++)
            {
                var arch = missionIni.Ships[index];
                var selected = arch == selectedShip;
                var id = String.IsNullOrWhiteSpace(arch?.Nickname) ? $"Untitled_{index.ToString()}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                    continue;

                selectedShipIndex = index;
                selectedShip = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedShipIndex = missionIni.Ships.Count;
            undoBuffer.Commit(new ListAdd<MissionShip>("Ship", missionIni.Ships, new()));
        }
        ImGui.SetItemTooltip("Create new Ship");
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedShipIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this ship?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<MissionShip>("Ship", missionIni.Ships,
                        selectedShipIndex,
                        missionIni.Ships[selectedShipIndex]));
                    selectedShipIndex--;
                });
        }
        ImGui.SetItemTooltip("Delete Ship");
        ImGui.EndDisabled();

        if (selectedShip is null)
        {
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.Spacing();


        ImGui.PushID(selectedShipIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedShip.Nickname, 165, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => !String.IsNullOrWhiteSpace(selectedShip.Nickname), "Nickname cannot be empty");

        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedShip.System, 165f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname == selectedShip.System));

        ImGui.AlignTextToFramePadding();
        ImGui.Text("NPC"); ImGui.SameLine(100f);
        ImGui.SetNextItemWidth(165f);
        if (ImGui.BeginCombo("##NPC", selectedShip.NPC ?? ""))
        {
            foreach (var npc in missionIni.NPCs
                         .Select(x => x.Nickname)
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.NPC == x)))
            {
                undoBuffer.Set("NPC", () => ref selectedShip.NPC, npc);
            }

            ImGui.EndCombo();
        }

        MissionEditorHelpers.AlertIfInvalidRef(() => missionIni.NPCs.Any(x => x.Nickname == selectedShip.NPC));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Controls.InputStringList("Labels", undoBuffer, selectedShip.Labels, true, 100f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedShip.Position);

        ImGui.Spacing();
        var pos = selectedShip.Position.Length();
        ImGui.Text("Relative Positioning:");

        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedShip.Position.Length() is 0f);

        Controls.InputTextIdUndo("Obj", undoBuffer, () => ref selectedShip.RelativePosition.ObjectName, 0f, 100f);
        // Don't think it's possible to validate this one, as it could refer to any solar object in any system

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Min Range", undoBuffer, () => ref selectedShip.RelativePosition.MinRange);

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Max Range", undoBuffer, () => ref selectedShip.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedShip.Orientation);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Random Name"); ImGui.SameLine(100f);
        Controls.CheckboxUndo("##RandomName", undoBuffer, () => ref selectedShip.RandomName);

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Jumper"); ImGui.SameLine(100f);
        Controls.CheckboxUndo("##Jumper", undoBuffer, () => ref selectedShip.Jumper);

        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedShip.Radius);
        Controls.InputTextIdUndo("Arrival Object", undoBuffer, () => ref selectedShip.ArrivalObj.Object, 0, 100f);

        ImGui.BeginDisabled(string.IsNullOrEmpty(selectedShip.ArrivalObj.Object));
        Controls.InputIntUndo("Undock Index", undoBuffer, () => ref selectedShip.ArrivalObj.Index, labelWidth: 100f);
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();

        ImGui.Text("Initial Objectives"); ImGui.SameLine(100f);
        ImGui.SetNextItemWidth(165f);
        if (ImGui.BeginCombo("##InitialObjectives", selectedShip.InitObjectives ?? ""))
        {
            if (ImGui.Selectable("no_op", selectedShip.InitObjectives == "no_op"))
            {
                undoBuffer.Set("InitObjectives", () => ref selectedShip.InitObjectives, "no_op");
            }

            foreach (var npc in missionIni.ObjLists
                         .Select(x => x.Nickname)
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.InitObjectives == x)))
            {
                undoBuffer.Set("InitObjectives", () => ref selectedShip.InitObjectives, npc);
            }

            ImGui.EndCombo();
        }

        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.InitObjectives is null ||
                                                     selectedShip.InitObjectives.Length is 0 ||
                                                     selectedShip.InitObjectives == "no_op" ||
                                                     missionIni.ObjLists.Any(x =>
                                                         x.Nickname == selectedShip.InitObjectives));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Cargo"); ImGui.SameLine(100f);
        MissionEditorHelpers.AddRemoveListButtons(selectedShip.Cargo, undoBuffer);

        if (selectedShip.Cargo.Count is not 0)
        {
            for (var i = 0; i < selectedShip.Cargo.Count; i++)
            {
                ImGui.Separator();
                var cargo = selectedShip.Cargo[i];
                ImGui.PushID(i);

                Controls.InputTextIdUndo($"    Cargo##{i}", undoBuffer, () => ref cargo.Cargo, 165f, 100f);

                MissionEditorHelpers.AlertIfInvalidRef(() =>
                    gameData.GameData.Items.Equipment.Any(x =>
                        x.Nickname.Equals(cargo.Cargo, StringComparison.InvariantCultureIgnoreCase)));


                Controls.InputIntUndo($"    Count##{i}", undoBuffer, () => ref cargo.Count, labelWidth: 100f);

                if (cargo.Count < 0)
                {
                    cargo.Count = 0;
                }

                ImGui.PopID();
                ImGui.Separator();
                selectedShip.Cargo[i] = cargo;
            }
        }
        ImGui.PopID();
    }

    private int objectiveListIndex = -1;

    private void RenderObjectiveListManager()
    {
        ImGui.Spacing();
        if (objectiveListIndex >= objLists.Count)
            objectiveListIndex = -1;

        ImGui.PushID(objectiveListIndex);
        var selectedObjList = objectiveListIndex != -1 ? objLists[objectiveListIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##ObjectiveLists", selectedObjList is not null ? selectedObjList.Nickname : "(none)"))
        {
            for (var index = 0; index < objLists.Count; index++)
            {
                var arch = objLists[index];
                var selected = arch == selectedObjList;

                var id = String.IsNullOrWhiteSpace(arch?.Nickname) ? $"Untitled_{index.ToString()}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                    continue;

                objectiveListIndex = index;
                selectedObjList = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            popup.OpenPopup(new NameInputPopup(
                NameInputConfig.Nickname("New Objective List",
                    x => objLists.Any(y => y.Nickname.Equals(x, StringComparison.OrdinalIgnoreCase))
                ), "", newName =>
                {
                    undoBuffer.Commit(new ListAdd<ScriptAiCommands>("Objective List", objLists, new(newName)));
                    objectiveListIndex = objLists.Count;
                }));
        }
        ImGui.SetItemTooltip("New Objective List");
        ImGui.SameLine();
        ImGui.BeginDisabled(objectiveListIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this ObjList?",
                () =>
                {
                    undoBuffer.Commit(new ListRemove<ScriptAiCommands>(
                        "Objective List",
                        objLists,
                        objectiveListIndex,
                        objLists[objectiveListIndex]));
                    objectiveListIndex--;
                });
        }
        ImGui.SetItemTooltip("Delete Objective List");
        ImGui.EndDisabled();

        if (selectedObjList is null)
        {
            ImGui.PopID();
            return;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var objListTypes = Enum.GetNames<ObjListCommands>();

        for (var index = 0; index < selectedObjList.Directives.Count; index++)
        {
            ImGui.PushID(index);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var obj = selectedObjList.Directives[index];

            var typeIndex = (int)obj.Command;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 1.5f);
            ImGui.Combo("##CommandType", ref typeIndex, objListTypes, objListTypes.Length);

            if ((int)obj.Command != typeIndex)
            {
                undoBuffer.Commit(new ListSet<MissionDirective>("Command", selectedObjList.Directives,
                    index, obj, MissionDirective.New((ObjListCommands)typeIndex)));
            }
            if (DrawDirective(index, obj))
            {
                selectedObjList.Directives.RemoveAt(index--);
            }

            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Add Command"))
        {
            undoBuffer.Commit(new ListAdd<MissionDirective>("Command", selectedObjList.Directives, new BreakFormationDirective()));
        }

        ImGui.PopID();
        return;

        bool DrawDirective(int id, MissionDirective cmd)
        {
            // begin border/frame/whatever
            ImGui.PushID($"obj-list-{id}-{cmd.GetType()}");

            ImGui.SameLine();

            if (ImGui.Button($"{Icons.TrashAlt}"))
            {
                ImGui.PopID();
                return true;
            }
            DirectiveEditor.EditDirective(cmd, undoBuffer);
            // end border/frame/whatever
            ImGui.PopID();
            return false;
        }
    }
}
