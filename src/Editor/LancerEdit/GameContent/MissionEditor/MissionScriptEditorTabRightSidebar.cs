using System.Linq;
using ImGuiNET;
using LibreLancer.Data.Missions;
using LibreLancer.ImUI;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private void RenderRightSidebar()
    {
        ImGuiExt.SeparatorText("Ship Manager");
        RenderMissionShipManager();
    }

    private int selectedShipIndex = -1;
    private void RenderMissionShipManager()
    {
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New Ship"))
        {
            selectedShipIndex = ini.NPCs.Count;
            ini.NPCs.Add(new MissionNPC());
        }

        ImGui.BeginDisabled(selectedShipIndex == -1);
        if (ImGui.Button("Delete Ship"))
        {
            win.Confirm("Are you sure you want to delete this ship?", () =>
            {
                ini.NPCs.RemoveAt(selectedShipIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedShip = selectedShipIndex != -1 ? ini.Ships[selectedShipIndex] : null;
        if (ImGui.BeginCombo("Ships", selectedShip is not null ? selectedShip.Nickname : ""))
        {
            for (var index = 0; index < ini.Ships.Count; index++)
            {
                var arch = ini.Ships[index];
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

        Controls.InputTextId("Ship Nickname##ID", ref selectedShip.Nickname);
        Controls.InputTextId("System##ID", ref selectedShip.System);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
            gameData.GameData.Systems.Any(x => x.Nickname == selectedShip.System));

        if (ImGui.BeginCombo("NPC##ID", selectedShip.NPC ?? ""))
        {
            foreach (var npc in ini.NPCs
                         .Select(x => x.Nickname)
                         .Where(x => ImGui.Selectable(x ?? "", selectedShip.NPC == x)))
            {
                selectedShip.NPC = npc;
            }

            ImGui.EndCombo();
        }

        MissionEditorHelpers.AlertIfInvalidRef(() => ini.NPCs.Any(x => x.Nickname == selectedShip.NPC));

        ImGui.NewLine();
        ImGui.Text("Labels");

        if (selectedShip.Labels.Count is not 0)
        {
            for (var i = 0; i < selectedShip.Labels.Count; i++)
            {
                var label = selectedShip.Labels[i];
                ImGui.PushID(label);
                Controls.InputTextId("##ID", ref label);
                ImGui.PopID();
                selectedShip.Labels[i] = label;
            }
        }
        MissionEditorHelpers.AddRemoveListButtons(selectedShip.Labels);

        ImGui.InputFloat3("Position##ID", ref selectedShip.Position);

        ImGui.NewLine();
        Controls.InputTextId("Relative Position Obj##ID", ref selectedShip.RelativePosition.ObjectName);
        // Don't think it's possible to validate this one, as it could refer to any solar object in any system

        ImGui.InputFloat("Relative Position Min Range##ID", ref selectedShip.RelativePosition.MinRange);
        ImGui.InputFloat("Relative Position Max Range##ID", ref selectedShip.RelativePosition.MaxRange);

        ImGui.NewLine();
        Controls.InputFlQuaternion("Orientation##ID", ref selectedShip.Orientation);
        ImGui.Checkbox("Random Name##ID", ref selectedShip.RandomName);
        ImGui.Checkbox("Jumper##ID", ref selectedShip.Jumper);
        ImGui.InputFloat("Radius##ID", ref selectedShip.Radius);
        Controls.InputTextId("Arrival Object##ID", ref selectedShip.ArrivalObj);

        if (ImGui.BeginCombo("Initial Objectives##ID", selectedShip.InitObjectives ?? ""))
        {
            if (ImGui.Selectable("no_op", selectedShip.InitObjectives == "no_op"))
            {
                selectedShip.InitObjectives = "no_op";
            }

            foreach (var npc in ini.ObjLists
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
                                                     ini.ObjLists.Any(x => x.Nickname == selectedShip.InitObjectives));

        ImGui.Text("Cargo");
        if (selectedShip.Cargo.Count is not 0)
        {
            for (var i = 0; i < selectedShip.Cargo.Count; i++)
            {
                var cargo = selectedShip.Cargo[i];
                ImGui.PushID(i);
                Controls.InputTextId("##Cargo", ref cargo.Cargo);
                ImGui.SameLine();
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
}
