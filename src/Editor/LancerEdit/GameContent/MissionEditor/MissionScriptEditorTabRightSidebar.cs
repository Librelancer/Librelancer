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

        if (ImGui.CollapsingHeader("Ship Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(1);
            RenderMissionShipManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Solar Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(2);
            RenderMissionSolarManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Loot Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(3);
            RenderLootManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Dialog Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(4);
            RenderDialogManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Objective List", ImGuiTreeNodeFlags.DefaultOpen))
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
        if (ImGui.Button("Create New Dialog"))
        {
            selectedDialogIndex = missionIni.Dialogs.Count;
            undoBuffer.Commit(new ListAdd<MissionDialog>("Dialog", missionIni.Dialogs, new()));
        }

        ImGui.BeginDisabled(selectedDialogIndex == -1);

        if (ImGui.Button("Delete Dialog"))
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

        ImGui.EndDisabled();

        if (selectedDialogIndex >= missionIni.Dialogs.Count)
            selectedDialogIndex = -1;

        var selectedDialog = selectedDialogIndex != -1 ? missionIni.Dialogs[selectedDialogIndex] : null;
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Dialogs", selectedDialog is not null ? selectedDialog.Nickname : ""))
        {
            for (var index = 0; index < missionIni.Dialogs.Count; index++)
            {
                var arch = missionIni.Dialogs[index];
                var selected = arch == selectedDialog;

                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedDialogIndex = index;
                selectedDialog = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedDialog is null)
        {
            return;
        }

        ImGui.PushID(selectedDialogIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedDialog.Nickname, 150f);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedDialog.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedDialog.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname == selectedDialog.System));

        for (var index = 0; index < selectedDialog.Lines.Count; index++)
        {
            var line = selectedDialog.Lines[index];
            ImGui.PushID(line.GetHashCode());
            Controls.InputTextIdUndo("Source", undoBuffer, () => ref line.Source);
            Controls.InputTextIdUndo("Target", undoBuffer, () => ref line.Target);
            Controls.InputTextIdUndo("Line", undoBuffer, () => ref line.Line);
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

            if (index + 1 != selectedDialog.Lines.Count)
            {
                ImGui.NewLine();
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedDialog.Lines, undoBuffer);

        ImGui.PopID();
    }

    private int selectedLootIndex = -1;

    private void RenderLootManager()
    {
        if (ImGui.Button("Create New Loot"))
        {
            selectedLootIndex = missionIni.Loots.Count;
            undoBuffer.Commit(new ListAdd<MissionLoot>("Loot", missionIni.Loots, new()));
        }

        ImGui.BeginDisabled(selectedLootIndex == -1);

        if (ImGui.Button("Delete Loot"))
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

        ImGui.EndDisabled();

        if(selectedLootIndex >= missionIni.Loots.Count)
            selectedLootIndex = -1;

        var selectedLoot = selectedLootIndex != -1 ? missionIni.Loots[selectedLootIndex] : null;
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Loots", selectedLoot is not null ? selectedLoot.Nickname : ""))
        {
            for (var index = 0; index < missionIni.Loots.Count; index++)
            {
                var arch = missionIni.Loots[index];
                var selected = arch == selectedLoot;

                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedLootIndex = index;
                selectedLoot = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedLoot is null)
        {
            return;
        }

        ImGui.PushID(selectedLootIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedLoot.Nickname, 150f);
        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedLoot.Archetype, 150f);
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedLoot.StringId,
            inputWidth: 150f);

        ImGui.BeginDisabled(!string.IsNullOrEmpty(selectedLoot.RelPosObj) && selectedLoot.RelPosOffset != Vector3.Zero);
        ImGui.SetNextItemWidth(200f);
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedLoot.Position);
        ImGui.EndDisabled();

        ImGui.Text("Relative Position");
        ImGui.BeginDisabled(selectedLoot.Position != Vector3.Zero);

        Controls.InputTextIdUndo("Object", undoBuffer, () => ref selectedLoot.RelPosObj, 150f);

        ImGui.SetNextItemWidth(200f);
        Controls.InputFloat3Undo("Offset", undoBuffer, () => ref selectedLoot.RelPosOffset);

        ImGui.EndDisabled();

        ImGui.NewLine();
        ImGui.SetNextItemWidth(200f);
        Controls.InputIntUndo("Equip Amount", undoBuffer, () => ref selectedLoot.EquipAmount);

        ImGui.SetNextItemWidth(200f);
        Controls.SliderFloatUndo("Health", undoBuffer, () => ref selectedLoot.Health, 0f, 1f);

        Controls.CheckboxUndo("Can Jettison", undoBuffer, () => ref selectedLoot.CanJettison);

        ImGui.PopID();
    }

    private int selectedSolarIndex = -1;

    private void RenderMissionSolarManager()
    {
        if (ImGui.Button("Create New Solar"))
        {
            selectedSolarIndex = missionIni.Solars.Count;
            undoBuffer.Commit(new ListAdd<MissionSolar>("Solar", missionIni.Solars, new()));
        }

        ImGui.BeginDisabled(selectedSolarIndex == -1);

        if (ImGui.Button("Delete Solar"))
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

        ImGui.EndDisabled();

        if (selectedSolarIndex >= missionIni.Solars.Count)
            selectedSolarIndex = -1;
        var selectedSolar = selectedSolarIndex != -1 ? missionIni.Solars[selectedSolarIndex] : null;
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Solars", selectedSolar is not null ? selectedSolar.Nickname : ""))
        {
            for (var index = 0; index < missionIni.Solars.Count; index++)
            {
                var arch = missionIni.Solars[index];
                var selected = arch == selectedSolar;

                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedSolarIndex = index;
                selectedSolar = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedSolar is null)
        {
            return;
        }

        ImGui.PushID(selectedSolarIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedSolar.Nickname, 150f);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedSolar.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.System,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Faction", undoBuffer, () => ref selectedSolar.Faction, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Faction.Length is 0 ||
                                                     gameData.GameData.Items.Factions.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Faction,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedSolar.Archetype, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Archetype.Length is 0 ||
                                                     gameData.GameData.Items.Archetypes.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Archetype,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Base", undoBuffer, () => ref selectedSolar.Base, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Base.Length is 0 ||
                                                     gameData.GameData.Items.Bases.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Base,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Loadout", undoBuffer, () => ref selectedSolar.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Loadout.Length is 0 ||
                                                     gameData.GameData.Items.Loadouts.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Loadout,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextIdUndo("Voice", undoBuffer, () => ref selectedSolar.Voice, 150f);
        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedSolar.Pilot, 150f);
        Controls.InputTextIdUndo("Costume Head", undoBuffer, () => ref selectedSolar.Costume[0], 150f);
        Controls.InputTextIdUndo("Costume Body", undoBuffer, () => ref selectedSolar.Costume[1], 150f);
        Controls.InputTextIdUndo("Costume Accessory", undoBuffer, () => ref selectedSolar.Costume[2], 150f);
        Controls.InputTextIdUndo("Visit", undoBuffer, () => ref selectedSolar.Visit, 150f);

        ImGui.SetNextItemWidth(100f);
        Controls.IdsInputStringUndo("String ID", gameData, popup, undoBuffer, () => ref selectedSolar.StringId);

        ImGui.SetNextItemWidth(100f);
        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedSolar.Radius);

        ImGui.NewLine();

        Controls.InputStringList("Labels", undoBuffer, selectedSolar.Labels);

        ImGui.SetNextItemWidth(200f);
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedSolar.Position);

        ImGui.SetNextItemWidth(200f);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedSolar.Orientation);

        ImGui.PopID();
    }

    private int selectedShipIndex = -1;

    private void RenderMissionShipManager()
    {
        if (ImGui.Button("Create New Ship"))
        {
            selectedShipIndex = missionIni.Ships.Count;
            undoBuffer.Commit(new ListAdd<MissionShip>("Ship", missionIni.Ships, new()));
        }

        ImGui.BeginDisabled(selectedShipIndex == -1);

        if (ImGui.Button("Delete Ship"))
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

        ImGui.EndDisabled();

        if (selectedShipIndex >= missionIni.Ships.Count)
            selectedShipIndex = -1;

        var selectedShip = selectedShipIndex != -1 ? missionIni.Ships[selectedShipIndex] : null;
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Ships", selectedShip is not null ? selectedShip.Nickname : ""))
        {
            for (var index = 0; index < missionIni.Ships.Count; index++)
            {
                var arch = missionIni.Ships[index];
                var selected = arch == selectedShip;

                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                selectedShipIndex = index;
                selectedShip = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedShip is null)
        {
            return;
        }

        ImGui.PushID(selectedShipIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedShip.Nickname, 150f);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedShip.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname == selectedShip.System));

        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("NPC", selectedShip.NPC ?? ""))
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

        ImGui.NewLine();

        Controls.InputStringList("Labels", undoBuffer, selectedShip.Labels);

        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedShip.Position);

        ImGui.NewLine();

        ImGui.Text("Relative Position:");

        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedShip.Position.Length() is not 0f);

        Controls.InputTextIdUndo("Obj", undoBuffer, () => ref selectedShip.RelativePosition.ObjectName, 150f);
        // Don't think it's possible to validate this one, as it could refer to any solar object in any system

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Min Range", undoBuffer, () => ref selectedShip.RelativePosition.MinRange);

        ImGui.SetNextItemWidth(150f);
        Controls.InputFloatUndo("Max Range", undoBuffer, () => ref selectedShip.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        ImGui.NewLine();
        ImGui.SetNextItemWidth(200f);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedShip.Orientation);
        Controls.CheckboxUndo("Random Name", undoBuffer, () => ref selectedShip.RandomName);
        Controls.CheckboxUndo("Jumper", undoBuffer, () => ref selectedShip.Jumper);
        ImGui.SetNextItemWidth(100f);
        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedShip.Radius);
        Controls.InputTextIdUndo("Arrival Object", undoBuffer, () => ref selectedShip.ArrivalObj.Object, 150f);
        ImGui.BeginDisabled(string.IsNullOrEmpty(selectedShip.ArrivalObj.Object));
        Controls.InputIntUndo("Undock Index", undoBuffer, () => ref selectedShip.ArrivalObj.Index);
        ImGui.EndDisabled();
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Initial Objectives", selectedShip.InitObjectives ?? ""))
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

        ImGui.Text("Cargo");

        if (selectedShip.Cargo.Count is not 0)
        {
            for (var i = 0; i < selectedShip.Cargo.Count; i++)
            {
                var cargo = selectedShip.Cargo[i];
                ImGui.PushID(i);
                Controls.InputTextIdUndo("##Cargo", undoBuffer, () => ref cargo.Cargo, 150f);
                MissionEditorHelpers.AlertIfInvalidRef(() =>
                    gameData.GameData.Items.Equipment.Any(x =>
                        x.Nickname.Equals(cargo.Cargo, StringComparison.InvariantCultureIgnoreCase)));
                ImGui.SameLine();
                ImGui.PushItemWidth(75f);
                Controls.InputIntUndo("##Count", undoBuffer, () => ref cargo.Count);

                if (cargo.Count < 0)
                {
                    cargo.Count = 0;
                }

                ImGui.PopID();
                selectedShip.Cargo[i] = cargo;
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedShip.Cargo, undoBuffer);

        ImGui.PopID();
    }

    private int objectiveListIndex = -1;

    private void RenderObjectiveListManager()
    {
        if (ImGui.Button("New Objective List"))
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

        ImGui.BeginDisabled(objectiveListIndex == -1);

        if (ImGui.Button("Delete Objective List"))
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

        ImGui.EndDisabled();

        if (objectiveListIndex >= objLists.Count)
            objectiveListIndex = -1;

        ImGui.PushID(objectiveListIndex);
        var selectedObjList = objectiveListIndex != -1 ? objLists[objectiveListIndex] : null;
        ImGui.SetNextItemWidth(150f);



        if (ImGui.BeginCombo("Objective Lists", selectedObjList is not null ? selectedObjList.Nickname : ""))
        {
            for (var index = 0; index < objLists.Count; index++)
            {
                var arch = objLists[index];
                var selected = arch == selectedObjList;

                if (!ImGui.Selectable(arch?.Nickname, selected))
                {
                    continue;
                }

                objectiveListIndex = index;
                selectedObjList = arch;
            }

            ImGui.EndCombo();
        }

        if (selectedObjList is null)
        {
            ImGui.PopID();
            return;
        }

        var objListTypes = Enum.GetNames<ObjListCommands>();

        for (var index = 0; index < selectedObjList.Directives.Count; index++)
        {
            ImGui.PushID(index);
            ImGui.Separator();

            var obj = selectedObjList.Directives[index];

            var typeIndex = (int) obj.Command;

            ImGui.SetNextItemWidth(150f);
            ImGui.Combo("Command Type", ref typeIndex, objListTypes, objListTypes.Length);

            if ((int) obj.Command != typeIndex)
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
