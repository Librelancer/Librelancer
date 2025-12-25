// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Ai;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using Encounter = LibreLancer.Data.Schema.Universe.Encounter;
using Pilot = LibreLancer.Data.GameData.Pilot;

namespace LibreLancer.Server
{
    /// <summary>
    /// Manages NPC spawning based on zone encounters.
    /// Tracks player positions, resolves encounter definitions, and spawns NPCs
    /// with appropriate loadouts and AI behaviors.
    /// </summary>
    /// <remarks>
    /// <para>Sprint 3: Wander behavior - NPCs wander within spawn zones using AiWanderState.</para>
    /// <para>Sprint 4: Population management - Density-based spawning, respawn timers, despawn logic.</para>
    /// <para>Note: This class assumes single-threaded access from ServerWorld.Update().</para>
    /// </remarks>
    public class EncounterManager
    {
        private readonly ServerWorld world;
        private ZoneTracker zoneTracker; // Lazy initialized - not readonly
        private EncounterResolver resolver; // Lazy initialized with ZoneTracker
        private readonly Dictionary<string, ZoneSpawnState> zoneStates = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random random = new(); // Reuse instance for better randomness
        private double spawnCooldown = 0;

        // Population cap to prevent unbounded spawning (PROD-002)
        private const int MAX_NPCS_PER_ZONE = 8;

        // Spawn timing constants
        private const double SPAWN_COOLDOWN_SECONDS = 5.0;

        // Spawn position randomization range (in meters)
        private const int SPAWN_OFFSET_XZ = 1000;
        private const int SPAWN_OFFSET_Y = 500;

        // Wander behavior constants
        private const float DEFAULT_WANDER_RADIUS = 2000f;  // Default radius when zone size unavailable
        private const float ZONE_SIZE_MULTIPLIER = 0.4f;    // Fraction of zone size to use as wander radius
        private const float MIN_WANDER_RADIUS = 500f;       // Minimum wander radius (too small = too cramped)
        private const float MAX_WANDER_RADIUS = 5000f;      // Maximum wander radius (too large = too spread)

        // Sprint 4: Population management constants
        private const float DESPAWN_DISTANCE = 100000f;     // 100km - NPCs despawn when ALL players are this far (very high as requested)
        private const double DESPAWN_CHECK_INTERVAL = 30.0; // Check for despawns every 30 seconds
        private const int DENSITY_DIVISOR = 10;             // Divide zone.Density by this for max population
        private const double DEFAULT_REPOP_TIME = 60.0;     // Default respawn time if zone doesn't specify
        private const double DEFAULT_RELIEF_TIME = 30.0;    // Default relief time if zone doesn't specify
        private const int MAX_NPCS_PER_SYSTEM = 1000;       // Absolute cap on NPCs per system (very high limit as requested)

        // Fallback loadouts for when resolver fails (static to avoid repeated allocation)
        private static readonly string[] FallbackLoadouts =
        {
            "fc_or_ge_fighter_d3-5_oorp_oorp", // Generic fighter
            "fc_n_grp_lf_d1-2",                // Light fighter
            "li_n_li_elite_loadout01",         // Liberty Elite
            "br_n_br_elite_loadout01",         // Bretonia Elite
            "rh_n_rh_elite_loadout01"          // Rheinland Elite
        };

        // Fallback pilots for when resolver fails (static to avoid repeated allocation)
        private static readonly string[] FallbackPilots =
        {
            "pilot_pirate_ace",
            "pilot_military_ace",
            "pilot_military_med",
            "pilot_police_ace"
        };

        // Completely safe constructor - no zone access until Update
        public EncounterManager(ServerWorld world)
        {
            this.world = world;
            // Don't create ZoneTracker here - system may not be fully loaded
            // It will be lazily initialized on first Update after delay
            FLLog.Info("Encounters", "EncounterManager constructor complete (deferred init)");
        }

        // Debug: Track when we last logged to avoid spam
        private double debugLogCooldown = 0;
        private const double DEBUG_LOG_INTERVAL = 10.0; // Log every 10 seconds

        // Initialization delay to ensure system is fully loaded
        private double initDelay = 5.0; // Wait 5 seconds after system load
        private bool initialized = false;
        private bool initFailed = false; // If init fails, don't keep retrying

        // Sprint 4: Despawn check timer
        private double despawnCheckTimer = 0;

        // Sprint 4: Track total NPCs in system for global limit
        private int totalSystemNPCs = 0;

        // Sprint 4: Pre-allocated list for player positions (avoids GC in despawn checks)
        private readonly List<Vector3> playerPositionsCache = new(16);

        /// <summary>
        /// Update encounter spawning. Called each frame from ServerWorld.Update().
        /// </summary>
        public void Update(double delta)
        {
            // If initialization failed previously, don't keep trying
            if (initFailed)
                return;

            try
            {
                // Wait for system to fully initialize before processing encounters
                if (!initialized)
                {
                    initDelay -= delta;
                    if (initDelay > 0)
                        return;

                    // Now try to initialize ZoneTracker
                    try
                    {
                        if (world?.System == null)
                        {
                            FLLog.Warning("Encounters", "System is null after delay, disabling encounters");
                            initFailed = true;
                            return;
                        }

                        zoneTracker = new ZoneTracker(world.System);

                        // Initialize EncounterResolver with NPCShipArch data
                        var npcShips = world.Server.GameData.Items.Ini.NPCShips?.ShipArches;
                        if (npcShips != null && npcShips.Count > 0)
                        {
                            resolver = new EncounterResolver(world.Server.GameData.Items, npcShips);
                            FLLog.Info("Encounters", $"EncounterResolver initialized with {npcShips.Count} NPCShipArch entries");
                        }
                        else
                        {
                            FLLog.Warning("Encounters", "No NPCShipArch entries found, using fallback spawning only");
                        }

                        initialized = true;
                        FLLog.Info("Encounters", $"EncounterManager initialized for system: {world.System.Nickname}, Zones: {world.System.Zones?.Count ?? 0}");
                    }
                    catch (Exception ex)
                    {
                        FLLog.Error("Encounters", $"Failed to initialize ZoneTracker: {ex.Message}");
                        initFailed = true;
                        return;
                    }
                }

                spawnCooldown -= delta;
                debugLogCooldown -= delta;
                despawnCheckTimer -= delta;

                // Sprint 4: Update all zone timers (respawn, relief)
                UpdateAllZoneTimers(delta);

                // Sprint 4: Periodic despawn check for distant NPCs
                if (despawnCheckTimer <= 0)
                {
                    despawnCheckTimer = DESPAWN_CHECK_INTERVAL;
                    CheckDespawnDistantNPCs();
                }

                // Periodic debug logging to confirm system is running
                if (debugLogCooldown <= 0)
                {
                    debugLogCooldown = DEBUG_LOG_INTERVAL;
                    var playerCount = world?.Players?.Count ?? 0;
                    var systemName = world?.System?.Nickname ?? "null";
                    var zoneCount = world?.System?.Zones?.Count ?? 0;
                    FLLog.Info("Encounters", $"Update running - Players: {playerCount}, System: {systemName}, Zones: {zoneCount}, TotalNPCs: {totalSystemNPCs}");
                }

                if (spawnCooldown > 0)
                    return;

                // Null safety check
                if (world?.Players == null)
                    return;

                // Sprint 4: Check system-wide NPC limit
                if (totalSystemNPCs >= MAX_NPCS_PER_SYSTEM)
                {
                    FLLog.Debug("Encounters", $"System NPC limit reached ({totalSystemNPCs}/{MAX_NPCS_PER_SYSTEM}), skipping spawns");
                    return;
                }

                // Process spawns for each player
                foreach (var playerEntry in world.Players)
                {
                    var playerObject = playerEntry.Value;
                    if (playerObject == null)
                        continue;

                    var playerPosition = playerObject.WorldTransform.Position;
                    ProcessPlayerZones(playerPosition);
                }
            }
            catch (Exception ex)
            {
                FLLog.Error("Encounters", $"Error in Update: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sprint 4: Update all zone spawn timers.
        /// </summary>
        private void UpdateAllZoneTimers(double delta)
        {
            foreach (var state in zoneStates.Values)
            {
                state.UpdateTimers(delta);
            }
        }

        /// <summary>
        /// Sprint 4: Check and despawn NPCs that are far from all players.
        /// Only despawns when NO player is within DESPAWN_DISTANCE of the zone.
        /// </summary>
        private void CheckDespawnDistantNPCs()
        {
            if (world?.Players == null || world.Players.Count == 0)
                return;

            // Collect player positions using pre-allocated cache (avoids GC pressure)
            playerPositionsCache.Clear();
            foreach (var player in world.Players.Values)
            {
                if (player != null)
                    playerPositionsCache.Add(player.WorldTransform.Position);
            }

            if (playerPositionsCache.Count == 0)
                return;

            foreach (var state in zoneStates.Values)
            {
                if (state.SpawnedNPCs.Count == 0)
                    continue;

                // Check if ANY player is within despawn distance of this zone
                bool anyPlayerNearby = false;
                foreach (var playerPos in playerPositionsCache)
                {
                    var distance = Vector3.Distance(playerPos, state.ZonePosition);
                    if (distance < DESPAWN_DISTANCE)
                    {
                        anyPlayerNearby = true;
                        break;
                    }
                }

                // Only despawn if NO player is nearby (very conservative)
                if (!anyPlayerNearby)
                {
                    FLLog.Info("Encounters", $"Despawning {state.SpawnedNPCs.Count} NPCs in zone {state.ZoneNickname} - all players > {DESPAWN_DISTANCE/1000}km away");
                    DespawnZoneNPCs(state);
                }
            }
        }

        /// <summary>
        /// Sprint 4: Despawn all NPCs in a zone.
        /// </summary>
        private void DespawnZoneNPCs(ZoneSpawnState state)
        {
            // Copy list to avoid modification during iteration
            var npcsToRemove = state.SpawnedNPCs.ToList();

            foreach (var npc in npcsToRemove)
            {
                if (npc != null)
                {
                    world.NPCs.Despawn(npc, false);
                    totalSystemNPCs--;
                }
            }

            state.SpawnedNPCs.Clear();
            state.CurrentPopulation = 0;
            state.InitialSpawnComplete = false; // Allow re-spawning when player returns
        }

        private void ProcessPlayerZones(Vector3 playerPosition)
        {
            try
            {
                if (zoneTracker == null)
                {
                    FLLog.Warning("Encounters", "ZoneTracker is null");
                    return;
                }

                var encounterZones = zoneTracker.GetEncounterZones(playerPosition)?.ToList();
                if (encounterZones == null || encounterZones.Count == 0)
                    return;

                FLLog.Debug("Encounters", $"Player at {playerPosition} is in {encounterZones.Count} encounter zone(s)");

                foreach (var zone in encounterZones)
                {
                    if (zone == null || string.IsNullOrEmpty(zone.Nickname))
                        continue;

                    FLLog.Debug("Encounters", $"Processing zone: {zone.Nickname}, Encounters: {zone.Encounters?.Length ?? 0}");

                    if (!zoneStates.TryGetValue(zone.Nickname, out var state))
                    {
                        // Sprint 4: Calculate MaxPopulation from zone.Density
                        int maxPop = MAX_NPCS_PER_ZONE; // Default cap
                        if (zone.Density > 0)
                        {
                            // Density-based calculation: higher density = more NPCs
                            maxPop = Math.Max(1, (int)(zone.Density / DENSITY_DIVISOR));
                            maxPop = Math.Min(maxPop, MAX_NPCS_PER_ZONE); // Cap at per-zone max
                        }

                        // Sprint 4: Get timing values from zone data
                        double repopTime = zone.RepopTime > 0 ? zone.RepopTime : DEFAULT_REPOP_TIME;
                        double reliefTime = zone.ReliefTime > 0 ? zone.ReliefTime : DEFAULT_RELIEF_TIME;

                        state = new ZoneSpawnState
                        {
                            ZoneNickname = zone.Nickname,
                            MaxPopulation = maxPop,
                            RepopTime = repopTime,
                            ReliefTime = reliefTime,
                            ReliefTimer = reliefTime,  // Start relief countdown
                            IsActive = reliefTime <= 0, // Immediately active if no relief time
                            ZonePosition = zone.Position
                        };
                        zoneStates[zone.Nickname] = state;
                        FLLog.Info("Encounters", $"Created spawn state for zone: {zone.Nickname}, MaxPop={maxPop} (Density={zone.Density}), RepopTime={repopTime}s, ReliefTime={reliefTime}s");
                    }

                    // Sprint 4: Use CanSpawn() which checks IsActive, population, and respawn timer
                    if (!state.CanSpawn())
                    {
                        FLLog.Debug("Encounters", $"Zone {zone.Nickname}: CanSpawn=false (Active={state.IsActive}, Pop={state.CurrentPopulation}/{state.MaxPopulation}, Timer={state.RespawnTimer:F1}s)");
                        continue;
                    }

                    // Select an encounter from the zone's encounters (weighted by Chance)
                    var encounter = SelectEncounter(zone);

                    // Attempt to spawn NPC using resolver, with fallback
                    // SpawnNPC now handles AddNPC and totalSystemNPCs tracking internally
                    if (SpawnNPC(encounter, zone, state))
                    {
                        state.InitialSpawnComplete = true;
                        state.LastSpawnTime = world.Server.TotalTime;
                        spawnCooldown = SPAWN_COOLDOWN_SECONDS;
                    }
                }
            }
            catch (Exception ex)
            {
                FLLog.Error("Encounters", $"Error in ProcessPlayerZones: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Calculate a random spawn position within a zone.
        /// </summary>
        /// <param name="zone">The zone to spawn in.</param>
        /// <returns>World-space position for NPC spawn.</returns>
        private Vector3 CalculateSpawnPosition(Zone zone)
        {
            var offset = new Vector3(
                random.Next(-SPAWN_OFFSET_XZ, SPAWN_OFFSET_XZ),
                random.Next(-SPAWN_OFFSET_Y, SPAWN_OFFSET_Y),
                random.Next(-SPAWN_OFFSET_XZ, SPAWN_OFFSET_XZ));
            return zone.Position + offset;
        }

        /// <summary>
        /// Select an encounter from the zone's encounters based on weighted chance.
        /// </summary>
        /// <param name="zone">Zone containing encounters.</param>
        /// <returns>Selected encounter, or null if no encounters defined.</returns>
        private Encounter SelectEncounter(Zone zone)
        {
            if (zone.Encounters == null || zone.Encounters.Length == 0)
                return null;

            // If single encounter, return it
            if (zone.Encounters.Length == 1)
                return zone.Encounters[0];

            // Weighted random selection based on Chance
            var totalChance = 0f;
            foreach (var enc in zone.Encounters)
                totalChance += enc.Chance;

            if (totalChance <= 0)
                return zone.Encounters[0];

            var roll = (float)random.NextDouble() * totalChance;
            var cumulative = 0f;

            foreach (var enc in zone.Encounters)
            {
                cumulative += enc.Chance;
                if (roll <= cumulative)
                    return enc;
            }

            return zone.Encounters[^1];
        }

        /// <summary>
        /// Spawn an NPC using EncounterResolver for proper configuration.
        /// Falls back to SpawnTestNPC if resolver fails.
        /// </summary>
        /// <param name="encounter">The encounter definition from zone data.</param>
        /// <param name="zone">The zone to spawn in.</param>
        /// <param name="state">Zone spawn state for population tracking.</param>
        /// <returns>True if spawn succeeded, false otherwise.</returns>
        private bool SpawnNPC(Encounter encounter, Zone zone, ZoneSpawnState state)
        {
            // Try resolver first (Sprint 2)
            if (resolver != null && encounter != null)
            {
                var resolved = resolver.Resolve(encounter, zone);
                if (resolved != null && resolved.IsValid)
                {
                    var position = CalculateSpawnPosition(zone);

                    try
                    {
                        var npc = world.NPCs.DoSpawn(
                            world.NPCs.RandomName(resolved.Faction),
                            null,                    // nickname (null = auto-generated)
                            resolved.Faction,        // affiliation
                            resolved.StateGraph,     // stateGraph from NPCShipArch
                            null, null, null,        // comm appearance (head, body, helmet)
                            resolved.Loadout,
                            resolved.Pilot,
                            position,
                            Quaternion.Identity,
                            null,                    // arrivalObj
                            0                        // arrivalIndex
                        );

                        if (npc != null)
                        {
                            LogNPCComponentDiagnostics(npc);
                            SetWanderBehavior(npc, zone, state); // Sprint 5: Pass state for formation support

                            // Sprint 4: Track NPC in zone state and system count
                            state.AddNPC(npc);
                            totalSystemNPCs++;
                            FLLog.Debug("Encounters", $"Spawned NPC via resolver in zone {zone.Nickname}: {resolved.SourceNpcArch} (faction={resolved.Faction}), ZonePop={state.CurrentPopulation}/{state.MaxPopulation}, SystemTotal={totalSystemNPCs}");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        FLLog.Error("Encounters", $"Failed to spawn resolved NPC in zone {zone.Nickname}: {ex.Message}");
                    }
                }
                else
                {
                    FLLog.Debug("Encounters", $"Resolver returned null for encounter {encounter.Archetype} in zone {zone.Nickname}, falling back");
                }
            }

            // Fallback to test NPC (Sprint 1 behavior)
            return SpawnTestNPC(zone, state);
        }

        /// <summary>
        /// Spawn a test NPC with fallback configuration.
        /// Used when EncounterResolver fails to find a matching NPCShipArch.
        /// </summary>
        /// <param name="zone">The zone to spawn in.</param>
        /// <param name="state">Zone spawn state for population tracking.</param>
        /// <returns>True if spawn succeeded, false otherwise.</returns>
        private bool SpawnTestNPC(Zone zone, ZoneSpawnState state)
        {
            // Resolve loadout from fallback list
            ObjectLoadout loadout = null;
            foreach (var name in FallbackLoadouts)
            {
                if (world.Server.GameData.Items.TryGetLoadout(name, out loadout))
                {
                    FLLog.Debug("Encounters", $"Using loadout: {name}");
                    break;
                }
            }

            // NULL SAFETY CHECK (PROD-001 - CRITICAL)
            if (loadout == null)
            {
                FLLog.Warning("Encounters", $"No valid loadout found for zone {zone.Nickname}, skipping spawn");
                state.InitialSpawnComplete = true; // Don't keep retrying
                return false;
            }

            // Resolve pilot from fallback list
            Pilot pilot = null;
            foreach (var name in FallbackPilots)
            {
                pilot = world.Server.GameData.Items.GetPilot(name);
                if (pilot != null)
                {
                    FLLog.Debug("Encounters", $"Using pilot: {name}");
                    break;
                }
            }

            // NULL SAFETY CHECK (PROD-001 - CRITICAL)
            if (pilot == null)
            {
                FLLog.Warning("Encounters", $"No valid pilot found for zone {zone.Nickname}, skipping spawn");
                state.InitialSpawnComplete = true; // Don't keep retrying
                return false;
            }

            var position = CalculateSpawnPosition(zone);

            // Determine faction from zone's first encounter if available
            string faction = "fc_n_grp"; // Default neutral faction
            if (zone.Encounters?.Length > 0 && zone.Encounters[0].FactionSpawns?.Count > 0)
            {
                faction = zone.Encounters[0].FactionSpawns[0].Faction ?? faction;
            }

            try
            {
                var npc = world.NPCs.DoSpawn(
                    world.NPCs.RandomName(faction),
                    null,                    // nickname (null = auto-generated)
                    faction,                 // affiliation
                    "FIGHTER",               // stateGraph
                    null, null, null,        // comm appearance (head, body, helmet)
                    loadout,
                    pilot,
                    position,
                    Quaternion.Identity,
                    null,                    // arrivalObj
                    0                        // arrivalIndex
                );

                if (npc != null)
                {
                    LogNPCComponentDiagnostics(npc);
                    SetWanderBehavior(npc, zone, state); // Sprint 5: Pass state for formation support

                    // Sprint 4: Track NPC in zone state and system count
                    state.AddNPC(npc);
                    totalSystemNPCs++;
                    FLLog.Debug("Encounters", $"Spawned fallback NPC in zone {zone.Nickname} at {position}, ZonePop={state.CurrentPopulation}/{state.MaxPopulation}, SystemTotal={totalSystemNPCs}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                FLLog.Error("Encounters", $"Failed to spawn NPC in zone {zone.Nickname}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Log diagnostic information about NPC movement components.
        /// Warns if critical components are missing.
        /// </summary>
        /// <param name="npc">The spawned NPC to diagnose.</param>
        private static void LogNPCComponentDiagnostics(GameObject npc)
        {
            var hasEngine = npc.GetComponent<Server.Components.SEngineComponent>() != null;
            var hasPower = npc.GetComponent<World.Components.PowerCoreComponent>() != null;
            var hasAutopilot = npc.GetComponent<World.Components.AutopilotComponent>() != null;
            var hasSteering = npc.GetComponent<World.Components.ShipSteeringComponent>() != null;
            var hasPhysics = npc.GetComponent<World.Components.ShipPhysicsComponent>() != null;
            FLLog.Debug("Encounters", $"NPC components - Engine:{hasEngine}, Power:{hasPower}, Autopilot:{hasAutopilot}, Steering:{hasSteering}, Physics:{hasPhysics}");

            if (!hasEngine || !hasPower)
            {
                FLLog.Warning("Encounters", $"NPC missing critical components for movement! Engine:{hasEngine}, Power:{hasPower}");
            }
        }

        /// <summary>
        /// Set the wander or formation behavior on a spawned NPC.
        /// NPCs will either wander within a radius of the zone center,
        /// or join an existing formation as a wingman.
        /// </summary>
        /// <param name="npc">The spawned NPC GameObject.</param>
        /// <param name="zone">The zone the NPC was spawned in.</param>
        /// <param name="state">Zone spawn state for formation tracking.</param>
        private void SetWanderBehavior(GameObject npc, Zone zone, ZoneSpawnState state = null)
        {
            if (npc == null) return;

            // Calculate wander radius based on zone size
            float wanderRadius = DEFAULT_WANDER_RADIUS;

            // Try to get zone size from zone properties
            if (zone.Size.X > 0 && zone.Size.Z > 0)
            {
                // Use smaller of X/Z dimensions, capped at reasonable values
                wanderRadius = Math.Min(zone.Size.X, zone.Size.Z) * ZONE_SIZE_MULTIPLIER;
                wanderRadius = Math.Clamp(wanderRadius, MIN_WANDER_RADIUS, MAX_WANDER_RADIUS);
            }

            // Set AI state on the NPC's SNPCComponent
            if (!npc.TryGetComponent<SNPCComponent>(out var snpc))
            {
                FLLog.Warning("Encounters", $"NPC has no SNPCComponent - cannot set behavior!");
                return;
            }

            // Get autopilot for logging (used in multiple places)
            npc.TryGetComponent<AutopilotComponent>(out var autopilot);

            // Sprint 5: Formation flying support
            if (state != null)
            {
                var leader = state.GetValidWingLeader();
                var wingSize = state.GetWingSize();

                // First NPC becomes wing leader (wanders)
                if (leader == null)
                {
                    state.WingLeader = npc;
                    FLLog.Debug("Encounters", $"NPC {npc.Nickname} is now wing leader for zone {zone.Nickname}");
                    // Leader gets wander behavior - fall through
                }
                // Subsequent NPCs join formation (up to MAX_WING_SIZE)
                else if (wingSize < ZoneSpawnState.MAX_WING_SIZE)
                {
                    FLLog.Debug("Encounters", $"NPC {npc.Nickname} joining formation with leader {leader.Nickname} (wing size: {wingSize + 1})");
                    var formationState = new AiFormationState(leader, default, zone.Position, wanderRadius);
                    snpc.SetState(formationState);

                    if (autopilot != null)
                    {
                        FLLog.Debug("Encounters", $"Autopilot behavior after formation: {autopilot.CurrentBehavior}");
                    }
                    return; // Don't set wander state
                }
                else
                {
                    // Wing is full - this NPC starts a new wing as leader
                    state.WingLeader = npc;
                    FLLog.Debug("Encounters", $"Wing full, NPC {npc.Nickname} starts new wing in zone {zone.Nickname}");
                    // Fall through to wander behavior
                }
            }

            // Default: Wander behavior (for leaders and solo NPCs)
            FLLog.Debug("Encounters", $"Setting wander state: center={zone.Position}, radius={wanderRadius}");
            var wanderState = new AiWanderState(zone.Position, wanderRadius);
            snpc.SetState(wanderState);
            FLLog.Debug("Encounters", $"Wander state set. Current directive: {snpc.CurrentDirective}");

            if (autopilot != null)
            {
                FLLog.Debug("Encounters", $"Autopilot behavior after SetState: {autopilot.CurrentBehavior}");
            }
        }

        /// <summary>
        /// Called when an NPC is destroyed to update population tracking.
        /// Enables respawning by decrementing zone population count.
        /// </summary>
        /// <param name="zoneNickname">The nickname of the zone the NPC was in.</param>
        public void OnNPCDestroyed(string zoneNickname)
        {
            if (string.IsNullOrEmpty(zoneNickname))
                return;

            if (zoneStates.TryGetValue(zoneNickname, out var state) && state.CurrentPopulation > 0)
            {
                state.CurrentPopulation--;
                totalSystemNPCs = Math.Max(0, totalSystemNPCs - 1);

                // Sprint 4: Start respawn timer to allow repopulation after RepopTime
                state.StartRespawnTimer();

                FLLog.Debug("Encounters", $"NPC destroyed in {zoneNickname}, population now {state.CurrentPopulation}/{state.MaxPopulation}, respawn in {state.RepopTime}s, SystemTotal={totalSystemNPCs}");
            }
        }

        /// <summary>
        /// Sprint 4: Called when an NPC is destroyed with the NPC object.
        /// This overload allows proper removal from tracking lists.
        /// </summary>
        /// <param name="npc">The destroyed NPC GameObject.</param>
        /// <param name="zoneNickname">The nickname of the zone the NPC was in.</param>
        public void OnNPCDestroyed(GameObject npc, string zoneNickname)
        {
            if (npc == null || string.IsNullOrEmpty(zoneNickname))
                return;

            if (zoneStates.TryGetValue(zoneNickname, out var state))
            {
                // Remove NPC from tracking (this updates CurrentPopulation via list count)
                state.RemoveNPC(npc);
                totalSystemNPCs = Math.Max(0, totalSystemNPCs - 1);

                // Start respawn timer to allow repopulation after RepopTime
                state.StartRespawnTimer();

                FLLog.Debug("Encounters", $"NPC destroyed in {zoneNickname}, population now {state.CurrentPopulation}/{state.MaxPopulation}, respawn in {state.RepopTime}s, SystemTotal={totalSystemNPCs}");
            }
        }

        /// <summary>
        /// Get current spawn state for debugging.
        /// </summary>
        public IReadOnlyDictionary<string, ZoneSpawnState> GetZoneStates() => zoneStates;

        /// <summary>
        /// Get spawn state for a specific zone.
        /// </summary>
        /// <param name="zoneNickname">Zone nickname to look up.</param>
        /// <returns>Zone spawn state if tracked, null otherwise.</returns>
        public ZoneSpawnState GetZoneState(string zoneNickname)
        {
            return zoneStates.TryGetValue(zoneNickname, out var state) ? state : null;
        }
    }

    /// <summary>
    /// Tracks spawn state for a single encounter zone.
    /// Manages population counts, spawn timing, and respawn logic.
    /// </summary>
    /// <remarks>
    /// Sprint 4: Enhanced with timer-based respawning and NPC tracking.
    /// - RespawnTimer: Counts down to allow next spawn (from zone.RepopTime)
    /// - ReliefTimer: Initial cooldown before zone becomes active (from zone.ReliefTime)
    /// - SpawnedNPCs: Tracks NPCs for despawn/destruction handling
    /// </remarks>
    public class ZoneSpawnState
    {
        /// <summary>Zone identifier for lookup.</summary>
        public string ZoneNickname { get; init; }

        /// <summary>Current number of spawned NPCs in this zone.</summary>
        public int CurrentPopulation { get; set; }

        /// <summary>Maximum NPCs allowed in this zone (population cap).</summary>
        public int MaxPopulation { get; set; }

        /// <summary>Whether initial spawn wave has occurred.</summary>
        public bool InitialSpawnComplete { get; set; }

        /// <summary>Server time when last spawn occurred.</summary>
        public double LastSpawnTime { get; set; }

        /// <summary>Time remaining before next respawn is allowed (seconds).</summary>
        public double RespawnTimer { get; set; }

        /// <summary>Time remaining in relief period after initial spawn (seconds).</summary>
        public double ReliefTimer { get; set; }

        /// <summary>Whether zone is active (relief period has ended).</summary>
        public bool IsActive { get; set; }

        /// <summary>RepopTime value from zone data (seconds between respawns).</summary>
        public double RepopTime { get; set; }

        /// <summary>ReliefTime value from zone data (initial cooldown).</summary>
        public double ReliefTime { get; set; }

        /// <summary>Zone center position for distance calculations.</summary>
        public Vector3 ZonePosition { get; set; }

        /// <summary>NPCs currently spawned in this zone (for tracking/despawn).</summary>
        public List<GameObject> SpawnedNPCs { get; } = new();

        /// <summary>
        /// Sprint 5: Wing leader for formation flying.
        /// First NPC spawned becomes leader, subsequent NPCs join formation.
        /// </summary>
        public GameObject WingLeader { get; set; }

        /// <summary>
        /// Sprint 5: Maximum wing size for formations.
        /// Wings beyond this size start a new formation.
        /// </summary>
        public const int MAX_WING_SIZE = 4;

        /// <summary>
        /// Sprint 5: Get the current wing leader if valid.
        /// Returns null if no leader or leader is dead.
        /// </summary>
        public GameObject GetValidWingLeader()
        {
            if (WingLeader != null && (WingLeader.Flags & GameObjectFlags.Exists) != 0)
                return WingLeader;
            WingLeader = null;
            return null;
        }

        /// <summary>
        /// Sprint 5: Get wing size (including leader).
        /// </summary>
        public int GetWingSize()
        {
            var leader = GetValidWingLeader();
            if (leader?.Formation == null)
                return leader != null ? 1 : 0;
            return 1 + leader.Formation.Followers.Count;
        }

        /// <summary>Check if more NPCs can spawn in this zone.</summary>
        public bool CanSpawn() => IsActive && CurrentPopulation < MaxPopulation && RespawnTimer <= 0;

        /// <summary>Calculate how many NPCs are needed to reach MaxPopulation.</summary>
        public int GetSpawnDeficit() => Math.Max(0, MaxPopulation - CurrentPopulation);

        /// <summary>Update timers each frame.</summary>
        public void UpdateTimers(double delta)
        {
            if (ReliefTimer > 0)
            {
                ReliefTimer -= delta;
                if (ReliefTimer <= 0)
                {
                    IsActive = true;
                    FLLog.Debug("Encounters", $"Zone {ZoneNickname} relief period ended, now active");
                }
            }

            if (RespawnTimer > 0)
            {
                RespawnTimer -= delta;
            }
        }

        /// <summary>Start respawn cooldown after an NPC dies.</summary>
        public void StartRespawnTimer()
        {
            RespawnTimer = RepopTime;
            InitialSpawnComplete = false; // Allow respawning
        }

        /// <summary>Remove destroyed/despawned NPC from tracking.</summary>
        /// <returns>True if NPC was found and removed, false otherwise.</returns>
        public bool RemoveNPC(GameObject npc)
        {
            bool removed = SpawnedNPCs.Remove(npc);
            CurrentPopulation = SpawnedNPCs.Count;

            // Sprint 5: Clear wing leader if the removed NPC was the leader
            if (npc == WingLeader)
            {
                WingLeader = null;
                FLLog.Debug("Encounters", $"Wing leader removed from zone {ZoneNickname}, next spawn will become new leader");
            }

            if (!removed)
            {
                FLLog.Warning("Encounters", $"RemoveNPC: NPC not found in zone {ZoneNickname} tracking list (may have been despawned already)");
            }
            return removed;
        }

        /// <summary>Add newly spawned NPC to tracking.</summary>
        public void AddNPC(GameObject npc)
        {
            SpawnedNPCs.Add(npc);
            CurrentPopulation = SpawnedNPCs.Count;
        }
    }
}
