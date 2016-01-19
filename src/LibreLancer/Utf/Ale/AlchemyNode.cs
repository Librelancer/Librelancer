using System;
using System.Collections.Generic;
namespace LibreLancer.Utf.Ale
{
	public class AlchemyNode
	{
		public string Name;
		public List<AleParameter> Parameters = new List<AleParameter>();
		public AlchemyNode ()
		{
		}
		public override string ToString ()
		{
			return Name;
		}
	}
}

