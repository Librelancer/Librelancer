// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LibreLancer.Ini
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class Entry : ICollection<IValue>
	{
		public string Name { get; private set; }

		private List<IValue> values;

        public string File = "[Null]";
        public int Line = -1;

		public Entry(string name, ICollection<IValue> values)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (values == null) throw new ArgumentNullException(nameof(values));

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
            StringBuilder sb = new(Name);
            if (values.Count > 0) sb.Append(" = ");
            for (int i = 0; i < values.Count; i++)
            {
                sb.Append(values[i]);
                if (i < values.Count - 1)
                    sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
