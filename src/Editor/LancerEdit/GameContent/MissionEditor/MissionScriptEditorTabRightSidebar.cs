using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
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
            RenderMissionShipManager();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Solar Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderMissionSolarManager();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Loot Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderLootManager();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Dialog Manager", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderDialogManager();
        }

        ImGui.NewLine();

        if (ImGui.CollapsingHeader("Objective List", ImGuiTreeNodeFlags.DefaultOpen))
        {
            RenderObjectiveListManager();
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
            missionIni.Dialogs.Add(new MissionDialog());
        }

        ImGui.BeginDisabled(selectedDialogIndex == -1);

        if (ImGui.Button("Delete Dialog"))
        {
            win.Confirm("Are you sure you want to delete this dialog?",
                () => { missionIni.Dialogs.RemoveAt(selectedDialogIndex--); });
        }

        ImGui.EndDisabled();

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

        Controls.InputTextId("Nickname##Dialog", ref selectedDialog.Nickname, 150f);
        Controls.InputTextId("System##Dialog", ref selectedDialog.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedDialog.System.Length is 0 ||
                                                     gameData.GameData.Systems.Any(x =>
                                                         x.Nickname == selectedDialog.System));

        for (var index = 0; index < selectedDialog.Lines.Count; index++)
        {
            var line = selectedDialog.Lines[index];
            ImGui.PushID(line.GetHashCode());
            Controls.InputTextId("Source##ID", ref line.Source);
            Controls.InputTextId("Target##ID", ref line.Target);
            Controls.InputTextId("Line##ID", ref line.Line);
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

        MissionEditorHelpers.AddRemoveListButtons(selectedDialog.Lines);

        ImGui.PopID();
    }

    private int selectedLootIndex = -1;

    private void RenderLootManager()
    {
        if (ImGui.Button("Create New Loot"))
        {
            selectedLootIndex = missionIni.Loots.Count;
            missionIni.Loots.Add(new MissionLoot());
        }

        ImGui.BeginDisabled(selectedLootIndex == -1);

        if (ImGui.Button("Delete Loot"))
        {
            win.Confirm("Are you sure you want to delete this loot?",
                () => { missionIni.Loots.RemoveAt(selectedLootIndex--); });
        }

        ImGui.EndDisabled();

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

        Controls.InputTextId("Nickname##Loot", ref selectedLoot.Nickname, 150f);
        Controls.InputTextId("Archetype##Loot", ref selectedLoot.Archetype, 150f);
        Controls.IdsInputString("Name##Loot", gameData, popup, ref selectedLoot.StringId,
            x => selectedLoot.StringId = x, inputWidth: 150f);

        ImGui.BeginDisabled(!string.IsNullOrEmpty(selectedLoot.RelPosObj) && selectedLoot.RelPosOffset != Vector3.Zero);
        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat3("Position##Loot", ref selectedLoot.Position);
        ImGui.EndDisabled();

        ImGui.Text("Relative Position");
        ImGui.BeginDisabled(selectedLoot.Position != Vector3.Zero);

        Controls.InputTextId("Object##LootRel", ref selectedLoot.RelPosObj, 150f);

        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat3("Position##LootRel", ref selectedLoot.RelPosOffset);

        ImGui.EndDisabled();

        ImGui.NewLine();
        ImGui.SetNextItemWidth(200f);
        ImGui.InputInt("Equip Amount##Loot", ref selectedLoot.EquipAmount);

        ImGui.SetNextItemWidth(200f);
        ImGui.SliderFloat("Health##Loot", ref selectedLoot.Health, 0f, 1f);

        ImGui.Checkbox("Can Jettison##Loot", ref selectedLoot.CanJettison);

        ImGui.PopID();
    }

    private int selectedSolarIndex = -1;

    private void RenderMissionSolarManager()
    {
        if (ImGui.Button("Create New Solar"))
        {
            selectedSolarIndex = missionIni.Solars.Count;
            missionIni.Solars.Add(new MissionSolar());
        }

        ImGui.BeginDisabled(selectedSolarIndex == -1);

        if (ImGui.Button("Delete Solar"))
        {
            win.Confirm("Are you sure you want to delete this solar?",
                () => { missionIni.Solars.RemoveAt(selectedSolarIndex--); });
        }

        ImGui.EndDisabled();

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

        Controls.InputTextId("Nickname##Solar", ref selectedSolar.Nickname, 150f);
        Controls.InputTextId("System##Solar", ref selectedSolar.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.System.Length is 0 ||
                                                     gameData.GameData.Systems.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.System,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Faction##Solar", ref selectedSolar.Faction, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Faction.Length is 0 ||
                                                     gameData.GameData.Factions.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Faction,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Archetype##Solar", ref selectedSolar.Archetype, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Archetype.Length is 0 ||
                                                     gameData.GameData.Archetypes.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Archetype,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Base##Solar", ref selectedSolar.Base, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Base.Length is 0 ||
                                                     gameData.GameData.Bases.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Base,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Loadout##Solar", ref selectedSolar.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Loadout.Length is 0 ||
                                                     gameData.GameData.Loadouts.Any(x =>
                                                         x.Nickname.Equals(selectedSolar.Loadout,
                                                             StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Voice##Solar", ref selectedSolar.Voice, 150f);
        Controls.InputTextId("Pilot##Solar", ref selectedSolar.Pilot, 150f);
        Controls.InputTextId("Costume Head##Solar", ref selectedSolar.Costume[0], 150f);
        Controls.InputTextId("Costume Body##Solar", ref selectedSolar.Costume[1], 150f);
        Controls.InputTextId("Costume Accessory##Solar", ref selectedSolar.Costume[2], 150f);
        Controls.InputTextId("Visit##Solar", ref selectedSolar.Visit, 150f);

        ImGui.SetNextItemWidth(100f);
        ImGui.InputInt("String ID##Solar", ref selectedSolar.StringId);

        ImGui.SetNextItemWidth(100f);
        ImGui.InputFloat("Radius##Solar", ref selectedSolar.Radius);

        ImGui.NewLine();

        Controls.InputStringList("Labels", selectedSolar.Labels);

        ImGui.SetNextItemWidth(200f);
        ImGui.InputFloat3("Position##Solar", ref selectedSolar.Position);

        ImGui.SetNextItemWidth(200f);
        Controls.InputFlQuaternion("Orientation##Solar", ref selectedSolar.Orientation);

        ImGui.PopID();
    }

    private int selectedShipIndex = -1;

    private void RenderMissionShipManager()
    {
        if (ImGui.Button("Create New Ship"))
        {
            selectedShipIndex = missionIni.Ships.Count;
            missionIni.Ships.Add(new MissionShip());
        }

        ImGui.BeginDisabled(selectedShipIndex == -1);

        if (ImGui.Button("Delete Ship"))
        {
            win.Confirm("Are you sure you want to delete this ship?",
                () => { missionIni.NPCs.RemoveAt(selectedShipIndex--); });
        }

        ImGui.EndDisabled();

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

        Controls.InputTextId("Nickname##Ship", ref selectedShip.Nickname, 150f);
        Controls.InputTextId("System##Ship", ref selectedShip.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
                                                     gameData.GameData.Systems.Any(x =>
                                                         x.Nickname == selectedShip.System));

        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("NPC##Ship", selectedShip.NPC ?? ""))
        {
            foreach (var npc in missionIni.NPCs
                         .Select(x => x.Nickname)
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.NPC == x)))
            {
                selectedShip.NPC = npc;
            }

            ImGui.EndCombo();
        }

        MissionEditorHelpers.AlertIfInvalidRef(() => missionIni.NPCs.Any(x => x.Nickname == selectedShip.NPC));

        ImGui.NewLine();

        Controls.InputStringList("Labels", selectedShip.Labels);

        ImGui.InputFloat3("Position##Ship", ref selectedShip.Position);

        ImGui.NewLine();

        ImGui.Text("Relative Position:");

        // Disable relative data if absolute data is provided
        ImGui.BeginDisabled(selectedShip.Position.Length() is not 0f);

        Controls.InputTextId("Obj##Ship", ref selectedShip.RelativePosition.ObjectName, 150f);
        // Don't think it's possible to validate this one, as it could refer to any solar object in any system

        ImGui.SetNextItemWidth(150f);
        ImGui.InputFloat("Min Range##Ship", ref selectedShip.RelativePosition.MinRange);

        ImGui.SetNextItemWidth(150f);
        ImGui.InputFloat("Max Range##Ship", ref selectedShip.RelativePosition.MaxRange);

        ImGui.EndDisabled();

        ImGui.NewLine();
        ImGui.SetNextItemWidth(200f);
        Controls.InputFlQuaternion("Orientation##Ship", ref selectedShip.Orientation);
        ImGui.Checkbox("Random Name##Ship", ref selectedShip.RandomName);
        ImGui.Checkbox("Jumper##Ship", ref selectedShip.Jumper);
        ImGui.SetNextItemWidth(100f);
        ImGui.InputFloat("Radius##Ship", ref selectedShip.Radius);
        Controls.InputTextId("Arrival Object##Ship", ref selectedShip.ArrivalObj, 150f);

        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Initial Objectives##Ship", selectedShip.InitObjectives ?? ""))
        {
            if (ImGui.Selectable("no_op", selectedShip.InitObjectives == "no_op"))
            {
                selectedShip.InitObjectives = "no_op";
            }

            foreach (var npc in missionIni.ObjLists
                         .Select(x => x.Nickname)
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.InitObjectives == x)))
            {
                selectedShip.InitObjectives = npc;
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
                Controls.InputTextId("##Cargo", ref cargo.Cargo, 150f);
                MissionEditorHelpers.AlertIfInvalidRef(() =>
                    gameData.GameData.Equipment.Any(x =>
                        x.Nickname.Equals(cargo.Cargo, StringComparison.InvariantCultureIgnoreCase)));
                ImGui.SameLine();
                ImGui.PushItemWidth(75f);
                ImGui.InputInt("##Count", ref cargo.Count);

                if (cargo.Count < 0)
                {
                    cargo.Count = 0;
                }

                ImGui.PopID();
                selectedShip.Cargo[i] = cargo;
            }
        }

        MissionEditorHelpers.AddRemoveListButtons(selectedShip.Cargo);

        ImGui.PopID();
    }

    private int objectiveListIndex = -1;

    private void RenderObjectiveListManager()
    {
        if (ImGui.Button("New Objective List"))
        {
            objectiveListIndex = missionIni.ObjLists.Count;
            missionIni.ObjLists.Add(new ObjList());
        }

        ImGui.BeginDisabled(objectiveListIndex == -1);

        if (ImGui.Button("Delete Objective List"))
        {
            win.Confirm("Are you sure you want to delete this ObjList?",
                () => { missionIni.ObjLists.RemoveAt(objectiveListIndex--); });
        }

        ImGui.EndDisabled();

        ImGui.PushID(objectiveListIndex);
        var selectedObjList = objectiveListIndex != -1 ? missionIni.ObjLists[objectiveListIndex] : null;
        ImGui.SetNextItemWidth(150f);

        if (ImGui.BeginCombo("Objective Lists", selectedObjList is not null ? selectedObjList.Nickname : ""))
        {
            for (var index = 0; index < missionIni.ObjLists.Count; index++)
            {
                var arch = missionIni.ObjLists[index];
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

        for (var index = 0; index < selectedObjList.Commands.Count; index++)
        {
            ImGui.PushID(index);
            ImGui.Separator();

            var obj = selectedObjList.Commands[index];

            var typeIndex = (int) obj.Command;

            ImGui.SetNextItemWidth(150f);
            ImGui.Combo("Command Type", ref typeIndex, objListTypes, objListTypes.Length);

            if ((int) obj.Command != typeIndex)
            {
                obj.Command = (ObjListCommands) typeIndex;
                obj.Entry = new Entry(new Section("ObjList"), obj.Command.ToString());
            }

            if (DrawDirective(index, obj))
            {
                selectedObjList.Commands.RemoveAt(index--);
            }

            ImGui.PopID();
        }

        if (ImGui.Button("Add Command"))
        {
            selectedObjList.Commands.Add(new ObjCmd()
            {
                Command = new(),
                Entry = new(new Section("ObjList"), "BreakFormation"),
            });
        }

        ImGui.PopID();
        return;

        bool DrawDirective(int id, ObjCmd cmd)
        {
            // begin border/frame/whatever
            ImGui.PushID($"obj-list-{id}-{(int) cmd.Command}");

            ImGui.SameLine();

            if (ImGui.Button($"{Icons.TrashAlt}"))
            {
                ImGui.PopID();
                return true;
            }

            switch (cmd.Command)
            {
                case ObjListCommands.BreakFormation:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new StringValue("no_params"));
                    }

                    break;
                }
                case ObjListCommands.MakeNewFormation:
                {
                    if (cmd.Entry.Count is 0)
                    {
                        cmd.Entry.Add(new StringValue("fighter_basic"));
                    }

                    var formation = cmd.Entry[0].ToString()!;
                    Controls.InputTextId("Formation", ref formation);

                    for (int i = 1; i < cmd.Entry.Count; i++)
                    {
                        ImGui.PushID(i);

                        var entry = cmd.Entry[i];
                        var val = entry.ToString()!;

                        // TODO: Replace with ship selector
                        Controls.InputTextId("##ship", ref val);
                        ImGui.SameLine();

                        if (ImGui.Button($"{Icons.TrashAlt}"))
                        {
                            cmd.Entry.Remove(entry);
                            i--;
                        }

                        ImGui.PopID();
                    }

                    if (ImGui.Button($"{Icons.PlusCircle} Add Ship"))
                    {
                        cmd.Entry.Add(new StringValue("Player"));
                    }

                    break;
                }
                case ObjListCommands.FollowPlayer:
                {
                    if (cmd.Entry.Count is 0)
                    {
                        cmd.Entry.Add(new StringValue("fighter_basic"));
                    }

                    var formation = cmd.Entry[0].ToString()!;
                    Controls.InputTextId("Formation", ref formation);

                    for (int i = 1; i < cmd.Entry.Count; i++)
                    {
                        ImGui.PushID(i);

                        var entry = cmd.Entry[i];
                        var val = entry.ToString()!;

                        // TODO: Replace with ship selector
                        Controls.InputTextId("##ship", ref val);
                        ImGui.SameLine();

                        if (ImGui.Button($"{Icons.TrashAlt}"))
                        {
                            cmd.Entry.Remove(entry);
                            i--;
                        }

                        ImGui.PopID();
                    }

                    if (ImGui.Button($"{Icons.PlusCircle} Add Ship"))
                    {
                        cmd.Entry.Add(new StringValue("Player"));
                    }

                    break;
                }
                case ObjListCommands.GotoShip:
                {
                    if (cmd.Entry.Count == 0)
                    {
                        cmd.Entry.Add(new StringValue("goto"));
                        cmd.Entry.Add(new StringValue("Player"));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new BooleanValue(false));
                    }

                    if (cmd.Entry.Count == 4)
                    {
                        cmd.Entry.Add(new SingleValue(-1.0f, null));
                    }

                    var combo = new[]
                    {
                        "goto",
                        "goto_cruise",
                        "goto_no_cruise"
                    };

                    var type = cmd.Entry[0].ToString();
                    var target = cmd.Entry[1].ToString();
                    var distance = cmd.Entry[2].ToSingle();
                    var unk = cmd.Entry[3].ToBoolean();
                    var maxThrottle = cmd.Entry[4].ToSingle();

                    var typeIndex = Array.FindIndex(combo,
                        x => type!.Equals(x, StringComparison.InvariantCultureIgnoreCase));

                    if (typeIndex == -1)
                    {
                        typeIndex = 0;
                    }

                    ImGui.Combo("Type", ref typeIndex, combo, combo.Length);
                    Controls.InputTextId("Target", ref target);
                    ImGui.InputFloat("Distance", ref distance);
                    ImGui.Checkbox("Unknown 2", ref unk);
                    ImGui.InputFloat("Max Throttle", ref maxThrottle);

                    break;
                }
                case ObjListCommands.StayInRange:
                {
                    if (cmd.Entry.Count == 0)
                    {
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new BooleanValue(false));
                    }

                    // Vanilla mission files often omit the boolean parameter on the end
                    switch (cmd.Entry.Count)
                    {
                        case 2:
                        case 4:
                            cmd.Entry.Add(new BooleanValue(false));
                            break;
                    }

                    // 0 = Point, 1 = Object
                    var radioIndex = Convert.ToInt32(cmd.Entry.Count == 3);

                    RenderPointOrObject(ref radioIndex, cmd.Entry);

                    var isObj = radioIndex == 1;
                    var radius = cmd.Entry[isObj ? 1 : 3].ToSingle();
                    var unk = cmd.Entry[isObj ? 2 : 4].ToBoolean();

                    ImGui.InputFloat("Radius", ref radius);
                    ImGui.Checkbox("Unknown", ref unk);

                    cmd.Entry[isObj ? 1 : 3] = new SingleValue(radius, null);
                    cmd.Entry[isObj ? 2 : 4] = new BooleanValue(unk);
                    break;
                }
                case ObjListCommands.SetLifetime:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new Int32Value(0));
                    }

                    var val = cmd.Entry[0].ToInt32();
                    ImGui.InputInt("Lifetime", ref val, 1, 10);
                    cmd.Entry[0] = new Int32Value(val);
                    break;
                }
                case ObjListCommands.GotoVec:
                {
                    if (cmd.Entry.Count != 7)
                    {
                        if (cmd.Entry.Count == 6)
                        {
                            cmd.Entry.Add(new SingleValue(-1.0f, null));
                        }
                        else
                        {
                            cmd.Entry.Clear();

                            cmd.Entry.Add(new StringValue("goto"));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new BooleanValue(false));
                            cmd.Entry.Add(new SingleValue(-1.0f, null));
                        }
                    }

                    var combo = new[]
                    {
                        "goto",
                        "goto_cruise",
                        "goto_no_cruise"
                    };

                    var type = cmd.Entry[0].ToString();
                    var distance = cmd.Entry[1].ToSingle();
                    var vec = new Vector3(cmd.Entry[2].ToSingle(), cmd.Entry[3].ToSingle(), cmd.Entry[4].ToSingle());
                    var unk = cmd.Entry[5].ToBoolean();
                    var maxThrottle = cmd.Entry[6].ToSingle();

                    var typeIndex = Array.FindIndex(combo,
                        x => type!.Equals(x, StringComparison.InvariantCultureIgnoreCase));

                    if (typeIndex == -1)
                    {
                        typeIndex = 0;
                    }

                    ImGui.Combo("Type", ref typeIndex, combo, combo.Length);
                    ImGui.InputFloat("Distance", ref distance);
                    ImGui.InputFloat3("Offset", ref vec, "%.0f");
                    ImGui.Checkbox("Unknown 2", ref unk);
                    ImGui.InputFloat("Max Throttle", ref maxThrottle);

                    cmd.Entry[0] = new StringValue(combo[typeIndex]);
                    cmd.Entry[1] = new SingleValue(distance, null);
                    cmd.Entry[2] = new SingleValue(vec.X, null);
                    cmd.Entry[3] = new SingleValue(vec.Y, null);
                    cmd.Entry[4] = new SingleValue(vec.Z, null);
                    cmd.Entry[5] = new BooleanValue(unk);
                    cmd.Entry[6] = new SingleValue(maxThrottle, null);

                    break;
                }
                case ObjListCommands.SetPriority:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new StringValue("NORMAL"));
                    }

                    var val = cmd.Entry[0].ToString()!.Equals("ALWAYS_EXECUTE", StringComparison.OrdinalIgnoreCase);
                    ImGui.Checkbox("Always Execute", ref val);
                    cmd.Entry[0] = new StringValue(val ? "ALWAYS_EXECUTE" : "NORMAL");
                    break;
                }
                case ObjListCommands.Follow:
                {
                    if (cmd.Entry.Count != 6)
                    {
                        if (cmd.Entry.Count == 5)
                        {
                            cmd.Entry.Add(new SingleValue(50000f, null));
                        }
                        else
                        {
                            cmd.Entry.Clear();

                            cmd.Entry.Add(new StringValue("Player"));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0.0f, null));
                        }
                    }

                    var target = cmd.Entry[0].ToString();
                    var distance = cmd.Entry[1].ToSingle();
                    var vec = new Vector3(cmd.Entry[2].ToSingle(), cmd.Entry[3].ToSingle(), cmd.Entry[4].ToSingle());

                    Controls.InputTextId("Target", ref target);
                    ImGui.InputFloat("Distance", ref distance);
                    ImGui.InputFloat3("Offset", ref vec, "%.0f");

                    break;
                }
                case ObjListCommands.Delay:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new Int32Value(0));
                    }

                    var val = cmd.Entry[0].ToInt32();
                    ImGui.InputInt("Delay", ref val, 1, 10);
                    cmd.Entry[0] = new Int32Value(val);
                    break;
                }
                case ObjListCommands.Dock:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new StringValue(""));
                    }

                    var val = cmd.Entry[0].ToString();
                    Controls.InputTextId("Dock Target", ref val, 150f);
                    cmd.Entry[0] = new StringValue(val);
                    break;
                }
                case ObjListCommands.StayOutOfRange:
                {
                    if (cmd.Entry.Count == 0)
                    {
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new SingleValue(0f, null));
                        cmd.Entry.Add(new BooleanValue(false));
                    }

                    // Vanilla mission files often omit the boolean parameter on the end
                    switch (cmd.Entry.Count)
                    {
                        case 2:
                        case 4:
                            cmd.Entry.Add(new BooleanValue(false));
                            break;
                    }

                    // 0 = Point, 1 = Object
                    var radioIndex = Convert.ToInt32(cmd.Entry.Count == 3);

                    RenderPointOrObject(ref radioIndex, cmd.Entry);

                    var isObj = radioIndex == 1;
                    var radius = cmd.Entry[isObj ? 1 : 3].ToSingle();
                    var unk = cmd.Entry[isObj ? 2 : 4].ToBoolean();

                    ImGui.InputFloat("Radius", ref radius);
                    ImGui.Checkbox("Unknown", ref unk);

                    cmd.Entry[isObj ? 1 : 3] = new SingleValue(radius, null);
                    cmd.Entry[isObj ? 2 : 4] = new BooleanValue(unk);
                    break;
                }
                case ObjListCommands.Avoidance:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new BooleanValue(false));
                    }

                    var val = cmd.Entry[0].ToBoolean();
                    ImGui.Checkbox("Avoidance", ref val);
                    cmd.Entry[0] = new BooleanValue(val);
                    break;
                }
                case ObjListCommands.Idle:
                {
                    if (cmd.Entry.Count != 1)
                    {
                        cmd.Entry.Add(new StringValue("no_params"));
                    }

                    break;
                }
                case ObjListCommands.GotoSpline:
                {
                    if (cmd.Entry.Count != 16)
                    {
                        if (cmd.Entry.Count == 15)
                        {
                            cmd.Entry.Add(new SingleValue(-1f, null));
                        }
                        else
                        {
                            cmd.Entry.Clear();

                            cmd.Entry.Add(new StringValue("goto"));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(0f, null));
                            cmd.Entry.Add(new SingleValue(1000f, null));
                            cmd.Entry.Add(new BooleanValue(true));
                            cmd.Entry.Add(new SingleValue(-1f, null));
                        }

                    }

                    var combo = new[]
                    {
                        "goto",
                        "goto_cruise",
                        "goto_no_cruise"
                    };

                    var type = cmd.Entry[0].ToString();
                    var pointA = new Vector3(cmd.Entry[1].ToSingle(), cmd.Entry[2].ToSingle(), cmd.Entry[3].ToSingle());
                    var pointB = new Vector3(cmd.Entry[4].ToSingle(), cmd.Entry[5].ToSingle(), cmd.Entry[6].ToSingle());
                    var pointC = new Vector3(cmd.Entry[7].ToSingle(), cmd.Entry[8].ToSingle(), cmd.Entry[9].ToSingle());
                    var pointD = new Vector3(cmd.Entry[10].ToSingle(), cmd.Entry[11].ToSingle(),
                        cmd.Entry[12].ToSingle());
                    var range = cmd.Entry[13].ToSingle();
                    var unk = cmd.Entry[14].ToBoolean();
                    var maxThrottle = cmd.Entry[15].ToSingle();

                    var typeIndex = Array.FindIndex(combo,
                        x => type!.Equals(x, StringComparison.InvariantCultureIgnoreCase));

                    if (typeIndex == -1)
                    {
                        typeIndex = 0;
                    }

                    ImGui.Combo("Type", ref typeIndex, combo, combo.Length);

                    ImGui.InputFloat3("A", ref pointA, "%.0f");
                    ImGui.InputFloat3("B", ref pointB, "%.0f");
                    ImGui.InputFloat3("C", ref pointC, "%.0f");
                    ImGui.InputFloat3("D", ref pointD, "%.0f");
                    ImGui.InputFloat("Range", ref range);
                    ImGui.Checkbox("Unknown", ref unk);
                    ImGui.InputFloat("Max Throttle", ref maxThrottle);

                    cmd.Entry[0] = new StringValue(combo[typeIndex]);
                    cmd.Entry[1] = new SingleValue(pointA.X, null);
                    cmd.Entry[2] = new SingleValue(pointA.Y, null);
                    cmd.Entry[3] = new SingleValue(pointA.Z, null);
                    cmd.Entry[4] = new SingleValue(pointB.X, null);
                    cmd.Entry[5] = new SingleValue(pointB.Y, null);
                    cmd.Entry[6] = new SingleValue(pointB.Z, null);
                    cmd.Entry[7] = new SingleValue(pointC.X, null);
                    cmd.Entry[8] = new SingleValue(pointC.Y, null);
                    cmd.Entry[9] = new SingleValue(pointC.Z, null);
                    cmd.Entry[10] = new SingleValue(pointD.X, null);
                    cmd.Entry[11] = new SingleValue(pointD.Y, null);
                    cmd.Entry[12] = new SingleValue(pointD.Z, null);
                    cmd.Entry[13] = new SingleValue(range, null);
                    cmd.Entry[14] = new BooleanValue(unk);
                    cmd.Entry[15] = new SingleValue(maxThrottle, null);

                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            // end border/frame/whatever
            ImGui.PopID();
            return false;
        }

        void RenderPointOrObject(ref int radioIndex, Entry entry)
        {
            ImGui.RadioButton("Point", ref radioIndex, 0);
            ImGui.RadioButton("Object", ref radioIndex, 1);

            if (radioIndex == 0)
            {
                var vec = new Vector3(entry[0].ToSingle(), entry[1].ToSingle(), entry[2].ToSingle());
                ImGui.InputFloat3("Position", ref vec);
                entry[0] = new SingleValue(vec.X, null);
                entry[1] = new SingleValue(vec.Y, null);
                entry[2] = new SingleValue(vec.Z, null);
            }
            else
            {
                var val = entry[0].ToString();
                Controls.InputTextId("Target", ref val);
                entry[0] = new StringValue(val);
            }
        }
    }
}
