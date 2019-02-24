// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Ini
{
	public class SingleValue : IValue
	{
		private float value;
		private long? longvalue;
		public SingleValue(BinaryReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			value = reader.ReadSingle();
		}

		public SingleValue(float value, long? templong)
		{
			longvalue = templong;
			this.value = value;
		}

		public static implicit operator float(SingleValue operand)
		{
			if (operand == null) return float.NaN;
			else return operand.value;
		}

		public bool ToBoolean()
		{
			return value != 0;
		}

		public int ToInt32()
		{
			if (longvalue != null)
			{
				return unchecked((int)longvalue.Value);
			}
			return (int)value;
		}

        public long ToInt64()
        {
            if (longvalue != null) return longvalue.Value;
            return (int)value;
        }

        public float ToSingle()
		{
			return value;
		}

		public StringKeyValue ToKeyValue()
		{
			throw new InvalidCastException ();
		}

		public override string ToString()
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
	}
}