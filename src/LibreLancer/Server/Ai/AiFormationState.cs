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
    /// AI state for NPCs that should fly in formation with a leader.
    /// Used for wing-based encounters and escort missions.
    /// </summary>
    /// <remarks>
    /// Formation behavior:
    /// 1. NPC joins the leader's formation at a specific offset position
    /// 2. AutopilotComponent handles actual formation flying
    /// 3. On combat: Can break formation to engage, then rejoin
    /// 4. If leader destroyed: Switches to wander behavior
    /// </remarks>
    public class AiFormationState : AiState
    {
        private readonly GameObject leader;
        private readonly Vector3 offset;
        private readonly Vector3 fallbackWanderCenter;
        private readonly float fallbackWanderRadius;

        // Track if we need to resume formation after combat
        private bool wasInCombat = false;
        private bool formationJoined = false;

        /// <summary>
        /// Check if the leader is still valid (exists and not destroyed).
        /// </summary>
        private bool IsLeaderValid => leader != null && (leader.Flags & GameObjectFlags.Exists) != 0;

        /// <summary>
        /// Create a formation state for an NPC to follow a leader.
        /// </summary>
        /// <param name="leader">The lead ship to follow.</param>
        /// <param name="offset">Position offset from leader (local space). Use Vector3.Zero for default.</param>
        /// <param name="fallbackCenter">Center point if leader is destroyed.</param>
        /// <param name="fallbackRadius">Wander radius if leader is destroyed.</param>
        public AiFormationState(GameObject leader, Vector3 offset = default, Vector3 fallbackCenter = default, float fallbackRadius = 2000f)
        {
            this.leader = leader;
            this.offset = offset;
            this.fallbackWanderCenter = fallbackCenter != default ? fallbackCenter : leader?.WorldTransform.Position ?? Vector3.Zero;
            this.fallbackWanderRadius = fallbackRadius;
        }

        public override void OnStart(GameObject obj, SNPCComponent ai)
        {
            if (!IsLeaderValid)
            {
                FLLog.Warning("AiFormationState", $"Leader is null or dead for {obj?.Nickname ?? "null"}, switching to wander");
                ai.SetState(new AiWanderState(fallbackWanderCenter, fallbackWanderRadius));
                return;
            }

            FLLog.Info("AiFormationState", $"Starting formation for {obj?.Nickname ?? "null"} following {leader.Nickname}");
            JoinFormation(obj);
        }

        private void JoinFormation(GameObject obj)
        {
            if (leader == null || obj == null)
                return;

            // Use FormationTools to properly join the formation
            FormationTools.EnterFormation(obj, leader, offset);
            formationJoined = true;

            FLLog.Debug("AiFormationState", $"{obj.Nickname} joined formation with {leader.Nickname} at offset {offset}");
        }

        public override void Update(GameObject obj, SNPCComponent ai, double time)
        {
            // Check if leader is still valid
            if (!IsLeaderValid)
            {
                FLLog.Info("AiFormationState", $"Leader destroyed, {obj?.Nickname ?? "null"} switching to wander");

                // Leave formation if we're in one
                if (obj.Formation != null)
                {
                    obj.Formation.Remove(obj);
                }

                ai.SetState(new AiWanderState(fallbackWanderCenter, fallbackWanderRadius));
                return;
            }

            // Check if we're in combat (have a hostile target)
            var isInCombat = obj.TryGetComponent<SelectedTargetComponent>(out var targetComp) &&
                            targetComp.Selected != null;

            // Resume formation after combat ends
            if (wasInCombat && !isInCombat)
            {
                FLLog.Debug("AiFormationState", "Combat ended, rejoining formation");

                // Re-enable formation flying
                if (obj.TryGetComponent<AutopilotComponent>(out var autopilot))
                {
                    autopilot.StartFormation();
                }
            }
            wasInCombat = isInCombat;

            // Ensure we're still in formation (may have been kicked during combat maneuvering)
            if (!isInCombat && obj.Formation == null && formationJoined)
            {
                FLLog.Debug("AiFormationState", "Lost formation membership, rejoining");
                JoinFormation(obj);
            }

            // If in combat, the state graph will handle movement (Face, Trail, Buzz)
            // Combat detection and firing is handled by SNPCComponent.Update() via GetHostileAndFire()
        }

        /// <summary>
        /// Whether this state allows combat pursuit via state graph.
        /// Formation members should break formation to engage enemies.
        /// </summary>
        public override bool AllowCombatInterruption => true;

        /// <summary>
        /// Get the leader being followed.
        /// </summary>
        public GameObject GetLeader() => leader;

        /// <summary>
        /// Check if currently in active formation with the leader.
        /// </summary>
        public bool IsInFormation(GameObject obj) =>
            obj?.Formation != null && leader != null && obj.Formation.LeadShip == leader;

        public override string ToString()
        {
            return $"Formation(leader={leader?.Nickname ?? "null"}, offset={offset})";
        }
    }
}
