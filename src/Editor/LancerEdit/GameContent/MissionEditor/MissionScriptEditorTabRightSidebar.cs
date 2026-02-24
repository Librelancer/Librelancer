using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.Lookups;
using LancerEdit.GameContent.Popups;
using LibreLancer.Data;
using LibreLancer.Data.GameData.Items;
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

        if (SidebarHeader("Ship Manager"))
        {
            ImGui.PushID(1);
            RenderMissionShipManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (SidebarHeader("Solar Manager"))
        {
            ImGui.PushID(2);
            RenderMissionSolarManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (SidebarHeader("Loot Manager"))
        {
            ImGui.PushID(3);
            RenderLootManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (SidebarHeader("Dialog Manager"))
        {
            ImGui.PushID(4);
            RenderDialogManager();
            ImGui.PopID();
        }

        ImGui.NewLine();

        if (SidebarHeader("Object Directive List"))
        {
            ImGui.PushID(5);
            RenderObjectiveListManager();
            ImGui.PopID();
        }

        ImGui.EndChild();
    }

    private ScriptDialog selectedDialog;

    private void RenderDialogManager()
    {
        DictionaryRemove<ScriptDialog> Delete()
        {
            return new(
                "Dialog",
                missionIni.Dialogs, selectedDialog,
                () => ref selectedDialog);
        }
        ItemList("Dialog", missionIni.Dialogs, () => ref selectedDialog, Delete);

        if (selectedDialog is null)
        {
            return;
        }

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Dialogs, selectedDialog);
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
                if (missionIni.Ships.TryGetValue(line.Source, out var src))
                {
                    gameData.Sounds.PlayVoiceLine(src.NPC.Voice, line.Line);

                }
                else if (missionIni.Solars.TryGetValue(line.Source, out var src2))
                {
                    gameData.Sounds.PlayVoiceLine(src2.Voice, line.Line);
                }
            }

            ImGui.PopID();

            if (index + 1 != selectedDialog.Lines.Count)
            {
                ImGui.NewLine();
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedDialog.Lines, undoBuffer);
    }

    private ScriptLoot selectedLoot;

    // TODO
    bool ValidLoot(Equipment eq) => true;

    private void RenderLootManager()
    {
        DictionaryRemove<ScriptLoot> Delete()
        {
            return new(
                "Loot",
                missionIni.Loots, selectedLoot,
                () => ref selectedLoot);
        }
        ItemList("Loot", missionIni.Loots, () => ref selectedLoot, Delete);

        if (selectedLoot is null)
        {
            return;
        }


        if (!Controls.BeginEditorTable("loot"))
            return;
        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Loots, selectedLoot);
        gameData.Equipment.DrawUndo("Archetype", undoBuffer,
            () => ref selectedLoot.Archetype);
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedLoot.StringId,
            inputWidth: 150f);

        ImGui.BeginDisabled(!string.IsNullOrEmpty(selectedLoot.RelPosObj) && selectedLoot.RelPosOffset != Vector3.Zero);
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedLoot.Position);
        ImGui.EndDisabled();

        ImGui.Text("Relative Position");
        ImGui.BeginDisabled(selectedLoot.Position != Vector3.Zero);

        Controls.InputTextIdUndo("Object", undoBuffer, () => ref selectedLoot.RelPosObj, 150f);

        Controls.InputFloat3Undo("Offset", undoBuffer, () => ref selectedLoot.RelPosOffset);

        ImGui.EndDisabled();

        Controls.TableSeparator();

        Controls.InputIntUndo("Equip Amount", undoBuffer, () => ref selectedLoot.EquipAmount);

        Controls.SliderFloatUndo("Health", undoBuffer, () => ref selectedLoot.Health, 0f, 1f);

        Controls.CheckboxUndo("Can Jettison", undoBuffer, () => ref selectedLoot.CanJettison);

        Controls.EndEditorTable();
    }

    private ScriptSolar selectedSolar = null;
    private ScriptSolar lookupSolar = null;

    private void RenderMissionSolarManager()
    {
        DictionaryRemove<ScriptSolar> Delete()
        {
            return new(
                "Solar",
                missionIni.Solars, selectedSolar,
                () => ref selectedSolar);
        }
        ItemList("Solar", missionIni.Solars, () => ref selectedSolar, Delete);

        if (selectedSolar is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("Solar"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Solars, selectedSolar);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedSolar.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.System,
                                                             StringComparison.InvariantCultureIgnoreCase)));
        gameData.Factions.DrawUndo("Faction", undoBuffer, () => ref selectedSolar.Faction, true);
        Controls.DisabledInputTextId("Archetype", selectedSolar.Archetype?.Nickname ?? "(none)");
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        // empty
        ImGui.TableNextColumn();
        if (ImGui.Button("Select Archetype"))
        {
            popup.OpenPopup(new ArchetypeSelection(
                x => undoBuffer.Set("Archetype", () => ref selectedSolar.Archetype, x),
                selectedSolar.Archetype,
                gameData));
        }

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
        Controls.TableSeparator();
        Controls.InputTextIdUndo("Voice", undoBuffer, () => ref selectedSolar.Voice);
        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedSolar.Pilot);
        Controls.TableSeparatorText("Costume");
        gameData.Costumes.Draw("costume", undoBuffer,
            () => ref selectedSolar.Costume.Head,
            () =>  ref selectedSolar.Costume.Body,
            () => ref selectedSolar.Costume.Accessory);
        Controls.TableSeparator();
        Controls.InputTextIdUndo("Visit", undoBuffer, () => ref selectedSolar.Visit);

        Controls.IdsInputStringUndo("String ID", gameData, popup, undoBuffer, () => ref selectedSolar.IdsName);
        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedSolar.Radius);
        Controls.TableSeparator();
        Controls.InputStringList("Labels", undoBuffer, selectedSolar.Labels);
        Controls.TableSeparator();
        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedSolar.Position);
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedSolar.Orientation);

        Controls.EndEditorTable();
    }

    private ScriptShip selectedShip;

    private void RenderMissionShipManager()
    {
        DictionaryRemove<ScriptShip> Delete()
        {
            return new(
                "Ship",
                missionIni.Ships, selectedShip,
                () => ref selectedShip);
        }
        ItemList("Ship", missionIni.Ships, () => ref selectedShip, Delete);

        if (selectedShip is null)
        {
            return;
        }

        if (!Controls.BeginEditorTable("Ship"))
            return;

        Controls.InputItemNickname("Nickname", undoBuffer, missionIni.Ships, selectedShip);
        Controls.InputTextIdUndo("System", undoBuffer, () => ref selectedShip.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
                                                     gameData.GameData.Items.Systems.Any(x =>
                                                         x.Nickname == selectedShip.System));

        Controls.EditControlSetup("NPC", 0);
        if (ImGui.BeginCombo("##npc", selectedShip.NPC?.Nickname ?? $"{Icons.Warning} None"))
        {
            foreach (var npc in missionIni.Npcs)
            {
                if (ImGui.Selectable(npc.Key, selectedShip.NPC == npc.Value)
                    && selectedShip.NPC != npc.Value)
                {
                    undoBuffer.Set("NPC", () => ref selectedShip.NPC, npc.Value);
                }
            }
            ImGui.EndCombo();
        }


        Controls.InputStringList("Labels", undoBuffer, selectedShip.Labels);

        Controls.TableSeparatorText("Absolute Position");

        Controls.InputFloat3Undo("Position", undoBuffer, () => ref selectedShip.Position);


        Controls.TableSeparatorText("Relative Position");

        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedShip.Position.Length() is not 0f);
        Controls.InputTextIdUndo("Obj", undoBuffer, () => ref selectedShip.RelativePosition.ObjectName, 150f);
        // Don't think it's possible to validate this one, as it could refer to any solar object in any system
        Controls.InputFloatUndo("Min Range", undoBuffer, () => ref selectedShip.RelativePosition.MinRange);
        Controls.InputFloatUndo("Max Range", undoBuffer, () => ref selectedShip.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        Controls.TableSeparator();
        Controls.InputQuaternionUndo("Orientation", undoBuffer, () => ref selectedShip.Orientation);
        Controls.CheckboxUndo("Random Name", undoBuffer, () => ref selectedShip.RandomName);
        Controls.CheckboxUndo("Jumper", undoBuffer, () => ref selectedShip.Jumper);
        Controls.InputFloatUndo("Radius", undoBuffer, () => ref selectedShip.Radius);
        Controls.InputTextIdUndo("Arrival Object", undoBuffer, () => ref selectedShip.ArrivalObj.Object, 150f);
        ImGui.BeginDisabled(string.IsNullOrEmpty(selectedShip.ArrivalObj.Object));
        Controls.InputIntUndo("Undock Index", undoBuffer, () => ref selectedShip.ArrivalObj.Index);
        ImGui.EndDisabled();

        Controls.EditControlSetup("Initial Objectives", 0);
        if (ImGui.BeginCombo("##objs", selectedShip.InitObjectives ?? ""))
        {
            if (ImGui.Selectable("no_op", selectedShip.InitObjectives == "no_op"))
            {
                undoBuffer.Set("InitObjectives", () => ref selectedShip.InitObjectives, "no_op");
            }

            foreach (var npc in missionIni.ObjLists.Keys
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.InitObjectives == x)))
            {
                undoBuffer.Set("InitObjectives", () => ref selectedShip.InitObjectives, npc);
            }

            ImGui.EndCombo();
        }

        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.InitObjectives is null ||
                                                     selectedShip.InitObjectives.Length is 0 ||
                                                     selectedShip.InitObjectives == "no_op" ||
                                                     missionIni.ObjLists.ContainsKey(selectedShip.InitObjectives));

        Controls.EditControlSetup("Cargo", 0);
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
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedShip.Cargo, undoBuffer);

        Controls.EndEditorTable();
    }

    private ScriptAiCommands selectedObjList;

    class SwapListItems(List<MissionDirective> Directives, int A, int B) : EditorAction
    {
        public override void Commit() => (Directives[A], Directives[B]) = (Directives[B], Directives[A]);
        public override void Undo() => Commit(); // Do the reverse
        public override string ToString() => "Move Directive";
    }

    private void RenderObjectiveListManager()
    {
        DictionaryRemove<ScriptAiCommands> Delete()
        {
            return new(
                "Objective List",
                missionIni.ObjLists, selectedObjList,
                () => ref selectedObjList);
        }
        ItemList("Objective List", missionIni.ObjLists, () => ref selectedObjList, Delete);

        if (selectedObjList is null)
        {
            return;
        }

        var objListTypes = Enum.GetNames<ObjListCommands>();
        int delete = -1, moveUp = -1, moveDown = -1;

        for (var index = 0; index < selectedObjList.Directives.Count; index++)
        {
            ImGui.PushID(index);
            ImGui.Separator();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
            if (ImGuiExt.Button($"{Icons.ArrowUp}", index > 0, ImDrawFlags.RoundCornersLeft))
                moveUp = index;
            ImGui.SameLine();
            if (ImGuiExt.Button($"{Icons.ArrowDown}", index + 1 < selectedObjList.Directives.Count,
                    ImDrawFlags.RoundCornersNone))
                moveDown = index;
            ImGui.SameLine();
            if (ImGuiExt.Button($"{Icons.TrashAlt}", true, ImDrawFlags.RoundCornersRight))
                delete = index;
            ImGui.PopStyleVar();
            ImGui.SameLine();


            var obj = selectedObjList.Directives[index];

            var typeIndex = (int) obj.Command;

            ImGui.SetNextItemWidth(-1);
            ImGui.Combo("##command", ref typeIndex, objListTypes, objListTypes.Length);

            if ((int) obj.Command != typeIndex)
            {
                undoBuffer.Commit(new ListSet<MissionDirective>("Command", selectedObjList.Directives,
                    index, obj, MissionDirective.New((ObjListCommands)typeIndex)));
            }
            DirectiveEditor.EditDirective(obj, undoBuffer);
            ImGui.PopID();
        }

        if (ImGui.Button("Add Command"))
        {
            undoBuffer.Commit(new ListAdd<MissionDirective>("Command", selectedObjList.Directives, new BreakFormationDirective()));
        }

        if (delete != -1)
        {
            undoBuffer.Commit(new ListRemove<MissionDirective>("Command", selectedObjList.Directives,
                delete, selectedObjList.Directives[delete]));
        }
        if (moveUp != -1)
            undoBuffer.Commit(new SwapListItems(selectedObjList.Directives, moveUp, moveUp - 1));
        if(moveDown != -1)
            undoBuffer.Commit(new SwapListItems(selectedObjList.Directives, moveDown, moveDown + 1));
    }
}
