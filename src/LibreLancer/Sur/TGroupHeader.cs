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
namespace LibreLancer.Sur
{
	public struct TGroupHeader
	{
		public const int SIZE = 16;
		public uint MeshID;
		public uint RefVertsCount;
		public short TriangleCount;
		public uint Type;
		public uint VertexArrayOffset;

		public TGroupHeader (BinaryReader reader)
		{
			VertexArrayOffset = reader.ReadUInt32 ();
			MeshID = reader.ReadUInt32 ();
			Type = reader.ReadByte ();
			RefVertsCount = reader.ReadUInt24 ();
			TriangleCount = reader.ReadInt16 ();
			//FL-OS Comment: padding
			reader.BaseStream.Seek (2, SeekOrigin.Current);
		}
	}
}

