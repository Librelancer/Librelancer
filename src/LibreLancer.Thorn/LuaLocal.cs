// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thorn
{
	public class LuaLocal
	{
		public string Name;
		public int Line;
		public override string ToString ()
		{
			return Line + ": " + Name;
		}
		public string ToStringIndented()
		{
			return "\t" + ToString ();
		}
	}
}

