// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
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

        public bool TryToInt32(out int value)
        {
            value = 0;
            return false;
        }

        public long ToInt64()
        {
            throw new NotImplementedException();
        }

        public StringKeyValue ToKeyValue()
		{
			return this;
		}

		public float ToSingle(string propertyName = null)
		{
			throw new NotImplementedException();
		}
	}
}

