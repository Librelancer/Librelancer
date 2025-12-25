// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Server.Ai.Trade
{
    /// <summary>
    /// Shared constants for trade route calculations.
    /// </summary>
    public static class TradeConstants
    {
        /// <summary>Normal ship cruise speed.</summary>
        public const float NORMAL_SPEED = 300f;

        /// <summary>Ship cruise speed.</summary>
        public const float CRUISE_SPEED = 600f;

        /// <summary>Tradelane transit speed.</summary>
        public const float TRADELANE_SPEED = 2500f;

        /// <summary>Maximum distance to search for a tradelane entry/exit point.</summary>
        public const float MAX_TRADELANE_SEARCH_DISTANCE = 50000f;

        /// <summary>
        /// Tradelane must be this much faster than direct flight to be used.
        /// 0.8 = tradelane route must be 20% faster.
        /// </summary>
        public const float TRADELANE_EFFICIENCY_THRESHOLD = 0.8f;
    }
}
