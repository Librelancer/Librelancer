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

