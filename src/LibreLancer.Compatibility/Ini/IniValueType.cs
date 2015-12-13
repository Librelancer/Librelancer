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

namespace LibreLancer.Ini
{
	/// <summary>
	/// Possible types of data stored in a BINI value field
	/// </summary>
	enum IniValueType : byte
	{
		/// <summary>
		/// Boolean value
		/// </summary>
		Boolean = 0x00,

		/// <summary>
		/// 32bit signed integer value
		/// </summary>
		Int32 = 0x01,

		/// <summary>
		/// 32bit single precision floating point value
		/// </summary>
		Single = 0x02,

		/// <summary>
		/// 32bit unsigned integer as string table pointer
		/// </summary>
		String = 0x03
	}
}