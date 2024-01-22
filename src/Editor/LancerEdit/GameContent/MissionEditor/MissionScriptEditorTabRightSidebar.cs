using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using LibreLancer.Data.Missions;

namespace LancerEdit.GameContent.MissionEditor;

public sealed partial class MissionScriptEditorTab
{
    private void RenderRightSidebar()
    {
        ImGui.BeginChild("NavbarRight", new Vector2(300f, ImGui.GetContentRegionMax().Y), ImGuiChildFlags.None, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);

        ImGui.PushStyleColor(ImGuiCol.Header, ImGui.GetColorU32(ImGuiCol.FrameBg));

        ImGui.NewLine();
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

        ImGui.PopStyleColor();

        ImGui.EndChild();
    }

    private int selectedLootIndex = -1;
    private void RenderLootManager()
    {
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New Loot"))
        {
            selectedLootIndex = ini.Loots.Count;
            ini.Loots.Add(new MissionLoot());
        }

        ImGui.BeginDisabled(selectedLootIndex == -1);
        if (ImGui.Button("Delete Loot"))
        {
            win.Confirm("Are you sure you want to delete this loot?", () =>
            {
                ini.Loots.RemoveAt(selectedLootIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedLoot = selectedLootIndex != -1 ? ini.Loots[selectedLootIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("Loots", selectedLoot is not null ? selectedLoot.Nickname : ""))
        {
            for (var index = 0; index < ini.Loots.Count; index++)
            {
                var arch = ini.Loots[index];
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
        Controls.IdsInputString("Name##Loot", gameData, popup, ref selectedLoot.StringId, x => selectedLoot.StringId = x, inputWidth: 150f);

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
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New Solar"))
        {
            selectedSolarIndex = ini.Solars.Count;
            ini.Solars.Add(new MissionSolar());
        }

        ImGui.BeginDisabled(selectedSolarIndex == -1);
        if (ImGui.Button("Delete Solar"))
        {
            win.Confirm("Are you sure you want to delete this solar?", () =>
            {
                ini.Solars.RemoveAt(selectedSolarIndex--);
            });
        }

        ImGui.EndDisabled();

        var selectedSolar = selectedSolarIndex != -1 ? ini.Solars[selectedSolarIndex] : null;
        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("Solars", selectedSolar is not null ? selectedSolar.Nickname : ""))
        {
            for (var index = 0; index < ini.Solars.Count; index++)
            {
                var arch = ini.Solars[index];
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
            gameData.GameData.Systems.Any(x => x.Nickname.Equals(selectedSolar.System, StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Faction##Solar", ref selectedSolar.Faction, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Faction.Length is 0 ||
            gameData.GameData.Factions.Any(x => x.Nickname.Equals(selectedSolar.Faction, StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Archetype##Solar", ref selectedSolar.Archetype, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Archetype.Length is 0 ||
            gameData.GameData.Archetypes.Any(x => x.Nickname.Equals(selectedSolar.Archetype, StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Base##Solar", ref selectedSolar.Base, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Base.Length is 0 ||
            gameData.GameData.Bases.Any(x => x.Nickname.Equals(selectedSolar.Base, StringComparison.InvariantCultureIgnoreCase)));

        Controls.InputTextId("Loadout##Solar", ref selectedSolar.Loadout, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedSolar.Loadout.Length is 0 ||
            gameData.GameData.Loadouts.Any(x => x.Nickname.Equals(selectedSolar.Loadout, StringComparison.InvariantCultureIgnoreCase)));

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
        var ini = missionScript.Ini;

        if (ImGui.Button("Create New Ship"))
        {
            selectedShipIndex = ini.Ships.Count;
            ini.Ships.Add(new MissionShip());
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
        ImGui.SetNextItemWidth(150f);
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

        Controls.InputTextId("Nickname##Ship", ref selectedShip.Nickname, 150f);
        Controls.InputTextId("System##Ship", ref selectedShip.System, 150f);
        MissionEditorHelpers.AlertIfInvalidRef(() => selectedShip.System.Length is 0 ||
            gameData.GameData.Systems.Any(x => x.Nickname == selectedShip.System));

        ImGui.SetNextItemWidth(150f);
        if (ImGui.BeginCombo("NPC##Ship", selectedShip.NPC ?? ""))
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
                Controls.InputTextId("##Cargo", ref cargo.Cargo, 150f);
                MissionEditorHelpers.AlertIfInvalidRef(() => gameData.GameData.Equipment.Any(x => x.Nickname.Equals(cargo.Cargo, StringComparison.InvariantCultureIgnoreCase)));
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
}
