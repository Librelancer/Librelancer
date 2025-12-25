// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai
{
    /// <summary>
    /// AI state for NPCs that patrol between zones in a path.
    /// Zones with matching PathLabel[0] form a patrol route.
    /// </summary>
    /// <remarks>
    /// Patrol behavior:
    /// 1. NPC navigates to current waypoint zone's position
    /// 2. Upon arrival (within threshold), moves to next waypoint
    /// 3. After reaching last waypoint, reverses direction or loops
    /// 4. Combat: If hostile detected, state can be interrupted
    /// </remarks>
    public class AiPatrolState : AiState
    {
        private readonly List<Vector3> waypoints;
        private readonly List<string> zoneNicknames; // For debugging
        private int currentWaypointIndex;
        private bool reverseDirection; // For ping-pong patrol
        private double waitTimer;
        private PatrolMode mode;

        // Navigation constants
        private const float ARRIVAL_THRESHOLD = 500f;  // Distance to consider "arrived"
        private const float PATROL_THROTTLE = 0.8f;    // Normal patrol speed
        private const double WAYPOINT_WAIT_TIME = 3.0; // Pause at each waypoint

        /// <summary>
        /// Patrol navigation modes.
        /// </summary>
        public enum PatrolMode
        {
            /// <summary>Move forward through waypoints, then reverse.</summary>
            PingPong,
            /// <summary>Loop from last waypoint back to first.</summary>
            Loop,
            /// <summary>Stop at last waypoint and wander.</summary>
            OneWay
        }

        /// <summary>
        /// Create a patrol state with explicit waypoints.
        /// </summary>
        /// <param name="waypoints">Ordered list of patrol positions.</param>
        /// <param name="mode">How to handle reaching the end of the route.</param>
        public AiPatrolState(IEnumerable<Vector3> waypoints, PatrolMode mode = PatrolMode.Loop)
        {
            this.waypoints = waypoints?.ToList() ?? new List<Vector3>();
            this.zoneNicknames = new List<string>();
            this.mode = mode;
            this.currentWaypointIndex = 0;
            this.reverseDirection = false;
            this.waitTimer = 0;
        }

        /// <summary>
        /// Create a patrol state from zones matching a path label.
        /// </summary>
        /// <param name="allZones">All zones in the system.</param>
        /// <param name="pathLabel">Path label to filter zones (first element of PathLabel array).</param>
        /// <param name="mode">How to handle reaching the end of the route.</param>
        public AiPatrolState(IEnumerable<Zone> allZones, string pathLabel, PatrolMode mode = PatrolMode.Loop)
        {
            this.mode = mode;
            this.currentWaypointIndex = 0;
            this.reverseDirection = false;
            this.waitTimer = 0;
            this.waypoints = new List<Vector3>();
            this.zoneNicknames = new List<string>();

            if (allZones == null || string.IsNullOrEmpty(pathLabel))
                return;

            // Find all zones matching this path label and sort by index
            var patrolZones = allZones
                .Where(z => z.PathLabel != null &&
                           z.PathLabel.Length > 0 &&
                           z.PathLabel[0].Equals(pathLabel, StringComparison.OrdinalIgnoreCase))
                .OrderBy(z => GetPathIndex(z))
                .ToList();

            foreach (var zone in patrolZones)
            {
                waypoints.Add(zone.Position);
                zoneNicknames.Add(zone.Nickname);
            }

            FLLog.Info("AiPatrolState", $"Created patrol route '{pathLabel}' with {waypoints.Count} waypoints");
        }

        /// <summary>
        /// Extract path index from zone's PathLabel array.
        /// </summary>
        private static int GetPathIndex(Zone zone)
        {
            if (zone.PathLabel == null || zone.PathLabel.Length < 2)
                return 0;

            if (int.TryParse(zone.PathLabel[1], out int index))
                return index;

            return 0;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (waypoints.Count == 0)
            {
                FLLog.Warning("AiPatrolState", $"No waypoints defined for {obj?.Nickname ?? "null"}, patrol will not work");
                return;
            }

            FLLog.Info("AiPatrolState", $"Starting patrol for {obj?.Nickname ?? "null"} with {waypoints.Count} waypoints");

            // Start navigating to first waypoint
            NavigateToCurrentWaypoint(obj);
        }

        // Track if we need to resume navigation after combat
        private bool wasInCombat = false;

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            if (waypoints.Count == 0)
                return;

            // Check if we're in combat (have a hostile target)
            var isInCombat = obj.TryGetComponent<SelectedTargetComponent>(out var targetComp) &&
                            targetComp.Selected != null;

            // Resume patrol navigation after combat ends
            if (wasInCombat && !isInCombat)
            {
                FLLog.Debug("AiPatrolState", "Combat ended, resuming patrol");
                NavigateToCurrentWaypoint(obj);
            }
            wasInCombat = isInCombat;

            // If in combat, let state graph handle movement (don't update patrol)
            if (isInCombat)
                return;

            // Handle wait timer at waypoints
            if (waitTimer > 0)
            {
                waitTimer -= time;
                if (waitTimer <= 0)
                {
                    AdvanceWaypoint();
                    NavigateToCurrentWaypoint(obj);
                }
                return;
            }

            // Check distance to current waypoint
            var myPos = obj.WorldTransform.Position;
            var targetPos = waypoints[currentWaypointIndex];
            var distance = Vector3.Distance(myPos, targetPos);

            if (distance < ARRIVAL_THRESHOLD)
            {
                // Arrived at waypoint
                FLLog.Debug("AiPatrolState", $"Arrived at waypoint {currentWaypointIndex + 1}/{waypoints.Count}");

                // Check if this is the last waypoint
                bool isLastWaypoint = reverseDirection
                    ? currentWaypointIndex == 0
                    : currentWaypointIndex == waypoints.Count - 1;

                if (isLastWaypoint && mode == PatrolMode.OneWay)
                {
                    // End of patrol - switch to wander at final position
                    FLLog.Info("AiPatrolState", "Patrol complete, switching to wander");
                    ai.SetState(new AiWanderState(targetPos, 1000f));
                    return;
                }

                // Wait at waypoint before moving on
                waitTimer = WAYPOINT_WAIT_TIME;
            }
        }

        /// <summary>
        /// Move to the next waypoint based on patrol mode.
        /// </summary>
        private void AdvanceWaypoint()
        {
            if (waypoints.Count <= 1)
                return;

            if (mode == PatrolMode.Loop)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }
            else if (mode == PatrolMode.PingPong)
            {
                if (reverseDirection)
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex <= 0)
                    {
                        currentWaypointIndex = 0;
                        reverseDirection = false;
                    }
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count - 1)
                    {
                        currentWaypointIndex = waypoints.Count - 1;
                        reverseDirection = true;
                    }
                }
            }
            else // OneWay - this shouldn't be called for OneWay at end
            {
                if (currentWaypointIndex < waypoints.Count - 1)
                    currentWaypointIndex++;
            }

            FLLog.Debug("AiPatrolState", $"Advancing to waypoint {currentWaypointIndex + 1}/{waypoints.Count}");
        }

        /// <summary>
        /// Navigate the NPC to the current waypoint using autopilot.
        /// </summary>
        private void NavigateToCurrentWaypoint(GameObject obj)
        {
            if (obj == null || currentWaypointIndex >= waypoints.Count)
                return;

            var targetPos = waypoints[currentWaypointIndex];

            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                autopilot.GotoVec(targetPos, GotoKind.GotoNoCruise, PATROL_THROTTLE, 0);
                FLLog.Debug("AiPatrolState", $"Navigating to waypoint {currentWaypointIndex + 1}: {targetPos}");
            }
            else
            {
                FLLog.Warning("AiPatrolState", $"No AutopilotComponent on {obj.Nickname}");
            }
        }

        /// <summary>
        /// Get current waypoint position for external queries.
        /// </summary>
        public Vector3 GetCurrentWaypoint() =>
            currentWaypointIndex < waypoints.Count ? waypoints[currentWaypointIndex] : Vector3.Zero;

        /// <summary>
        /// Get patrol progress as fraction (0.0 to 1.0).
        /// </summary>
        public float GetProgress() =>
            waypoints.Count > 0 ? (float)currentWaypointIndex / waypoints.Count : 0f;

        public override string ToString()
        {
            var zoneName = currentWaypointIndex < zoneNicknames.Count
                ? zoneNicknames[currentWaypointIndex]
                : "?";
            return $"Patrol(waypoint={currentWaypointIndex + 1}/{waypoints.Count}, zone={zoneName}, mode={mode})";
        }
    }
}
