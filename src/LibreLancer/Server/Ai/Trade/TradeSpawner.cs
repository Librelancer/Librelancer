// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Components;
using LibreLancer.World;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server.Ai.Trade
{
    /// <summary>
    /// Manages spawning of trader NPCs along trade routes.
    /// Can be triggered by timers, player proximity, or zone encounters.
    /// </summary>
    public class TradeSpawner
    {
        private readonly ServerWorld world;
        private readonly Random random = new();

        // Spawn timing
        private double spawnTimer = 0;
        private const double SPAWN_INTERVAL = 60.0;  // Try to spawn every 60 seconds
        private const double INITIAL_DELAY = 10.0;   // Wait before first spawn
        private double initDelay;

        // Population limits
        private const int MAX_TRADERS_PER_SYSTEM = 10;
        private int currentTraderCount = 0;

        // Tracked traders for cleanup
        private readonly List<GameObject> activeTraders = new();

        // Fallback loadouts for traders
        private static readonly string[] TraderLoadouts =
        {
            "fc_or_transport_d4",
            "fc_n_grp_freighter_d3",
            "rh_n_freighter_d3",
            "br_n_freighter_d3",
            "li_n_freighter_d3"
        };

        // Fallback pilots
        private static readonly string[] TraderPilots =
        {
            "pilot_pirate_easy",
            "pilot_police_easy"
        };

        // Trade factions
        private static readonly string[] TradeFactions =
        {
            "li_n_grp",
            "br_n_grp",
            "rh_n_grp",
            "ku_n_grp",
            "co_n_grp"
        };

        public TradeSpawner(ServerWorld world)
        {
            this.world = world;
            this.initDelay = INITIAL_DELAY;
            FLLog.Info("TradeSpawner", "TradeSpawner initialized");
        }

        /// <summary>
        /// Update trader spawning. Called each frame from ServerWorld.
        /// </summary>
        public void Update(double delta)
        {
            // Initial delay
            if (initDelay > 0)
            {
                initDelay -= delta;
                return;
            }

            // Clean up destroyed traders
            CleanupDestroyedTraders();

            // Check spawn timer
            spawnTimer += delta;
            if (spawnTimer < SPAWN_INTERVAL)
                return;

            spawnTimer = 0;

            // Check population limit
            if (currentTraderCount >= MAX_TRADERS_PER_SYSTEM)
            {
                FLLog.Debug("TradeSpawner", $"At trader limit ({currentTraderCount}/{MAX_TRADERS_PER_SYSTEM})");
                return;
            }

            // Try to spawn a trader
            TrySpawnTrader();
        }

        /// <summary>
        /// Attempt to spawn a trader between two bases.
        /// </summary>
        private void TrySpawnTrader()
        {
            // Find bases in the system
            var bases = FindDockableBases();
            if (bases.Count < 2)
            {
                FLLog.Debug("TradeSpawner", "Not enough bases for trade route");
                return;
            }

            // Pick random origin and destination
            var origin = bases[random.Next(bases.Count)];
            var destination = bases.Where(b => b != origin).OrderBy(_ => random.Next()).FirstOrDefault();

            if (destination == null)
                return;

            // Get loadout
            ObjectLoadout loadout = null;
            foreach (var name in TraderLoadouts)
            {
                if (world.Server.GameData.Items.TryGetLoadout(name, out loadout))
                    break;
            }

            if (loadout == null)
            {
                FLLog.Warning("TradeSpawner", "No valid trader loadout found");
                return;
            }

            // Get pilot
            Pilot pilot = null;
            foreach (var name in TraderPilots)
            {
                pilot = world.Server.GameData.Items.GetPilot(name);
                if (pilot != null)
                    break;
            }

            if (pilot == null)
            {
                FLLog.Warning("TradeSpawner", "No valid trader pilot found");
                return;
            }

            // Pick faction
            var faction = TradeFactions[random.Next(TradeFactions.Length)];

            // Spawn the trader
            var trader = world.NPCs.SpawnTrader(
                origin,
                destination,
                loadout,
                pilot,
                faction,
                roundTrip: random.NextDouble() > 0.5);

            if (trader != null)
            {
                activeTraders.Add(trader);
                currentTraderCount++;
                FLLog.Info("TradeSpawner", $"Spawned trader from {origin.Nickname} to {destination.Nickname} (total: {currentTraderCount})");
            }
        }

        /// <summary>
        /// Spawn a trader with specific parameters.
        /// </summary>
        public GameObject SpawnTrader(
            string originNickname,
            string destinationNickname,
            string faction = null,
            bool roundTrip = false)
        {
            var origin = world.GameWorld.GetObject(originNickname);
            var destination = world.GameWorld.GetObject(destinationNickname);

            if (origin == null || destination == null)
            {
                FLLog.Warning("TradeSpawner", $"Invalid base nicknames: {originNickname}, {destinationNickname}");
                return null;
            }

            // Get loadout
            ObjectLoadout loadout = null;
            foreach (var name in TraderLoadouts)
            {
                if (world.Server.GameData.Items.TryGetLoadout(name, out loadout))
                    break;
            }

            if (loadout == null)
                return null;

            // Get pilot
            Pilot pilot = null;
            foreach (var name in TraderPilots)
            {
                pilot = world.Server.GameData.Items.GetPilot(name);
                if (pilot != null)
                    break;
            }

            if (pilot == null)
                return null;

            faction ??= TradeFactions[random.Next(TradeFactions.Length)];

            var trader = world.NPCs.SpawnTrader(origin, destination, loadout, pilot, faction, roundTrip);

            if (trader != null)
            {
                activeTraders.Add(trader);
                currentTraderCount++;
            }

            return trader;
        }

        /// <summary>
        /// Find all dockable bases in the current system.
        /// </summary>
        private List<GameObject> FindDockableBases()
        {
            var bases = new List<GameObject>();

            foreach (var obj in world.GameWorld.Objects)
            {
                if (obj.TryGetComponent<SDockableComponent>(out var dock))
                {
                    // Check if it's a base (not a tradelane or jump gate)
                    if (dock.Action.Kind == Data.GameData.World.DockKinds.Base ||
                        dock.Action.Kind == Data.GameData.World.DockKinds.Jump)
                    {
                        // Only include actual stations/planets, not jump gates
                        if (dock.Action.Kind == Data.GameData.World.DockKinds.Base)
                        {
                            bases.Add(obj);
                        }
                    }
                }
            }

            return bases;
        }

        /// <summary>
        /// Remove destroyed traders from tracking.
        /// </summary>
        private void CleanupDestroyedTraders()
        {
            for (int i = activeTraders.Count - 1; i >= 0; i--)
            {
                var trader = activeTraders[i];
                if (trader == null || (trader.Flags & GameObjectFlags.Exists) == 0)
                {
                    activeTraders.RemoveAt(i);
                    currentTraderCount--;
                }
                else if (trader.TryGetComponent<SNPCComponent>(out var npc))
                {
                    // Check if trade is complete
                    if (npc.CurrentDirective is AiTradeState trade && trade.IsComplete)
                    {
                        activeTraders.RemoveAt(i);
                        currentTraderCount--;
                        // NPC will despawn on its own
                    }
                }
            }
        }

        /// <summary>
        /// Get current trader count for monitoring.
        /// </summary>
        public int GetActiveTraderCount() => currentTraderCount;

        /// <summary>
        /// Force spawn a trader (ignores limits).
        /// </summary>
        public void ForceSpawn()
        {
            TrySpawnTrader();
        }
    }
}
