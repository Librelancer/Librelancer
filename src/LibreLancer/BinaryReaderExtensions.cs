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
using System.Text;
using System.IO;
namespace LibreLancer
{
	public static class BinaryReaderExtensions
	{
		static byte[] tagBuf = new byte[4];

		public static string ReadTag(this BinaryReader reader)
		{
			reader.BaseStream.Read (tagBuf, 0, 4);
			return Encoding.ASCII.GetString (tagBuf);
		}

		public static uint ReadUInt24(this BinaryReader reader)
		{
			return (uint)reader.ReadByte() + ((uint)reader.ReadByte() << 8) + ((uint)reader.ReadByte() << 16);
		}
	}
}

