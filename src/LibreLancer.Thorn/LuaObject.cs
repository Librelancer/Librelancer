using System;

namespace LibreLancer.Thorn
{
	public class LuaObject
	{
		public LuaTypes Type;
		public object Value;
		public override string ToString ()
		{
			return "[" + Type.ToString () + ": " + Value.ToString () + "]";
		}
		public string ToStringIndented()
		{
			return "\t" + ToString ();
		}
		public T Cast<T>()
		{
			if (typeof(T) == typeof(string) && Type != LuaTypes.String)
				throw new InvalidCastException ();
			if (typeof(T) == typeof(float) && Type != LuaTypes.Number)
				throw new InvalidCastException ();
			return (T)Value;
		}
	}
}

