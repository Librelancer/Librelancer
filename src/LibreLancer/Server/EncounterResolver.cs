// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Data.Schema.Missions;
using Encounter = LibreLancer.Data.Schema.Universe.Encounter;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server
{
    /// <summary>
    /// Resolved NPC configuration ready for spawning.
    /// Contains validated loadout and pilot references.
    /// </summary>
    public class ResolvedEncounter
    {
        public string Faction { get; init; }
        public ObjectLoadout Loadout { get; init; }
        public Pilot Pilot { get; init; }
        public string StateGraph { get; init; }
        public string ShipArchetype { get; init; }
        public int Difficulty { get; init; }
        public string SourceNpcArch { get; init; }

        public bool IsValid => Loadout != null && Pilot != null;
    }

    /// <summary>
    /// Resolves zone encounter definitions to spawnable NPC configurations.
    /// Maps Encounter archetypes + faction spawns + difficulty to specific
    /// NPCShipArch entries with validated loadouts and pilots.
    /// </summary>
    /// <remarks>
    /// Resolution process:
    /// 1. Get faction from zone encounter's FactionSpawns
    /// 2. Get difficulty from zone encounter (d1-d19)
    /// 3. Find NPCShipArch entries matching faction prefix and difficulty
    /// 4. Filter by ship class (fighter, gunboat, etc.) based on archetype hints
    /// 5. Validate loadout and pilot exist in GameData
    /// 6. Return ready-to-spawn configuration
    /// </remarks>
    public class EncounterResolver
    {
        private readonly GameItemDb gameData;
        private readonly List<NPCShipArch> npcShipArches;
        private readonly Dictionary<string, List<NPCShipArch>> factionCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random random = new();

        // Ship class keywords for filtering NPCShipArch by archetype
        private static readonly Dictionary<string, string[]> ArchetypeToShipClass = new(StringComparer.OrdinalIgnoreCase)
        {
            { "area_scout", new[] { "class_fighter", "fighter" } },
            { "area_defend", new[] { "class_fighter", "fighter" } },
            { "area_assault", new[] { "class_fighter", "fighter" } },
            { "area_patrol", new[] { "class_fighter", "fighter" } },
            { "area_gunboats", new[] { "class_gunboat", "gunboat" } },
            { "area_cruisers", new[] { "class_cruiser", "cruiser" } },
            { "area_battleships", new[] { "class_battleship", "battleship" } },
            { "area_carriers", new[] { "class_carrier", "carrier" } },
            { "area_bombers", new[] { "class_bomber", "bomber" } },
            { "area_freighter", new[] { "class_freighter", "freighter", "trader" } },
            { "area_trade", new[] { "class_freighter", "freighter", "trader" } },
        };

        public EncounterResolver(GameItemDb gameData, IEnumerable<NPCShipArch> shipArches)
        {
            this.gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            this.npcShipArches = shipArches?.ToList() ?? new List<NPCShipArch>();

            BuildFactionCache();

            FLLog.Info("EncounterResolver", $"Initialized with {npcShipArches.Count} NPCShipArch entries, {factionCache.Count} factions cached");
        }

        private void BuildFactionCache()
        {
            foreach (var arch in npcShipArches)
            {
                if (string.IsNullOrEmpty(arch.Nickname)) continue;

                // Extract faction prefix from nickname (e.g., "br_n_br_elite_d1-6" → "br_n")
                var factionPrefix = ExtractFactionPrefix(arch.Nickname);
                if (string.IsNullOrEmpty(factionPrefix)) continue;

                if (!factionCache.TryGetValue(factionPrefix, out var list))
                {
                    list = new List<NPCShipArch>();
                    factionCache[factionPrefix] = list;
                }
                list.Add(arch);
            }
        }

        /// <summary>
        /// Extract faction prefix from NPCShipArch nickname.
        /// Examples: "br_n_br_elite_d1-6" → "br_n", "li_p_li_fighter_d5" → "li_p"
        /// </summary>
        private static string ExtractFactionPrefix(string nickname)
        {
            if (string.IsNullOrEmpty(nickname)) return null;

            // Common faction prefixes in Discovery: xx_n (navy), xx_p (police), fc_n (generic)
            var parts = nickname.Split('_');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}_{parts[1]}";
            }
            return null;
        }

        /// <summary>
        /// Map faction nickname to NPCShipArch prefix.
        /// Example: "br_n_grp" → "br_n"
        /// </summary>
        private static string FactionToPrefix(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;

            // Remove _grp suffix if present
            var clean = faction.Replace("_grp", "").Replace("_GRP", "");

            // Take first two parts
            var parts = clean.Split('_');
            if (parts.Length >= 2)
            {
                return $"{parts[0]}_{parts[1]}";
            }
            return clean;
        }

        /// <summary>
        /// Resolve an encounter to a spawnable NPC configuration.
        /// </summary>
        /// <param name="encounter">Zone encounter definition</param>
        /// <param name="zone">The zone containing the encounter</param>
        /// <returns>Resolved encounter config, or null if resolution failed</returns>
        public ResolvedEncounter Resolve(Encounter encounter, Zone zone)
        {
            if (encounter == null) return null;

            // Get faction from encounter's FactionSpawns (pick one based on chance)
            var faction = SelectFaction(encounter);
            if (string.IsNullOrEmpty(faction))
            {
                FLLog.Debug("EncounterResolver", $"No faction found for encounter {encounter.Archetype}");
                return null;
            }

            // Get difficulty level (default to d5 if not specified)
            var difficulty = encounter.Difficulty > 0 ? encounter.Difficulty : 5;
            var difficultyTag = $"d{difficulty}";

            // Get ship class hints from archetype
            var shipClassHints = GetShipClassHints(encounter.Archetype);

            // Find matching NPCShipArch
            var npcArch = FindMatchingNpcArch(faction, difficultyTag, shipClassHints);
            if (npcArch == null)
            {
                FLLog.Debug("EncounterResolver", $"No NPCShipArch found for faction={faction}, difficulty={difficultyTag}, archetype={encounter.Archetype}");
                return null;
            }

            // Resolve loadout
            if (!gameData.TryGetLoadout(npcArch.Loadout, out var loadout))
            {
                FLLog.Warning("EncounterResolver", $"Loadout not found: {npcArch.Loadout} for NPC arch {npcArch.Nickname}");
                return null;
            }

            // Resolve pilot
            var pilot = gameData.GetPilot(npcArch.Pilot);
            if (pilot == null)
            {
                FLLog.Warning("EncounterResolver", $"Pilot not found: {npcArch.Pilot} for NPC arch {npcArch.Nickname}");
                return null;
            }

            FLLog.Debug("EncounterResolver", $"Resolved: {encounter.Archetype} → {npcArch.Nickname} (faction={faction}, loadout={npcArch.Loadout})");

            return new ResolvedEncounter
            {
                Faction = faction,
                Loadout = loadout,
                Pilot = pilot,
                StateGraph = npcArch.StateGraph ?? "FIGHTER",
                ShipArchetype = npcArch.ShipArchetype,
                Difficulty = difficulty,
                SourceNpcArch = npcArch.Nickname
            };
        }

        /// <summary>
        /// Select a faction from encounter's FactionSpawns based on weighted chance.
        /// </summary>
        private string SelectFaction(Encounter encounter)
        {
            if (encounter.FactionSpawns == null || encounter.FactionSpawns.Count == 0)
                return null;

            // If single faction, return it
            if (encounter.FactionSpawns.Count == 1)
                return encounter.FactionSpawns[0].Faction;

            // Weighted random selection
            var totalChance = encounter.FactionSpawns.Sum(f => f.Chance);
            if (totalChance <= 0)
                return encounter.FactionSpawns[0].Faction;

            var roll = (float)random.NextDouble() * totalChance;
            var cumulative = 0f;

            foreach (var spawn in encounter.FactionSpawns)
            {
                cumulative += spawn.Chance;
                if (roll <= cumulative)
                    return spawn.Faction;
            }

            return encounter.FactionSpawns[^1].Faction;
        }

        /// <summary>
        /// Get ship class hints from encounter archetype name.
        /// </summary>
        private string[] GetShipClassHints(string archetype)
        {
            if (string.IsNullOrEmpty(archetype))
                return new[] { "class_fighter", "fighter" }; // Default to fighters

            foreach (var kvp in ArchetypeToShipClass)
            {
                if (archetype.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            // Default to fighters
            return new[] { "class_fighter", "fighter" };
        }

        /// <summary>
        /// Find NPCShipArch matching faction, difficulty, and ship class.
        /// </summary>
        private NPCShipArch FindMatchingNpcArch(string faction, string difficultyTag, string[] shipClassHints)
        {
            var factionPrefix = FactionToPrefix(faction);
            if (string.IsNullOrEmpty(factionPrefix))
                return null;

            // Try exact faction prefix match first
            if (factionCache.TryGetValue(factionPrefix, out var factionArches))
            {
                var match = FindInList(factionArches, difficultyTag, shipClassHints);
                if (match != null) return match;
            }

            // Try fallback: generic faction (fc_n_grp)
            if (factionCache.TryGetValue("fc_n", out var genericArches))
            {
                var match = FindInList(genericArches, difficultyTag, shipClassHints);
                if (match != null) return match;
            }

            // Last resort: any matching difficulty
            foreach (var arch in npcShipArches)
            {
                if (MatchesDifficulty(arch, difficultyTag) && MatchesShipClass(arch, shipClassHints))
                    return arch;
            }

            return null;
        }

        private NPCShipArch FindInList(List<NPCShipArch> arches, string difficultyTag, string[] shipClassHints)
        {
            // First pass: exact difficulty and ship class match
            var candidates = arches.Where(a =>
                MatchesDifficulty(a, difficultyTag) &&
                MatchesShipClass(a, shipClassHints)).ToList();

            if (candidates.Count > 0)
                return candidates[random.Next(candidates.Count)];

            // Second pass: just ship class match (any difficulty)
            candidates = arches.Where(a => MatchesShipClass(a, shipClassHints)).ToList();

            if (candidates.Count > 0)
                return candidates[random.Next(candidates.Count)];

            // Third pass: just difficulty match
            candidates = arches.Where(a => MatchesDifficulty(a, difficultyTag)).ToList();

            if (candidates.Count > 0)
                return candidates[random.Next(candidates.Count)];

            return null;
        }

        private static bool MatchesDifficulty(NPCShipArch arch, string difficultyTag)
        {
            if (arch.NpcClass == null || arch.NpcClass.Count == 0)
                return false;

            return arch.NpcClass.Any(c =>
                c.Equals(difficultyTag, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesShipClass(NPCShipArch arch, string[] shipClassHints)
        {
            if (arch.NpcClass == null || arch.NpcClass.Count == 0)
                return true; // No class restriction

            foreach (var hint in shipClassHints)
            {
                if (arch.NpcClass.Any(c =>
                    c.Contains(hint, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get all available faction prefixes for debugging.
        /// </summary>
        public IEnumerable<string> GetCachedFactions() => factionCache.Keys;

        /// <summary>
        /// Get count of NPCShipArch entries for a faction prefix.
        /// </summary>
        public int GetFactionArchCount(string factionPrefix)
        {
            return factionCache.TryGetValue(factionPrefix, out var list) ? list.Count : 0;
        }
    }
}
