// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;

namespace LibreLancer.Data.GameData.World
{
	public class NebulaExclusionZone
	{
		public Zone Zone;
        //Shell
        public string ShellPath;
		public ResolvedModel Shell;
		public Color3f ShellTint;
		public float ShellMaxAlpha;
		public float ShellScalar;
		//Fog
		public float FogFar;

        public NebulaExclusionZone Clone(Dictionary<string, Zone> newZones)
        {
            var o = (NebulaExclusionZone)MemberwiseClone();
            o.Zone = Zone == null
                ? null
                : newZones.GetValueOrDefault(Zone.Nickname);
            return o;
        }
	}
}

