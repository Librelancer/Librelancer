using System;

namespace LibreLancer.Ini
{
	public class MapValue : IValue
	{
		public StringKeyValue Value;

		public MapValue (string k, string v)
		{
			Value = new StringKeyValue (k, v);
		}

		public bool ToBoolean ()
		{
			throw new InvalidCastException ();
		}

		public int ToInt32 ()
		{
			throw new InvalidCastException ();
		}

		public float ToSingle ()
		{
			throw new InvalidCastException ();
		}

		public StringKeyValue ToKeyValue()
		{
			return Value;
		}
	}
}

