using System;

namespace LibreLancer.Ini
{
	public class StringKeyValue : IValue
	{
		public string Key;
		public string Value;
		public StringKeyValue (string k, string v)
		{
			Key = k;
			Value = v;
		}

		public bool ToBoolean()
		{
			throw new NotImplementedException();
		}

		public int ToInt32()
		{
			throw new NotImplementedException();
		}

		public StringKeyValue ToKeyValue()
		{
			return this;
		}

		public float ToSingle()
		{
			throw new NotImplementedException();
		}
	}
}

