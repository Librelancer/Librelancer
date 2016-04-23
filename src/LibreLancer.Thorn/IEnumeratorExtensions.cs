using System;
using System.Collections.Generic;
namespace LibreLancer.Thorn
{
	static class IEnumeratorExtensions
	{
		public static void MustMoveNext(this IEnumerator<object> e)
		{
			if (!e.MoveNext ())
				throw new Exception ("Unexpected EOF");
		}
		public static void AssertChar(this IEnumerator<object> e, char c)
		{
			if (!(e.Current is char) || ((char)e.Current) != c)
				throw new Exception (string.Format ("Expected '{0}', got '{1}'", c, e.Current));
			e.MustMoveNext ();
		}
	}
}

