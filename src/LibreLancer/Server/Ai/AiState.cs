// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Server.Components;
using LibreLancer.World;

namespace LibreLancer.Server.Ai
{
    public abstract class AiState
    {
        /// <summary>
        /// Whether this state allows combat state graph transitions when hostiles are detected.
        /// When true, the NPC will pursue and engage enemies using the state graph (Face, Trail, Buzz).
        /// When false, the NPC will only shoot at enemies but continue its current behavior.
        /// Default: true (allows combat pursuit)
        /// </summary>
        public virtual bool AllowCombatInterruption => true;

        public abstract void OnStart(GameObject obj, SNPCComponent ai);
        public abstract void Update(GameObject obj, SNPCComponent ai, double dt);
    }
}