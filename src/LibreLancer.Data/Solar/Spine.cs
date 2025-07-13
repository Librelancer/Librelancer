// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Solar
{
	public class Spine
	{
		//FORMAT: LengthScale, WidthScale, [Inner: r, g, b], [Outer: r, g, b], Alpha

		public float LengthScale { get; set; }
		public float WidthScale { get; set; }
		public Color3f InnerColor { get; set; }
		public Color3f OuterColor { get; set; }
		public float Alpha { get; set; }

		public Spine() { }

		public Spine(Entry e)
		{
			LengthScale = e[0].ToSingle();
			WidthScale = e[1].ToSingle();
			InnerColor = new Color3f(e[2].ToSingle(), e[3].ToSingle(), e[4].ToSingle());
			OuterColor = new Color3f(e[5].ToSingle(), e[6].ToSingle(), e[7].ToSingle());
			Alpha = e[8].ToSingle();
		}
	}
}

