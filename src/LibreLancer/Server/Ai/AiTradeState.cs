// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Ai.Trade;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;
using static LibreLancer.Server.Ai.Trade.TradeConstants;

namespace LibreLancer.Server.Ai
{
    /// <summary>
    /// AI state for NPCs that follow trade routes between bases.
    /// Uses tradelanes for efficient transit and docks at destinations to simulate trading.
    /// </summary>
    /// <remarks>
    /// Trade behavior phases:
    /// 1. SeekingTradeLane - Navigate to nearest tradelane entry point
    /// 2. ApproachingLane - Close approach to initiate tradelane dock
    /// 3. InTradeLane - Component handles transit at 2500 speed
    /// 4. ExitedTradelane - Check if more lanes or proceed to destination
    /// 5. ApproachingDestination - Navigate to target base
    /// 6. Docking - Dock at destination base
    /// 7. Trading - Simulated trading pause at destination
    /// 8. Complete - Ready for despawn or return trip
    ///
    /// Handles combat interruption, tradelane disruption, and object destruction gracefully.
    /// </remarks>
    public class AiTradeState : AiState
    {
        // Route definition
        private readonly GameObject originBase;
        private readonly GameObject destinationBase;
        private readonly bool roundTrip;

        // State tracking
        private TradePhase phase = TradePhase.SeekingTradeLane;
        private List<TradelaneSegment> tradelaneRoute = new();
        private int currentLaneIndex;
        private Vector3 expectedExitPosition;

        // Timers
        private double phaseTimer;
        private double tradingTimer;

        // Constants
        private const double TRADING_TIME = 10.0;           // Time at destination
        private const float LANE_APPROACH_DIST = 500f;      // Distance to initiate lane dock
        private const float BASE_APPROACH_DIST = 500f;      // Distance to initiate base dock
        private const float DISRUPTION_THRESHOLD = 1000f;   // Distance to detect disruption
        private const float TRADE_THROTTLE = 0.9f;          // Normal transit speed

        // Timeout protection
        private const double SEEKING_TIMEOUT = 120.0;       // 2 min to find tradelane
        private const double APPROACH_TIMEOUT = 60.0;       // 1 min to approach lane
        private const double DESTINATION_TIMEOUT = 180.0;   // 3 min to reach destination
        private const double DOCKING_TIMEOUT = 30.0;        // 30s to complete dock

        // Combat state
        private bool wasInCombat;

        /// <summary>
        /// Indicates whether trade cycle is complete and NPC can be despawned.
        /// </summary>
        public bool IsComplete => phase == TradePhase.Complete;

        /// <summary>
        /// Trade route phases.
        /// </summary>
        public enum TradePhase
        {
            /// <summary>Flying to nearest tradelane entry.</summary>
            SeekingTradeLane,
            /// <summary>Close to lane, initiating dock.</summary>
            ApproachingLane,
            /// <summary>STradelaneMoveComponent handles movement.</summary>
            InTradeLane,
            /// <summary>Exited tradelane, checking next step.</summary>
            ExitedTradelane,
            /// <summary>Flying to destination base.</summary>
            ApproachingDestination,
            /// <summary>Docking at destination.</summary>
            Docking,
            /// <summary>Simulated trading (timer).</summary>
            Trading,
            /// <summary>Done, ready for despawn.</summary>
            Complete
        }

        /// <summary>
        /// Represents a segment of tradelane route.
        /// </summary>
        private class TradelaneSegment
        {
            public GameObject EntryRing { get; set; }
            public GameObject ExitRing { get; set; }
            public Vector3 ExitPosition { get; set; }
        }

        /// <summary>
        /// Disable combat pursuit during critical phases.
        /// </summary>
        public override bool AllowCombatInterruption =>
            phase != TradePhase.InTradeLane &&
            phase != TradePhase.Docking &&
            phase != TradePhase.Trading;

        /// <summary>
        /// Create a trade state for traveling between bases.
        /// </summary>
        /// <param name="origin">Starting base (for return trips).</param>
        /// <param name="destination">Target destination base.</param>
        /// <param name="roundTrip">Whether to return to origin after trading.</param>
        public AiTradeState(GameObject origin, GameObject destination, bool roundTrip = false)
        {
            this.originBase = origin;
            this.destinationBase = destination;
            this.roundTrip = roundTrip;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (!ValidateDestination())
            {
                FLLog.Warning("AiTradeState", $"Invalid destination for {obj?.Nickname ?? "null"}, aborting trade");
                TransitionTo(TradePhase.Complete);
                return;
            }

            FLLog.Info("AiTradeState", $"Starting trade route for {obj?.Nickname ?? "null"}: " +
                $"{originBase?.Nickname ?? "?"} -> {destinationBase?.Nickname ?? "?"}");

            // Build tradelane route
            BuildTradelaneRoute(obj);

            if (tradelaneRoute.Count > 0)
            {
                // Navigate to first tradelane entry
                NavigateToTradelane(obj, tradelaneRoute[0]);
                TransitionTo(TradePhase.SeekingTradeLane);
            }
            else
            {
                // No tradelanes available, fly direct
                FLLog.Debug("AiTradeState", "No tradelane route found, flying direct");
                NavigateToBase(obj, destinationBase);
                TransitionTo(TradePhase.ApproachingDestination);
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double dt)
        {
            // Update phase timer
            phaseTimer += dt;

            // Handle combat interruption/resume
            HandleCombatResume(obj);

            // Validate destination still exists
            if (!ValidateDestination() && phase != TradePhase.Complete)
            {
                FLLog.Warning("AiTradeState", "Destination destroyed during transit");
                HandleDestinationLost(obj, ai);
                return;
            }

            switch (phase)
            {
                case TradePhase.SeekingTradeLane:
                    UpdateSeekingTradeLane(obj);
                    break;
                case TradePhase.ApproachingLane:
                    UpdateApproachingLane(obj);
                    break;
                case TradePhase.InTradeLane:
                    UpdateInTradeLane(obj);
                    break;
                case TradePhase.ExitedTradelane:
                    UpdateExitedTradelane(obj);
                    break;
                case TradePhase.ApproachingDestination:
                    UpdateApproachingDestination(obj);
                    break;
                case TradePhase.Docking:
                    UpdateDocking(obj);
                    break;
                case TradePhase.Trading:
                    UpdateTrading(obj, ai, dt);
                    break;
                case TradePhase.Complete:
                    // Nothing to do, waiting for despawn
                    break;
            }
        }

        #region Phase Updates

        private void UpdateSeekingTradeLane(GameObject obj)
        {
            // Timeout check
            if (phaseTimer > SEEKING_TIMEOUT)
            {
                FLLog.Warning("AiTradeState", "Timeout seeking tradelane, flying direct");
                NavigateToBase(obj, destinationBase);
                TransitionTo(TradePhase.ApproachingDestination);
                return;
            }

            if (currentLaneIndex >= tradelaneRoute.Count)
            {
                NavigateToBase(obj, destinationBase);
                TransitionTo(TradePhase.ApproachingDestination);
                return;
            }

            var segment = tradelaneRoute[currentLaneIndex];
            if (!ValidateGameObject(segment.EntryRing))
            {
                // Tradelane destroyed, skip to next or fly direct
                AdvanceToNextLane(obj);
                return;
            }

            var distance = Vector3.Distance(
                obj.WorldTransform.Position,
                segment.EntryRing.WorldTransform.Position);

            if (distance < LANE_APPROACH_DIST)
            {
                TransitionTo(TradePhase.ApproachingLane);
            }
        }

        private void UpdateApproachingLane(GameObject obj)
        {
            // Timeout check
            if (phaseTimer > APPROACH_TIMEOUT)
            {
                FLLog.Warning("AiTradeState", "Timeout approaching tradelane, skipping");
                AdvanceToNextLane(obj);
                return;
            }

            var segment = tradelaneRoute[currentLaneIndex];
            if (!ValidateGameObject(segment.EntryRing))
            {
                AdvanceToNextLane(obj);
                return;
            }

            // Initiate tradelane dock
            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                autopilot.StartDock(segment.EntryRing, GotoKind.GotoNoCruise);
                expectedExitPosition = segment.ExitPosition;
                TransitionTo(TradePhase.InTradeLane);
            }
        }

        private void UpdateInTradeLane(GameObject obj)
        {
            // Check if STradelaneMoveComponent still exists
            if (!obj.TryGetComponent<STradelaneMoveComponent>(out _))
            {
                // Exited tradelane - check if normal exit or disruption
                var actualPos = obj.WorldTransform.Position;
                var distToExpected = Vector3.Distance(actualPos, expectedExitPosition);

                if (distToExpected > DISRUPTION_THRESHOLD)
                {
                    // Disrupted mid-transit
                    FLLog.Warning("AiTradeState", $"Tradelane disrupted at distance {distToExpected:F0} from expected exit");
                    HandleDisruption(obj);
                }
                else
                {
                    // Normal exit
                    FLLog.Debug("AiTradeState", "Exited tradelane normally");
                    TransitionTo(TradePhase.ExitedTradelane);
                }
            }
        }

        private void UpdateExitedTradelane(GameObject obj)
        {
            currentLaneIndex++;

            if (currentLaneIndex < tradelaneRoute.Count)
            {
                // More tradelanes to use
                NavigateToTradelane(obj, tradelaneRoute[currentLaneIndex]);
                TransitionTo(TradePhase.SeekingTradeLane);
            }
            else
            {
                // Done with tradelanes, approach destination
                NavigateToBase(obj, destinationBase);
                TransitionTo(TradePhase.ApproachingDestination);
            }
        }

        private void UpdateApproachingDestination(GameObject obj)
        {
            // Timeout check
            if (phaseTimer > DESTINATION_TIMEOUT)
            {
                FLLog.Warning("AiTradeState", "Timeout approaching destination, aborting");
                TransitionTo(TradePhase.Complete);
                return;
            }

            var distance = Vector3.Distance(
                obj.WorldTransform.Position,
                destinationBase.WorldTransform.Position);

            if (distance < BASE_APPROACH_DIST)
            {
                InitiateDocking(obj);
            }
        }

        private void UpdateDocking(GameObject obj)
        {
            // Timeout check
            if (phaseTimer > DOCKING_TIMEOUT)
            {
                FLLog.Warning("AiTradeState", "Docking timeout, completing trade");
                TransitionTo(TradePhase.Complete);
            }
            // Docking completion is handled via OnDockComplete callback
        }

        private void UpdateTrading(GameObject obj, SNPCComponent ai, double dt)
        {
            tradingTimer += dt;

            if (tradingTimer >= TRADING_TIME)
            {
                FLLog.Debug("AiTradeState", "Trading complete");

                if (roundTrip && originBase != null && ValidateGameObject(originBase))
                {
                    // Start return trip
                    FLLog.Info("AiTradeState", "Starting return trip to origin");
                    StartReturnTrip(obj);
                }
                else
                {
                    TransitionTo(TradePhase.Complete);
                }
            }
        }

        #endregion

        #region Navigation Helpers

        private void NavigateToTradelane(GameObject obj, TradelaneSegment segment)
        {
            if (obj == null || segment?.EntryRing == null)
                return;

            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                var targetPos = segment.EntryRing.WorldTransform.Position;
                autopilot.GotoVec(targetPos, GotoKind.GotoNoCruise, TRADE_THROTTLE, 0);
                FLLog.Debug("AiTradeState", $"Navigating to tradelane entry: {segment.EntryRing.Nickname}");
            }
        }

        private void NavigateToBase(GameObject obj, GameObject targetBase)
        {
            if (obj == null || targetBase == null)
                return;

            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                var targetPos = targetBase.WorldTransform.Position;
                autopilot.GotoVec(targetPos, GotoKind.GotoCruise, TRADE_THROTTLE, 0);
                FLLog.Debug("AiTradeState", $"Navigating to base: {targetBase.Nickname}");
            }
        }

        private void InitiateDocking(GameObject obj)
        {
            if (obj.TryGetComponent<AutopilotComponent>(out var ap) &&
                destinationBase.TryGetComponent<SDockableComponent>(out var dock))
            {
                dock.StartDock(obj, 0);
                ap.StartDock(destinationBase, GotoKind.GotoNoCruise);
                TransitionTo(TradePhase.Docking);
                FLLog.Debug("AiTradeState", $"Initiating dock at {destinationBase.Nickname}");
            }
        }

        private void AdvanceToNextLane(GameObject obj)
        {
            currentLaneIndex++;
            if (currentLaneIndex < tradelaneRoute.Count)
            {
                NavigateToTradelane(obj, tradelaneRoute[currentLaneIndex]);
                TransitionTo(TradePhase.SeekingTradeLane);
            }
            else
            {
                NavigateToBase(obj, destinationBase);
                TransitionTo(TradePhase.ApproachingDestination);
            }
        }

        #endregion

        #region Route Building

        /// <summary>
        /// Build a route of tradelanes from current position to destination.
        /// Uses BFS to find shortest path through tradelane network.
        /// </summary>
        private void BuildTradelaneRoute(GameObject obj)
        {
            tradelaneRoute.Clear();
            currentLaneIndex = 0;

            if (obj?.World == null || destinationBase == null)
                return;

            var world = obj.World;
            var startPos = obj.WorldTransform.Position;
            var destPos = destinationBase.WorldTransform.Position;

            // Find all tradelane objects in the world
            var tradelanes = TradeRouteBuilder.FindTradelaneRings(world);
            if (tradelanes.Count == 0)
            {
                FLLog.Debug("AiTradeState", "No tradelanes found in system");
                return;
            }

            // Find nearest tradelane entry to start position
            var nearestEntry = FindNearestTradelane(tradelanes, startPos, out float entryDist);
            if (nearestEntry == null || entryDist > MAX_TRADELANE_SEARCH_DISTANCE)
            {
                FLLog.Debug("AiTradeState", "No tradelane near start position");
                return;
            }

            // Find nearest tradelane exit to destination
            var nearestExit = FindNearestTradelane(tradelanes, destPos, out float exitDist);
            if (nearestExit == null || exitDist > MAX_TRADELANE_SEARCH_DISTANCE)
            {
                FLLog.Debug("AiTradeState", "No tradelane near destination");
                return;
            }

            // Calculate if tradelane is worth using (20% faster threshold)
            float directDist = Vector3.Distance(startPos, destPos);

            float directTime = directDist / NORMAL_SPEED;
            float tlTime = (entryDist + exitDist) / NORMAL_SPEED; // Time to reach lanes
            // Assume tradelane covers remaining distance
            float laneTime = (directDist - entryDist - exitDist) / TRADELANE_SPEED;
            float totalTlTime = tlTime + Math.Max(0, laneTime);

            if (totalTlTime > directTime * TRADELANE_EFFICIENCY_THRESHOLD)
            {
                FLLog.Debug("AiTradeState", "Tradelane not significantly faster, flying direct");
                return;
            }

            // Build simple route (entry -> exit)
            // TODO: Implement full BFS for multi-segment routes
            var segment = new TradelaneSegment
            {
                EntryRing = nearestEntry,
                ExitRing = nearestExit,
                ExitPosition = nearestExit.WorldTransform.Position
            };
            tradelaneRoute.Add(segment);

            FLLog.Info("AiTradeState", $"Built tradelane route with {tradelaneRoute.Count} segment(s)");
        }

        private GameObject FindNearestTradelane(List<GameObject> tradelanes, Vector3 position, out float distance)
        {
            GameObject nearest = null;
            distance = float.MaxValue;

            foreach (var tl in tradelanes)
            {
                if (!ValidateGameObject(tl))
                    continue;

                float dist = Vector3.Distance(position, tl.WorldTransform.Position);
                if (dist < distance)
                {
                    distance = dist;
                    nearest = tl;
                }
            }

            return nearest;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called by SNPCComponent when docking completes.
        /// </summary>
        public void OnDockComplete()
        {
            if (phase == TradePhase.Docking)
            {
                FLLog.Debug("AiTradeState", "Dock complete, starting trading");
                tradingTimer = 0;
                TransitionTo(TradePhase.Trading);
            }
        }

        private void HandleCombatResume(GameObject obj)
        {
            var isInCombat = obj.TryGetComponent<SelectedTargetComponent>(out var targetComp) &&
                            targetComp.Selected != null;

            if (wasInCombat && !isInCombat)
            {
                FLLog.Debug("AiTradeState", "Combat ended, resuming trade route");
                ResumeNavigation(obj);
            }
            wasInCombat = isInCombat;

            // If in combat, let state graph handle movement
            if (isInCombat && AllowCombatInterruption)
                return;
        }

        private void ResumeNavigation(GameObject obj)
        {
            switch (phase)
            {
                case TradePhase.SeekingTradeLane:
                    if (currentLaneIndex < tradelaneRoute.Count)
                        NavigateToTradelane(obj, tradelaneRoute[currentLaneIndex]);
                    break;
                case TradePhase.ApproachingDestination:
                    NavigateToBase(obj, destinationBase);
                    break;
            }
        }

        private void HandleDisruption(GameObject obj)
        {
            FLLog.Info("AiTradeState", "Handling tradelane disruption, flying direct to destination");
            // Skip remaining tradelanes, fly direct
            tradelaneRoute.Clear();
            currentLaneIndex = 0;
            NavigateToBase(obj, destinationBase);
            TransitionTo(TradePhase.ApproachingDestination);
        }

        private void HandleDestinationLost(GameObject obj, SNPCComponent ai)
        {
            if (roundTrip && originBase != null && ValidateGameObject(originBase))
            {
                FLLog.Info("AiTradeState", "Destination lost, returning to origin");
                NavigateToBase(obj, originBase);
                TransitionTo(TradePhase.Complete);
            }
            else
            {
                FLLog.Info("AiTradeState", "Destination lost, switching to wander");
                ai.SetState(new AiWanderState(obj.WorldTransform.Position, 5000f));
            }
        }

        private void StartReturnTrip(GameObject obj)
        {
            // Swap origin and destination for return
            // Create new trade state with swapped endpoints
            if (obj.TryGetComponent<SNPCComponent>(out var ai))
            {
                ai.SetState(new AiTradeState(destinationBase, originBase, false));
            }
        }

        #endregion

        #region Utilities

        private void TransitionTo(TradePhase newPhase)
        {
            FLLog.Debug("AiTradeState", $"Phase transition: {phase} -> {newPhase}");
            phase = newPhase;
            phaseTimer = 0;
        }

        private bool ValidateDestination()
        {
            return destinationBase != null &&
                   (destinationBase.Flags & GameObjectFlags.Exists) != 0;
        }

        private bool ValidateGameObject(GameObject obj)
        {
            return obj != null &&
                   (obj.Flags & GameObjectFlags.Exists) != 0;
        }

        /// <summary>
        /// Get current phase for external monitoring.
        /// </summary>
        public TradePhase GetCurrentPhase() => phase;

        /// <summary>
        /// Get destination for external queries.
        /// </summary>
        public GameObject GetDestination() => destinationBase;

        public override string ToString()
        {
            return $"Trade({phase}, dest={destinationBase?.Nickname ?? "?"}, lane={currentLaneIndex}/{tradelaneRoute.Count})";
        }

        #endregion
    }
}
