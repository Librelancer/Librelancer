// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Server.Components;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Ai
{
    /// <summary>
    /// AI state for NPCs that should wander within a defined area.
    /// Used by encounter-spawned NPCs to patrol their spawn zone.
    /// </summary>
    public class AiWanderState : AiState
    {
        private readonly Vector3 wanderCenter;
        private readonly float wanderRadius;

        /// <summary>
        /// Create a wander state for an NPC.
        /// </summary>
        /// <param name="center">Center point of the wander area (typically zone position).</param>
        /// <param name="radius">Radius of the wander area in meters.</param>
        public AiWanderState(Vector3 center, float radius)
        {
            this.wanderCenter = center;
            this.wanderRadius = radius;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            FLLog.Debug("AiWanderState", $"OnStart called for {obj?.Nickname ?? "null"} with center={wanderCenter}, radius={wanderRadius}");

            // Start the autopilot wander behavior
            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                FLLog.Debug("AiWanderState", $"Starting wander behavior: {autopilot.CurrentBehavior}");
                autopilot.StartWander(wanderCenter, wanderRadius);
            }
            else
            {
                FLLog.Warning("AiWanderState", $"No AutopilotComponent found on {obj?.Nickname ?? "null"} - NPC cannot wander!");
            }
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            // After combat ends (state graph no longer controlling movement),
            // ensure wander behavior is resumed if autopilot behavior changed
            if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
            {
                // If not wandering anymore (e.g., state graph set it to Goto during combat),
                // restart wander behavior
                if (autopilot.CurrentBehavior != AutopilotBehaviors.Wander &&
                    autopilot.CurrentBehavior != AutopilotBehaviors.None)
                {
                    // Check if we're no longer in combat (no hostile target)
                    // This happens when combat ends and we should resume wander
                    var hasTarget = obj.TryGetComponent<SelectedTargetComponent>(out var targetComp) &&
                                   targetComp.Selected != null;

                    if (!hasTarget)
                    {
                        FLLog.Debug("AiWanderState", "Combat ended, resuming wander");
                        autopilot.StartWander(wanderCenter, wanderRadius);
                    }
                }
            }

            // Note: Combat detection and firing is handled by SNPCComponent.Update()
            // via GetHostileAndFire(). When AllowCombatInterruption is true (default),
            // the state graph will handle pursuit/evasion movements.
        }

        public override string ToString()
        {
            return $"Wander(center={wanderCenter}, radius={wanderRadius})";
        }
    }
}
