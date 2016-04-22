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
using System.IO;
namespace LibreLancer.ImageLib
{
	static class BinaryReaderExtensions
	{
		public static void Skip(this BinaryReader reader, int bytes)
		{
			reader.BaseStream.Seek (bytes, SeekOrigin.Current);
		}
			
		public static int ReadInt32BE(this BinaryReader reader)
		{
			var bytes = reader.ReadBytes (4);
			if (BitConverter.IsLittleEndian) {
				int x = (bytes [0] << 24) | (bytes [1] << 16) | (bytes [2] << 8) | bytes [3];
				return x;
			} else {
				return BitConverter.ToInt32 (bytes, 0);
			}
		}
	}
}

