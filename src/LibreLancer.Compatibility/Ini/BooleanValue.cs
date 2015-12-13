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