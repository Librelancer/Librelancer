using System;
namespace LancerEdit
{
	public struct MissingReference
	{
		public string Missing;
		public string Reference;
		public MissingReference(string m, string r)
		{
			Missing = m;
			Reference = r;
		}
	}
}
