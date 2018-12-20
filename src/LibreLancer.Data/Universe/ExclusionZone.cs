// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.Universe
{
	public class ExclusionZone
	{
		private StarSystem parent;

		private string exclusionName;
		private Zone exclusion;
		public Zone Exclusion
		{
			get
			{
				if (exclusion == null) exclusion = parent.FindZone(exclusionName);
				return exclusion;
			}
		}

		public float? FogFar { get; set; }
		public float? FogNear { get; set; }
		public float? ShellScalar { get; set; }
		public float? MaxAlpha { get; set; }
		public Color4? Color { get; set; }
		public Color3f? Tint { get; set; }
		public int? ExcludeBillboards { get; set; }
		public int? ExcludeDynamicAsteroids { get; set; }
		public float? EmptyCubeFrequency { get; set; }
		public int? BillboardCount { get; set; }
		public string ZoneShellPath { get; set; }

		public ExclusionZone(StarSystem parent, string exclusion)
		{
			this.parent = parent;
			this.exclusionName = exclusion;
		}
	}
}