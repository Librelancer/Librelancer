using System;

namespace LibreLancer.Utf.Ale
{
	public class AleParameter
	{
		public string Name;
		public object Value;
		public AleParameter ()
		{
		}
		public override string ToString ()
		{
			return string.Format ("[{0}: {1}]", Name, Value);
		}
	}
}

