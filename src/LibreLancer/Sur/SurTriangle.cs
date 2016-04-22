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
	//TODO: SurTriangle - What does this stuff do?
	public class SurTriangle
	{
		public const int SIZE = 16;
		public Side[] Vertices = new Side[3];
		public uint TriNumber;
		public uint Flag;
		public uint TriOp;
		public uint Unknown; //FL-OS Comment: tested for zero (which they all are), but not used

		public SurTriangle (BinaryReader reader)
		{
			uint arg = reader.ReadUInt32 ();
			TriNumber = (arg >> 0) & 0xFFF;
			TriOp = (arg >> 12) & 0xFFF;
			Unknown = (arg >> 24) & 0x7F;
			Flag = arg >> 31;

			Vertices [0] = new Side (reader);
			Vertices [1] = new Side (reader);
			Vertices [2] = new Side (reader);
		}
	}
}

