// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Ini
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class Entry : ICollection<IValue>
	{
		private const string BINI = "BINI";
		private const int N_LEN = 2, C_LEN = 1, T_LEN = 1;

		public string Name { get; private set; }

		private List<IValue> values;

        public string File = "BINI";
        public int Line = -1;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
		public Entry(BinaryReader reader, string stringBlock)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			if (stringBlock == null) throw new ArgumentNullException("stringBlock");

			short nameOffset = reader.ReadInt16();
			Name = stringBlock.Substring(nameOffset, stringBlock.IndexOf('\0', nameOffset) - nameOffset);

			byte count = reader.ReadByte();
			values = new List<IValue>(count);

			for (int i = 0; i < count; i++)
			{
				IniValueType valueType = (IniValueType)reader.ReadByte();
				switch (valueType)
				{
				case IniValueType.Boolean:
					values.Add(new BooleanValue(reader));
					break;
				case IniValueType.Int32:
					values.Add(new Int32Value(reader));
					break;
				case IniValueType.Single:
					values.Add(new SingleValue(reader));
					break;
				case IniValueType.String:
					values.Add(new StringValue(reader, stringBlock));
					break;
				default:
					throw new FileContentException(BINI, "Unknown BINI value type: " + valueType);
				}
			}
		}

		public Entry(string name, ICollection<IValue> values)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (values == null) throw new ArgumentNullException("values");

			this.Name = name;
			this.values = new List<IValue>(values);
		}

		public IValue this[int index]
		{
			get { return values[index]; }
			set { values[index] = value; }
		}

		public void Add(IValue item)
		{
			values.Add(item);
		}

		public void Clear()
		{
			values.Clear();
		}

		public bool Contains(IValue item)
		{
			return values.Contains(item);
		}

		public void CopyTo(IValue[] array, int arrayIndex)
		{
			values.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return values.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(IValue item)
		{
			return values.Remove(item);
		}

		public IEnumerator<IValue> GetEnumerator()
		{
			return values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return values.GetEnumerator();
		}

		/*public static bool operator ==(Entry operand1, Entry operand2)
        {
            return operand1.Equals(operand2);
        }

        public static bool operator !=(Entry operand1, Entry operand2)
        {
            return !(operand1 == operand2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Entry)
            {
                Entry e = (Entry)obj;
                return namePointer == e.namePointer && values == e.values;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return namePointer.GetHashCode() ^ values.GetHashCode();
        }*/

		public override string ToString()
		{
			string result = Name;
            if (values.Count > 0) result += " = ";
            for (int i = 0; i < values.Count; i++)
            {
                result += values[i];
                if (i < values.Count - 1) result += ", ";
            }
            return result;
		}
	}
}