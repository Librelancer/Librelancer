using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LancerEdit.GameContent.MissionEditor.NodeTypes;
using LibreLancer.Data.Schema.Missions;
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
    int selectedNpcClassIndex = -1;
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Navigate To");
        ImGui.SameLine(100f);
        jumpLookup?.Draw();

        Controls.InputTextIdUndo("Node Filter", undoBuffer, () => ref NodeFilter, labelWidth: 100f);
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));
        if (ImGui.CollapsingHeader("Mission Information", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(1);
            RenderMissionInformation();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader("NPC Arch Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(2);
            RenderNpcArchManager();
            ImGui.PopID();
        }

        if (ImGui.CollapsingHeader("NPC Management", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.PushID(3);
            RenderNpcManagement();
            ImGui.PopID();
        }

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
        if (selectedNpcIndex >= missionIni.NPCs.Count)
            selectedNpcIndex = -1;

        var selectedNpc = selectedNpcIndex != -1 ? missionIni.NPCs[selectedNpcIndex] : null;


        ImGui.Spacing();


        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##NPCs", selectedNpc is not null ? selectedNpc.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.NPCs.Count; index++)
            {
                var arch = missionIni.NPCs[index];
                var selected = arch == selectedNpc;
                var id = string.IsNullOrEmpty(arch?.Nickname) ? $"Untitled_{index}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                {
                    continue;
                }

                selectedNpcIndex = index;
                selectedNpc = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedNpcIndex = missionIni.NPCs.Count;
            undoBuffer.Commit(new ListAdd<MissionNPC>("NPC", missionIni.NPCs, new()));
        }
        ImGui.SetItemTooltip("Create New NPC");
        ImGui.SameLine();
        ImGui.BeginDisabled(selectedNpcIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            win.Confirm("Are you sure you want to delete this NPC?", () =>
            {
                undoBuffer.Commit(new ListRemove<MissionNPC>("NPC", missionIni.NPCs,
                    selectedNpcIndex,
                    missionIni.NPCs[selectedNpcIndex]));
                selectedNpcIndex--;
            });
        }
        ImGui.SetItemTooltip("Delete NPC");
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();


        if (selectedNpc is null)
        {
            return;
        }

        ImGui.PushID(selectedNpcIndex);

        Controls.InputTextIdUndo("Nickname", undoBuffer, () => ref selectedNpc.Nickname, 0f, 100f);
        Controls.InputTextIdUndo("Archetype", undoBuffer, () => ref selectedNpc.NpcShipArch, 0f, 100f);
        if (missionIni.ShipIni != null)
        {
            MissionEditorHelpers.AlertIfInvalidRef(() =>
                missionIni.ShipIni.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch)
                || gameData.GameData.Items.Ini.NPCShips.ShipArches.Any(x => x.Nickname == selectedNpc.NpcShipArch));
        }
        Controls.IdsInputStringUndo("Name", gameData, popup, undoBuffer,
            () => ref selectedNpc.IndividualName,
            false, 75f, 100f, -1f);
        Controls.InputTextIdUndo("Affiliation", undoBuffer, () => ref selectedNpc.Affiliation, 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Factions.Any(x => x.Nickname == selectedNpc.Affiliation));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushFont(ImGuiHelper.Roboto, 16);
        ImGuiExt.CenterText("Costume Settings");
        ImGui.PopFont();
        ImGui.Spacing();
        Controls.InputTextIdUndo("Head", undoBuffer, () => ref selectedNpc.SpaceCostume[0], 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[0]) is not null);
        Controls.InputTextIdUndo("Body", undoBuffer, () => ref selectedNpc.SpaceCostume[1], 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Ini.Bodyparts.FindBodypart(selectedNpc.SpaceCostume[1]) is not null);
        Controls.InputTextIdUndo("Accessory", undoBuffer, () => ref selectedNpc.SpaceCostume[2], 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Ini.Bodyparts.FindAccessory(selectedNpc.SpaceCostume[2]) is not null);

        ImGui.PopID();
        ImGui.NewLine();
    }

    private void RenderNpcArchManager()
    {
        ImGui.Spacing();
        ImGui.Spacing();

        if (missionIni.ShipIni == null)
        {
            ImGui.BulletText("NPCShipIni not defined");
            return;
        }

        if (selectedArchIndex >= missionIni.ShipIni.ShipArches.Count)
            selectedArchIndex = -1;

        var selectedArch = selectedArchIndex != -1 ? missionIni.ShipIni.ShipArches[selectedArchIndex] : null;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X- ImGui.GetFrameHeightWithSpacing() * 3);
        if (ImGui.BeginCombo("##ShipArchs", selectedArch is not null ? selectedArch.Nickname : "(none)"))
        {
            for (var index = 0; index < missionIni.ShipIni.ShipArches.Count; index++)
            {
                var arch = missionIni.ShipIni.ShipArches[index];
                var selected = arch == selectedArch;
                var id = string.IsNullOrEmpty(arch?.Nickname) ? $"Untitled_{index}" : arch?.Nickname;

                if (!ImGui.Selectable(id, selected))
                {
                    continue;
                }

                selectedArchIndex = index;
                selectedArch = arch;
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            selectedArchIndex = missionIni.ShipIni.ShipArches.Count;
            undoBuffer.Commit(new ListAdd<NPCShipArch>("Ship Arch", missionIni.ShipIni.ShipArches, new()));
        }
        ImGui.SetItemTooltip("Create New Ship Arch");

        ImGui.SameLine();
        ImGui.BeginDisabled(selectedArchIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
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
        ImGui.SetItemTooltip("Delete Ship Arch");
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (selectedArch is null)
        {
            return;
        }

        ImGui.PushID(selectedArchIndex);

        Controls.InputTextIdUndo("Arch Nickname", undoBuffer, () => ref selectedArch.Nickname, 0f, 100f);
        Controls.InputTextIdUndo("Loadout", undoBuffer, () => ref selectedArch.Loadout, 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() =>
            gameData.GameData.Items.Loadouts.Any(x => x.Nickname == selectedArch.Loadout));

        Controls.InputIntUndo("Level", undoBuffer, () => ref selectedArch.Level, 1, 10, labelWidth: 100f);

        Controls.InputTextIdUndo("Pilot", undoBuffer, () => ref selectedArch.Pilot, 0f, 100f);
        MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Items.GetPilot(selectedArch.Pilot) is not null);

        string[] stateGraphs =
            { "NOTHING", "FIGHTER", "FREIGHTER", "GUNBOAT", "CRUISER", "TRANSPORT", "CAPITAL", "MINING" };
        int currentStateGraphIndex = Array.FindIndex(stateGraphs,
            x => selectedArch.StateGraph?.Equals(x, StringComparison.InvariantCultureIgnoreCase) ?? false);
        if (currentStateGraphIndex == -1)
        {
            currentStateGraphIndex = 0;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("State Graph"); ImGui.SameLine(100f);
        ImGui.SetNextItemWidth(-1);
        ImGui.Combo("##StateGraph", ref currentStateGraphIndex, stateGraphs, stateGraphs.Length);
        if (selectedArch.StateGraph != stateGraphs[currentStateGraphIndex])
        {
            undoBuffer.Set("##StateGraph", () => ref selectedArch.StateGraph, stateGraphs[currentStateGraphIndex]);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("NPC Classes"); ImGui.SameLine(100f);

        if (ImGui.Button(Icons.PlusCircle.ToString()))
        {
            int insertIndex = selectedArch.NpcClass.Count;

            undoBuffer.Commit(new ListAdd<string>(
                "NPC Class",
                selectedArch.NpcClass,
                String.Empty));

            selectedNpcClassIndex = insertIndex;
        }
        ImGui.SetItemTooltip("Create New NPC Class");

        ImGui.SameLine();
        ImGui.BeginDisabled(selectedArchIndex == -1);
        if (ImGui.Button(Icons.TrashAlt.ToString()))
        {
            // delete item
            if (selectedNpcClassIndex >= 0 &&
        selectedNpcClassIndex < selectedArch.NpcClass.Count)
            {
                int removeIndex = selectedNpcClassIndex;
                string removedValue = selectedArch.NpcClass[removeIndex];

                undoBuffer.Commit(new ListRemove<string>(
                    "NPC Class",
                    selectedArch.NpcClass,
                    removeIndex,
                    removedValue));

                // Clamp selection after delete
                selectedNpcClassIndex = Math.Min(
                    removeIndex,
                    selectedArch.NpcClass.Count - 1);
            }
        }
        ImGui.SetItemTooltip("Delete NPC Class");
        ImGui.EndDisabled();
        ImGui.Spacing();
        if (selectedArch.NpcClass != null && selectedArch.NpcClass.Count > 0)
        if (ImGui.BeginTable("##NpcClasses", 2,
            ImGuiTableFlags.BordersInnerV |
            ImGuiTableFlags.SizingFixedFit |
            ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Class Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (int i = 0; i < selectedArch.NpcClass.Count; i++)
            {
                ImGui.TableNextRow();

                // ---- Column 0: index
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text((i+1).ToString());

                // ---- Column 1: input
                ImGui.TableSetColumnIndex(1);
                ImGui.PushItemWidth(-1);

                var npcClass = selectedArch.NpcClass[i];

                ImGuiExt.InputTextLogged(
                    $"##npc-class{i}",
                    ref npcClass,
                    250,
                    (old, upd) => new ListSet<string>(
                        "Npc Class",
                        selectedArch.NpcClass,
                        i,
                        old,
                        upd),
                    true);

                selectedArch.NpcClass[i] = npcClass;
                ImGui.PopItemWidth();

                // ---- Row selection logic
                if (ImGui.IsItemClicked())
                    selectedNpcClassIndex = i;

                // ---- Selection background (FULL ROW, correct height)
                if (selectedNpcClassIndex == i)
                {
                    ImGui.TableSetBgColor(
                        ImGuiTableBgTarget.RowBg0,
                        ImGui.GetColorU32(ImGuiCol.Header));
                }
            }

            ImGui.EndTable();
        }
        ImGui.PopID();

        if (selectedNpcClassIndex >= selectedArch.NpcClass.Count)
            selectedNpcClassIndex = selectedArch.NpcClass.Count - 1;

        if (selectedNpcClassIndex < 0 && selectedArch.NpcClass.Count > 0)
            selectedNpcClassIndex = 0;

        ImGui.NewLine();
    }

    private int selectedArchIndex = -1;

    private void RenderMissionInformation()
    {
        ImGui.Spacing();
        var info = missionIni.Info;
        Controls.IdsInputStringUndo("Title IDS", gameData, popup, undoBuffer, () => ref info.MissionTitle, false, 75f,100f,-1f);
        Controls.IdsInputStringUndo("Offer IDS", gameData, popup, undoBuffer, () => ref info.MissionOffer, false, 75f, 100f, -1f);

        Controls.InputIntUndo("Reward", undoBuffer, () => ref info.Reward,labelWidth:100f);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("NPC Ship File"); ImGui.SameLine(100f);

        ImGui.PushItemWidth(-1f);
        ImGui.InputText("##NPCShipFile", ref info.NpcShipFile, 255, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.ElideLeft);
        ImGui.PopItemWidth();

        ImGui.Dummy(new Vector2(100f, 1f)); ImGui.SameLine(100f);
        if (ImGui.Button("Change Ship File", new Vector2(-1f,0f))) 
        {
            popup.OpenPopup(new VfsFileSelector("Change Ship File",
                gameData.GameData.VFS,
                gameData.GameData.Items.Ini.Freelancer.DataPath, x =>
                {
                    undoBuffer.Commit(new SetNpcShipFileAction(
                        missionIni.Info.NpcShipFile,
                        gameData.GameData.Items.Ini.Freelancer.DataPath + x,
                        this));
                }, VfsFileSelector.MakeFilter(".ini")));
        }

        ImGui.NewLine();
    }
}
