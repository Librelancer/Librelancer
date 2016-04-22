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
using System.Collections.Generic;
using System.IO;
using Jitter.LinearMath;

namespace LibreLancer.Sur
{
	public class Surface
	{
		const int SIZE = 48;
		public JVector Center;
		public JVector Inertia;
		public uint BitsEnd;
		public uint BitsStart;
		public float Radius;
		//FL-OS comment: some sort of multiplier for the radius
		public uint Scale; //TODO: Surface - What is this?
		public Surface (BinaryReader reader)
		{
			Center = new JVector (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
			Inertia = new JVector (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
			Radius = reader.ReadSingle ();
			Scale = reader.ReadByte ();
			BitsEnd = reader.ReadUInt24 ();
			BitsStart = reader.ReadUInt32 ();
			//FL-OS comment: padding.
			//TODO: Surface - Is this actually padding?
			reader.BaseStream.Seek (12, SeekOrigin.Current);

			long bStart = reader.BaseStream.Position + BitsStart - SIZE;
			long bEnd = reader.BaseStream.Position + BitsEnd - SIZE;

			bool done = false;
			do {
				TGroupHeader th = new TGroupHeader(reader);
				for(int i = 0; i < th.TriangleCount;i++) {
					var tri = new SurTriangle(reader);
				}
				done = (th.VertexArrayOffset == (TGroupHeader.SIZE + SurTriangle.SIZE * th.TriangleCount));
			} while (!done);

			while (reader.BaseStream.Position < bStart) {
				var vert = new SurVertex (reader);
			}
			while (reader.BaseStream.Position < bEnd) {
				var bh = new BitHeader (reader);
			}
		}
		//TODO: Sur - I don't know what this is either.
		private class BitHeader
		{
			public const int SIZE = 28;
			public JVector Centre;
			public byte[] Scale;
			public float Radius;
			public int OffsetToNextSibling;
			public int OffsetToTriangles;

			public BitHeader(BinaryReader reader)
			{
				OffsetToNextSibling = reader.ReadInt32();
				OffsetToTriangles = reader.ReadInt32();
				Centre = new JVector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
				Radius = reader.ReadSingle();
				Scale = reader.ReadBytes(3);
				//FL-OS Comment: padding
				reader.BaseStream.Seek(1, SeekOrigin.Current);
			}
		}
	}
}

