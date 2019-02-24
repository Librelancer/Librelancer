// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Ini
{
	public class BooleanValue : IValue
	{
		private bool value;

		public BooleanValue(BinaryReader reader)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			value = reader.ReadBoolean();
		}

		public BooleanValue(bool value)
		{
			this.value = value;
		}

		public static implicit operator bool(BooleanValue operand)
		{
			if (operand == null) return false;
			else return operand.value;
		}

		public bool ToBoolean()
		{
			return value;
		}

		public int ToInt32()
		{
			return value ? 1 : 0;
		}

        public long ToInt64()
        {
            return value ? 1 : 0;
        }

        public float ToSingle()
		{
			return value ? 1 : 0;
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