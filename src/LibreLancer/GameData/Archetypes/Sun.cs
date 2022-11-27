// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Solar;
using Spine = LibreLancer.GameData.World.Spine;

namespace LibreLancer.GameData.Archetypes
{
	public class Sun : Archetype
	{
		public float Radius;
		public string GlowSprite;
		public Color3f GlowColorInner;
		public Color3f GlowColorOuter;
		public float GlowScale;
		public string CenterSprite;
		public Color3f CenterColorInner;
		public Color3f CenterColorOuter;
		public float CenterScale;
		public string SpinesSprite;
		public float SpinesScale;
		public List<Spine> Spines;

        public Sun ()
        {
            Type = ArchetypeType.sun;
        }
	}
}

