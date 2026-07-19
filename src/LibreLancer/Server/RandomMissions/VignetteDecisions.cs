namespace LibreLancer.Server.RandomMissions;

public struct VignetteDecisions
{
    // This is the outer branch used by vignetteparams. It contains both
    // AssassinateShip and the Pk_all_ships destroy variant.
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

    public VignetteDecisions With(string decision, bool value)
    {
        var n = this;
        n.SetDecision(decision, value);
        return n;
    }


    void SetDecision(string decision, bool value)
    {
        switch (decision.ToLowerInvariant())
        {
            case "assassinate_mission":
                AssassinateMission = value;
                break;
            case "assassinate_ship":
                AssassinateShip = value;
                break;
            case "assassinate_solar":
                AssassinateSolar = value;
                break;
            case "big_solar":
                BigSolar = value;
                break;
            case "bring_back_loot":
                BringBackLoot = value;
                break;
            case "continuous_reenforcements":
                ContinuousReinforcements = value;
                break;
            case "defensive_solars":
                DefensiveSolars = value;
                break;
            case "defensive_solars_at_main_battle":
                DefensiveSolarsAtMainBattle = value;
                break;
            case "destroy_solars_mission":
                DestroySolarsMission = value;
                break;
            case "friendly_ships":
                FriendlyShips = value;
                break;
            case "friendly_ships_after_30_s":
                FriendlyShipsAfter30S = value;
                break;
            case "friendly_ships_at_installation":
                FriendlyShipsAtInstallation = value;
                break;
            case "friendly_ships_at_main_battle":
                FriendlyShipsAtMainBattle = value;
                break;
            case "friendly_ships_come_in_to_whoop_up":
                FriendlyShipsComeInToWhoopUp = value;
                break;
            case "hostile_ships":
                HostileShips = value;
                break;
            case "hostile_ships_after_30_s":
                HostileShipsAfter30S = value;
                break;
            case "hostile_waves":
                HostileWaves = value;
                break;
            case "main_battle_non_target_wave":
                MainBattleNonTargetWave = value;
                break;
            case "main_battle_wave_1":
                MainBattleWave1 = value;
                break;
            case "main_battle_wave_2":
                MainBattleWave2 = value;
                break;
            case "pk_defensive_solars":
                PkDefensiveSolars = value;
                break;
            case "pk_hostile_ships":
                PkHostileShips = value;
                break;
            case "pre_battle":
                PreBattle = value;
                break;
            case "pre_battle_non_target_wave":
                PreBattleNonTargetWave = value;
                break;
            case "pre_battle_wave_2":
                PreBattleWave2 = value;
                break;
            case "pre_battle_wave_runs":
                PreBattleWaveRuns = value;
                break;
            case "target_drops_critical_loot":
                TargetDropsCriticalLoot = value;
                break;
            case "target_spawns_at_pre_battle":
                TargetSpawnsAtPreBattle = value;
                break;
            case "target_spawns_with_wave":
                TargetSpawnsWithWave = value;
                break;
            case "tractor_in_loot":
                TractorInLoot = value;
                break;
            case "wave_just_after_main_battle_starts":
                WaveJustAfterMainBattleStarts = value;
                break;
            case "branch":
                //no-op
                break;
            default:
                FLLog.Warning("vignetteparams.ini", $"Unknown decision {decision}");
                break;
        }
    }
}
