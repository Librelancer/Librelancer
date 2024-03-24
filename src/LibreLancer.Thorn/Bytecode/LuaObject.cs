// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Thorn.Bytecode
{
    class LuaObject
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

