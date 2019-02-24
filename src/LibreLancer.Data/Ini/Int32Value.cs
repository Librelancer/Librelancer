// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Ini
{
	public class Int32Value : IValue
	{
		private int value;

		public Int32Value(BinaryReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			value = reader.ReadInt32();
		}

		public Int32Value(int value)
		{
			this.value = value;
		}

		public static implicit operator int(Int32Value operand)
		{
			if (operand == null) return -1;
			else return operand.value;
		}

		public bool ToBoolean()
		{
			return value != 0;
		}

		public int ToInt32()
		{
			return value;
		}

        public long ToInt64()
        {
            return value;
        }

        public float ToSingle()
		{
			return value;
		}

		public override string ToString()
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}

		public StringKeyValue ToKeyValue()
		{
			throw new InvalidCastException ();
		}
	}
}