// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.GameData
{
	public class Archetype
    {
        public ResolvedModel ModelFile;
		//HACK: remove later
		public string ArchetypeName;
        public string LoadoutName;
        public Data.Solar.ArchetypeType Type;
		public List<DockSphere> DockSpheres = new List<DockSphere>();
		public float[] LODRanges;
        public Data.Solar.CollisionGroup[] CollisionGroups;
		public Archetype ()
		{
		}
	}
}

