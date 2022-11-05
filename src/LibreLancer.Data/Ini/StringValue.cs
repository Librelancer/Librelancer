// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Ini
{
	public class StringValue : IValue
	{
		private string value;
        private string section;
        private string file;
        private int line;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
		public StringValue(BinaryReader reader, BiniStringBlock stringBlock, string section, string file, int line)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			if (stringBlock == null) throw new ArgumentNullException("stringBlock");

            this.value = stringBlock.Get(reader.ReadInt32());
            this.section = section;
            this.file = file;
            this.line = line;
        }

		public StringValue(string value, string section, string file, int line)
		{
			if (value == null) throw new ArgumentNullException("value");
			this.value = value;
            this.section = section;
            this.file = file;
            this.line = line;
        }

		public static implicit operator string(StringValue operand)
		{
			if (operand == null) return null;
			else return operand.value;
		}

		public bool ToBoolean()
		{
			bool result;
			if (bool.TryParse(value, out result)) return result;
			else return !string.IsNullOrEmpty(value);
		}

        public bool TryToInt32(out int result)
        {
            if (int.TryParse(value, out result)) 
                return true;
            if (uint.TryParse(value, out var result2))
            {
                result = unchecked((int) result2);
                return true;
            }
            result = 0;
            return false;
        }

		public int ToInt32()
		{
			int result;
            uint result2;
			if (int.TryParse(value, out result)) return result;
			else if (uint.TryParse(value, out result2)) return (int) result2;
            else return -1;
		}

        public long ToInt64()
        {
            long result;
            if (long.TryParse(value, out result)) return result;
            else return -1;
        }

        public float ToSingle(string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
			float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return result;
            else
            {
                var lineInfo = line >= 0 ? ":" + line : " (line not available)";
                var nameInfo = string.IsNullOrWhiteSpace(propertyName) ? "" : $" for {propertyName}";
                FLLog.Error("Ini", 
                    $"Failed to parse float '{value}'{nameInfo} in section {section}: {file}{lineInfo}");
                return 0;
            }
        }

		public override string ToString()
		{
			return value;
		}
		public StringKeyValue ToKeyValue()
		{
			throw new InvalidCastException ();
		}
	}
}