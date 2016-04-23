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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;

namespace LibreLancer.Thorn
{
	class LuaPrototype
	{
		public int LinesDefined;
		public string Source;
		public byte[] Code;
		public LuaLocal[] Locals;
		public LuaObject[] Constants;
		public string DescriptionText ()
		{
			return "Lines Defined: " + LinesDefined + "\n" +
			"Source: " + Source + "\n" +
			"Code Length: " + Code.Length + "\n" +
				"Locals: \n" + GetLocalsString () + "\n" +
				"Constants: \n" + GetConstantsString () + "\n";
		}
		string GetLocalsString()
		{
			if (Locals == null)
				return "\tNone";
			string s = "";
			for (int i = 0; i < Locals.Length; i++) {
				s += Locals [i].ToStringIndented () + "\n";
			}
			return s;
		}
		string GetConstantsString()
		{
			if (Constants == null)
				return "";
			string s = "";
			for (int i = 0; i < Constants.Length; i++) {
				s += Constants [i].ToStringIndented () + "\n";
			}
			return s;
		}
	}
}

