using System;
using System.Collections.Generic;
using LibreLancer.Data.GameData.RandomMissions;
using LibreLancer.Data.Schema.RandomMissions;

namespace LibreLancer.Server;

public class VignetteParameters
{
    public int Seed = 4869;
    public string OfferGroup;

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
    public List<string> Documentation = new();
    public Dictionary<string, VignetteString> ObjectiveStrings = new();
    public Dictionary<string, CommSequence> CommSequences = new();
    public VignetteString RewardText;
    public VignetteString FailureText;
    public List<OfferTextItem> OfferText = new();
}

public static class VignetteBuilder
{
    static void Error(VignetteAst ast, string error)
    {
        FLLog.Error("VignetteParams", $"{error} at id={ast.Id}");
    }

    public static VignetteInfo Run(VignetteTree tree, VignetteParameters parameters)
    {
        Dictionary<string, bool> conditions = new(StringComparer.OrdinalIgnoreCase);
        conditions["Assassinate_mission"] = parameters.AssassinateMission;
        conditions["Assassinate_Ship"] = parameters.AssassinateShip;
        conditions["Assassinate_solar"] = parameters.AssassinateSolar;
        conditions["Big_solar"] = parameters.BigSolar;
        conditions["Bring_back_loot"] = parameters.BringBackLoot;
        // sic.
        // ReSharper disable once StringLiteralTypo
        conditions["Continuous_reenforcements"] = parameters.ContinuousReinforcements;
        conditions["Defensive_solars"] = parameters.DefensiveSolars;
        conditions["Defensive_solars_at_main_battle"] = parameters.DefensiveSolarsAtMainBattle;
        conditions["Destroy_solars_mission"] = parameters.DestroySolarsMission;
        conditions["Friendly_ships"] = parameters.FriendlyShips;
        conditions["Friendly_ships_after_30_s"] = parameters.FriendlyShipsAfter30S;
        conditions["Friendly_ships_at_installation"] = parameters.FriendlyShipsAtInstallation;
        conditions["Friendly_ships_at_main_battle"] = parameters.FriendlyShipsAtMainBattle;
        conditions["Friendly_ships_come_in_to_whoop_up"] = parameters.FriendlyShipsComeInToWhoopUp;
        conditions["Hostile_ships"] = parameters.HostileShips;
        conditions["Hostile_ships_after_30_s"] = parameters.HostileShipsAfter30S;
        conditions["Hostile_waves"] = parameters.HostileWaves;
        conditions["Main_battle_non_target_wave"] = parameters.MainBattleNonTargetWave;
        conditions["Main_battle_wave_1"] = parameters.MainBattleWave1;
        conditions["Main_battle_wave_2"] = parameters.MainBattleWave2;
        conditions["Pk_defensive_solars"] = parameters.PkDefensiveSolars;
        conditions["Pk_hostile_ships"] = parameters.PkHostileShips;
        conditions["Pre_battle"] = parameters.PreBattle;
        conditions["Pre_battle_non_target_wave"] = parameters.PreBattleNonTargetWave;
        conditions["Pre_battle_wave_2"] = parameters.PreBattleWave2;
        conditions["Pre_battle_wave_runs"] = parameters.PreBattleWaveRuns;
        conditions["target_drops_critical_loot"] = parameters.TargetDropsCriticalLoot;
        conditions["Target_spawns_at_pre_battle"] = parameters.TargetSpawnsAtPreBattle;
        conditions["Target_spawns_with_wave"] = parameters.TargetSpawnsWithWave;
        conditions["Tractor_in_loot"] = parameters.TractorInLoot;
        conditions["Wave_just_after_main_battle_starts"] = parameters.WaveJustAfterMainBattleStarts;

        VignetteAst currentNode = tree.StartNode;
        var vinfo = new VignetteInfo();
        var r = new Random(parameters.Seed);
        while (currentNode != null)
        {
            FLLog.Debug("VignetteParams", "Processing: " + currentNode.ToString());
            switch (currentNode)
            {
                case AstDoc doc:
                    vinfo.Documentation.Add(doc.Docs.Documentation);
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
                            vinfo.OfferText = new();
                        vinfo.OfferText.AddRange(ot.Items);
                    }

                    foreach (var str in data.Data.ObjectiveTexts)
                    {
                        vinfo.ObjectiveStrings[str.Target] = str;
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
