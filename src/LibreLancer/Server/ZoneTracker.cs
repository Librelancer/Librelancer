// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data.GameData.World;

namespace LibreLancer.Server
{
    /// <summary>
    /// Tracks player positions relative to zones for encounter spawning.
    /// Provides spatial queries using Zone.ContainsPoint() for determining
    /// which zones a position falls within.
    /// </summary>
    /// <remarks>
    /// This is a lightweight, stateless utility class used by EncounterManager
    /// to determine which zones should spawn NPCs near players.
    /// </remarks>
    public class ZoneTracker
    {
        private readonly StarSystem system;

        /// <summary>
        /// Creates a new ZoneTracker for the given star system.
        /// </summary>
        /// <param name="system">The star system containing zones to track.</param>
        public ZoneTracker(StarSystem system)
        {
            this.system = system;
        }

        /// <summary>
        /// Get all zones containing the given position.
        /// </summary>
        /// <param name="position">World-space position to check.</param>
        /// <returns>Enumerable of zones that contain the position.</returns>
        public IEnumerable<Zone> GetZonesAtPosition(Vector3 position)
        {
            if (system?.Zones == null)
                yield break;

            foreach (var zone in system.Zones)
            {
                if (zone == null)
                    continue;

                // Wrap in try-catch to handle zones with invalid/uninitialized shapes
                bool contains = false;
                try
                {
                    contains = zone.ContainsPoint(position);
                }
                catch (Exception ex)
                {
                    FLLog.Warning("ZoneTracker", $"Error checking zone {zone.Nickname}: {ex.Message}");
                    continue;
                }

                if (contains)
                    yield return zone;
            }
        }

        /// <summary>
        /// Get only zones that have encounter definitions at the given position.
        /// </summary>
        /// <param name="position">World-space position to check.</param>
        /// <returns>Enumerable of encounter zones containing the position.</returns>
        public IEnumerable<Zone> GetEncounterZones(Vector3 position)
        {
            return GetZonesAtPosition(position)
                .Where(z => z.Encounters != null && z.Encounters.Length > 0);
        }

        /// <summary>
        /// Check if position is in any encounter zone.
        /// </summary>
        /// <param name="position">World-space position to check.</param>
        /// <param name="zone">Output: First encounter zone found, or null.</param>
        /// <returns>True if position is in an encounter zone.</returns>
        public bool IsInEncounterZone(Vector3 position, out Zone zone)
        {
            zone = GetEncounterZones(position).FirstOrDefault();
            return zone != null;
        }

        /// <summary>
        /// Get the star system this tracker is monitoring.
        /// </summary>
        public StarSystem System => system;
    }
}
