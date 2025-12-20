using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using LibreLancer.ImUI;
using LancerEdit.GameContent.Lookups;
using StarSystem = LibreLancer.Data.GameData.World.StarSystem;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.Schema.Universe;

namespace LancerEdit.GameContent.Popups;

public class PatrolRouteDialog : PopupWindow
{
    public override string Title { get; set; } = "Patrol Route Configuration";
    public override Vector2 InitSize => new Vector2(400, 500);

    // Patrol route parameters
    private string pathLabel = "";
    private int toughness = 3;
    private int density = 15;
    private int repopTime = 5;
    private int maxBattleSize = 99;
    private int reliefTime = 30;
    private float sort = 99f;

    // Callback when user confirms
    private Action<PatrolRouteConfig> onConfirm;
    private Action onCancel;
    private List<Vector3> routePoints;
    private List<PatrolEncounter> encounters = new();
    private FactionLookup factionLookup;
    private EncounterLookup encounterLookup;
    private GameDataContext gameData;
    private StarSystem system;
    private bool confirmed = false;

    public PatrolRouteDialog(List<Vector3> points, Action<PatrolRouteConfig> onConfirm, Action onCancel, GameDataContext gameData, StarSystem system)
    {
        this.routePoints = points;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
        this.gameData = gameData;
        this.system = system;

        // Generate default path label using the current system's nickname
        pathLabel = $"{system.Nickname.ToLowerInvariant()}_patrol";

        // Initialize faction lookup with first available faction
        var defaultFaction = gameData.GameData.Items.Factions.FirstOrDefault();
        factionLookup = new FactionLookup("##faction", gameData, defaultFaction);

        // Add a default encounter
        var defaultEncounter = new PatrolEncounter();
        defaultEncounter.EncounterLookup = new EncounterLookup("##encounter_0", gameData, null);
        defaultEncounter.SetDefaultArchetype();
        var defaultFactionInfo = defaultFaction;
        var defaultPatrolFaction = new PatrolFaction
        {
            Faction = defaultFactionInfo,
            FactionLookup = new FactionLookup("##faction_0_0", gameData, defaultFactionInfo)
        };
        defaultEncounter.Factions.Add(defaultPatrolFaction);
        encounters.Add(defaultEncounter);
    }

    public override void Draw(bool appearing)
    {
        ImGui.Text("Configure patrol route parameters:");
        ImGui.Separator();

        if (ImGui.BeginTable("patrolConfig", 2, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("one", ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelper.Scale);
            ImGui.TableSetupColumn("two", ImGuiTableColumnFlags.WidthStretch);

            // Path Label
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Base Path Name");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputText("##pathLabel", ref pathLabel, 128);
            ImGui.PopItemWidth();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("This is the base name for the patrol route. It must be unique within the entire universe or Freelancer could confuse it with others.");
                ImGui.EndTooltip();
            }

            // Zone Properties
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.SeparatorText("Zone Properties");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Toughness");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputInt("##toughness", ref toughness, 1);
            ImGui.PopItemWidth();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Density");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputInt("##density", ref density, 1);
            ImGui.PopItemWidth();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Repop Time");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputInt("##repopTime", ref repopTime, 1);
            ImGui.PopItemWidth();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Max Battle Size");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputInt("##maxBattleSize", ref maxBattleSize, 1);
            ImGui.PopItemWidth();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Relief Time");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputInt("##reliefTime", ref reliefTime, 1);
            ImGui.PopItemWidth();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Sort");
            ImGui.TableNextColumn();
            ImGui.PushItemWidth(-1);
            ImGui.InputFloat("##sort", ref sort, 0, 0, "%.3f");
            ImGui.PopItemWidth();

            ImGui.EndTable();
        }

        ImGui.Separator();
        ImGui.Text($"Route Points: {routePoints.Count}");
        ImGui.Text($"Will create {routePoints.Count - 1} patrol zones");

        ImGui.Separator();

        if (ImGui.Button("Add Encounter"))
        {
            var newEncounter = new PatrolEncounter();
            newEncounter.EncounterLookup = new EncounterLookup($"##encounter_{encounters.Count}", gameData, null);
            newEncounter.SetDefaultArchetype();
            var newFaction = new PatrolFaction();
            newFaction.Faction = gameData.GameData.Items.Factions.FirstOrDefault();
            newFaction.FactionLookup = new FactionLookup($"##faction_{encounters.Count}_0", gameData, newFaction.Faction);
            newEncounter.Factions.Add(newFaction);
            encounters.Add(newEncounter);
        }

        int encounterToDelete = -1;
        for (int i = 0; i < encounters.Count; i++)
        {
            var encounter = encounters[i];
            ImGui.PushID(i);
            if (ImGuiExt.Button(Icons.TrashAlt, encounters.Count > 1))
                encounterToDelete = i;
            ImGui.SameLine();
            if (ImGui.CollapsingHeader($"Encounter #{i + 1}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable($"encounter_table_{i}", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("one", ImGuiTableColumnFlags.WidthFixed, 120 * ImGuiHelper.Scale);
                    ImGui.TableSetupColumn("two", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Archetype");
                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    encounter.EncounterLookup.Draw();
                    encounter.Archetype = encounter.EncounterLookup.Selected;
                    ImGui.PopItemWidth();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Difficulty");
                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    var difficulty = encounter.Difficulty;
                    if(ImGui.InputInt("##difficulty", ref difficulty, 1))
                        encounter.Difficulty = difficulty;
                    ImGui.PopItemWidth();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Chance");
                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    var chance = encounter.Chance;
                    if(ImGui.InputInt("##chance", ref chance, 1))
                        encounter.Chance = chance;
                    ImGui.PopItemWidth();

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.SeparatorText("Factions");

                    int factionToDelete = -1;
                    for (int j = 0; j < encounter.Factions.Count; j++)
                    {
                        var faction = encounter.Factions[j];
                        ImGui.PushID(j);
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if (ImGuiExt.Button(Icons.TrashAlt,encounter.Factions.Count > 1))
                            factionToDelete = j;
                        ImGui.SameLine();
                        ImGui.Text($"Faction #{j + 1}");
                        ImGui.TableNextColumn();
                        faction.FactionLookup.Draw();
                        faction.Faction = faction.FactionLookup.Selected;
                        ImGui.SameLine();
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
                        var factionChance = faction.Chance;
                        if(ImGui.InputInt("##faction_chance", ref factionChance, 1))
                            faction.Chance = factionChance;
                        ImGui.PopItemWidth();
                        ImGui.PopID();
                    }

                    if (factionToDelete != -1)
                        encounter.Factions.RemoveAt(factionToDelete);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    if (ImGui.Button("Add Faction"))
                    {
                        var newFaction = new PatrolFaction();
                        newFaction.Faction = gameData.GameData.Items.Factions.FirstOrDefault();
                        newFaction.FactionLookup = new FactionLookup($"##faction_{i}_{encounter.Factions.Count}", gameData, newFaction.Faction);
                        encounter.Factions.Add(newFaction);
                    }
                    ImGui.EndTable();
                }
            }
            ImGui.PopID();
        }

        if (encounterToDelete != -1)
            encounters.RemoveAt(encounterToDelete);

        ImGui.Separator();

        // Buttons
        if (ImGui.Button("Create Patrol Route"))
        {
            var config = new PatrolRouteConfig
            {
                PathLabel = pathLabel,
                Toughness = toughness,
                Density = density,
                RepopTime = repopTime,
                MaxBattleSize = maxBattleSize,
                ReliefTime = reliefTime,
                Sort = sort
            };

            // Collect encounters
            config.Encounters.AddRange(encounters);

            confirmed = true;
            onConfirm(config);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            confirmed = true;
            onCancel();
            ImGui.CloseCurrentPopup();
        }
    }

    public override void OnClosed()
    {
        if (!confirmed)
            onCancel();
    }
}

public class PatrolEncounter
{
    public string Archetype { get; set; }
    public int Difficulty { get; set; } = 2;
    public int Chance { get; set; } = 1;
    public List<PatrolFaction> Factions { get; set; } = new();
    public EncounterLookup EncounterLookup { get; set; }

    public void SetDefaultArchetype()
    {
        Archetype = EncounterLookup?.Archetypes?.FirstOrDefault();
    }
}

public class PatrolFaction
{
    public Faction Faction { get; set; }
    public int Chance { get; set; } = 1;
    public FactionLookup FactionLookup { get; set; }
}

public class PatrolRouteConfig
{
    public string PathLabel { get; set; }
    public int Toughness { get; set; }
    public int Density { get; set; }
    public int RepopTime { get; set; }
    public int MaxBattleSize { get; set; }
    public int ReliefTime { get; set; }
    public float Sort { get; set; }
    public List<PatrolEncounter> Encounters { get; set; } = new();
}
