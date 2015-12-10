/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure by Bas Westerbaan (http://blog.w-nz.com/uploads/bini.pdf)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.IO;

namespace LibreLancer.Ini
{
	public class StringValue : IValue
	{
		private int valuePointer;
		private string value;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
		public StringValue(BinaryReader reader, string stringBlock)
		{
			if (reader == null) throw new ArgumentNullException("reader");
			if (stringBlock == null) throw new ArgumentNullException("stringBlock");

			this.valuePointer = reader.ReadInt32();
			this.value = stringBlock.Substring((int)valuePointer, stringBlock.IndexOf('\0', (int)valuePointer) - (int)valuePointer);
		}

		public StringValue(string value)
		{
			if (value == null) throw new ArgumentNullException("value");
			this.valuePointer = -1;
			this.value = value;
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

		public int ToInt32()
		{
			int result;
			if (int.TryParse(value, out result)) return result;
			else return -1;
		}

		public float ToSingle()
		{
			float result;
			if (float.TryParse(value, out result)) return result;
			else return float.NaN;
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