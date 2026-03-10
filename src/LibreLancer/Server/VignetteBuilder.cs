using System;
using System.Collections.Generic;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Server;

public class VignetteParameters
{
    public int Seed = 4869;
    public string OfferGroup = "";

    public bool AssassinateMission;
    public bool AssassinateShip;
    public bool AssassinateSolar;
    public bool BigSolar;
    public bool BringBackLoot;
    public bool ContinuousReinforcements;
    public bool DefensiveSolars;
    public bool DefensiveSolarsAtMainBattle;
    public bool DestroySolarsMission;
    public bool FriendlyShips;
    public bool FriendlyShipsAfter30S;
    public bool FriendlyShipsAtInstallation;
    public bool FriendlyShipsAtMainBattle;
    public bool FriendlyShipsComeInToWhoopUp;
    public bool HostileShips;
    public bool HostileShipsAfter30S;
    public bool HostileWaves;
    public bool MainBattleNonTargetWave;
    public bool MainBattleWave1;
    public bool MainBattleWave2;
    public bool PkDefensiveSolars;
    public bool PkHostileShips;
    public bool PreBattle;
    public bool PreBattleNonTargetWave;
    public bool PreBattleWave2;
    public bool PreBattleWaveRuns;
    public bool TargetDropsCriticalLoot;
    public bool TargetSpawnsAtPreBattle;
    public bool TargetSpawnsWithWave;
    public bool TractorInLoot;
    public bool WaveJustAfterMainBattleStarts;
}

public class VignetteInfo
{
    public bool IsError;
    public List<string> Documentation = [];
    public Dictionary<string, VignetteString> ObjectiveStrings = new();
    public Dictionary<string, CommSequence> CommSequences = new();
    public VignetteString RewardText;
    public VignetteString FailureText;
    public List<OfferTextItem> OfferText = [];
}

public static class VignetteBuilder
{
    private static void Error(VignetteAst ast, string error)
    {
        FLLog.Error("VignetteParams", $"{error} at id={ast.Id}");
    }

    public static VignetteInfo Run(VignetteTree tree, VignetteParameters parameters)
    {
        Dictionary<string, bool> conditions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Assassinate_mission"] = parameters.AssassinateMission,
            ["Assassinate_Ship"] = parameters.AssassinateShip,
            ["Assassinate_solar"] = parameters.AssassinateSolar,
            ["Big_solar"] = parameters.BigSolar,
            ["Bring_back_loot"] = parameters.BringBackLoot,
            // sic.
            // ReSharper disable once StringLiteralTypo
            ["Continuous_reenforcements"] = parameters.ContinuousReinforcements,
            ["Defensive_solars"] = parameters.DefensiveSolars,
            ["Defensive_solars_at_main_battle"] = parameters.DefensiveSolarsAtMainBattle,
            ["Destroy_solars_mission"] = parameters.DestroySolarsMission,
            ["Friendly_ships"] = parameters.FriendlyShips,
            ["Friendly_ships_after_30_s"] = parameters.FriendlyShipsAfter30S,
            ["Friendly_ships_at_installation"] = parameters.FriendlyShipsAtInstallation,
            ["Friendly_ships_at_main_battle"] = parameters.FriendlyShipsAtMainBattle,
            ["Friendly_ships_come_in_to_whoop_up"] = parameters.FriendlyShipsComeInToWhoopUp,
            ["Hostile_ships"] = parameters.HostileShips,
            ["Hostile_ships_after_30_s"] = parameters.HostileShipsAfter30S,
            ["Hostile_waves"] = parameters.HostileWaves,
            ["Main_battle_non_target_wave"] = parameters.MainBattleNonTargetWave,
            ["Main_battle_wave_1"] = parameters.MainBattleWave1,
            ["Main_battle_wave_2"] = parameters.MainBattleWave2,
            ["Pk_defensive_solars"] = parameters.PkDefensiveSolars,
            ["Pk_hostile_ships"] = parameters.PkHostileShips,
            ["Pre_battle"] = parameters.PreBattle,
            ["Pre_battle_non_target_wave"] = parameters.PreBattleNonTargetWave,
            ["Pre_battle_wave_2"] = parameters.PreBattleWave2,
            ["Pre_battle_wave_runs"] = parameters.PreBattleWaveRuns,
            ["target_drops_critical_loot"] = parameters.TargetDropsCriticalLoot,
            ["Target_spawns_at_pre_battle"] = parameters.TargetSpawnsAtPreBattle,
            ["Target_spawns_with_wave"] = parameters.TargetSpawnsWithWave,
            ["Tractor_in_loot"] = parameters.TractorInLoot,
            ["Wave_just_after_main_battle_starts"] = parameters.WaveJustAfterMainBattleStarts
        };

        VignetteAst? currentNode = tree.StartNode;
        var vinfo = new VignetteInfo();
        var r = new Random(parameters.Seed);
        while (currentNode != null)
        {
            FLLog.Debug("VignetteParams", "Processing: " + currentNode.ToString());
            switch (currentNode)
            {
                case AstDoc doc:
                    vinfo.Documentation.Add(doc.Docs.Documentation!);
                    break;
                case AstData data:
                    if (!data.Data.Implemented)
                    {
                        Error(currentNode, "Unimplemented");
                        vinfo.IsError = true;
                        return vinfo;
                    }

                    if (data.Data.RewardText.Target != null)
                    {
                        vinfo.RewardText = data.Data.RewardText;
                    }

                    if (data.Data.FailureText.Target != null)
                    {
                        vinfo.FailureText = data.Data.FailureText;
                    }

                    if (data.Data.OfferTexts is { Count: > 0 })
                    {
                        var ot = data.Data.OfferTexts.Count == 1
                            ? data.Data.OfferTexts[0]
                            : data.Data.OfferTexts[r.Next(0, data.Data.OfferTexts.Count)];
                        if (ot.Op == OfferTextOp.replace)
                            vinfo.OfferText = [];
                        vinfo.OfferText.AddRange(ot.Items);
                    }

                    foreach (var str in data.Data.ObjectiveTexts)
                    {
                        vinfo.ObjectiveStrings[str.Target!] = str;
                    }

                    foreach (var comm in data.Data.CommSequences)
                    {
                        vinfo.CommSequences[comm.Event] = comm;
                    }

                    break;
                case AstDecision dec:
                    if (dec.Decision.Nickname.Equals("branch", StringComparison.OrdinalIgnoreCase))
                    {
                        if (dec.Children[0] is not AstData d1 ||
                            d1.Data.OfferGroup?.Length <= 0)
                        {
                            Error(currentNode, "Invalid branch node");
                            vinfo.IsError = true;
                            return vinfo;
                        }

                        if (d1.Data.OfferGroup.Contains(parameters.OfferGroup, StringComparer.OrdinalIgnoreCase))
                        {
                            currentNode = d1;
                        }
                        else if (dec.Children[1] is AstData d2 &&
                                 d2.Data.OfferGroup?.Length > 0)
                        {
                            if (d2.Data.OfferGroup.Contains(parameters.OfferGroup, StringComparer.OrdinalIgnoreCase))
                            {
                                currentNode = d2;
                            }
                            else
                            {
                                Error(currentNode, $"{parameters.OfferGroup} not in either");
                                vinfo.IsError = true;
                                return vinfo;
                            }
                        }
                        else
                        {
                            currentNode = dec.Children[1];
                        }
                    }
                    else if (conditions.TryGetValue(dec.Decision.Nickname, out var condition))
                    {
                        if (condition)
                        {
                            currentNode = currentNode.Children[0];
                        }
                        else
                        {
                            currentNode = currentNode.Children[1];
                        }
                    }
                    else
                    {
                        FLLog.Warning("VignetteParams", $"Unknown decision {dec.Decision.Nickname}, assume false.");
                        currentNode = currentNode.Children[1];
                    }

                    continue;
            }

            currentNode = currentNode.Children.Count > 0
                ? currentNode.Children[0]
                : null;
        }

        return vinfo;
    }
}
