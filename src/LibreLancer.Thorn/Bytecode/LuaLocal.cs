// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Thorn.Bytecode
{
    class LuaLocal
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

