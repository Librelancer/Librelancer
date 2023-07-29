// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreLancer.GameData.World
{
	public class ExclusionZone
	{
		public Zone Zone;
        //Shell
        public string ShellPath;
		public ResolvedModel Shell;
        public RigidModel ShellModel; //HACK: This should be loaded in the renderer
		public Color3f ShellTint;
		public float ShellMaxAlpha;
		public float ShellScalar;
		//Fog
		public float FogFar;

        public ExclusionZone Clone(Dictionary<string, Zone> newZones)
        {
            var o = (ExclusionZone)MemberwiseClone();
            o.Zone = Zone == null
                ? null
                : newZones.GetValueOrDefault(Zone.Nickname);
            return o;
        }
	}
}

