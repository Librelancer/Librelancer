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