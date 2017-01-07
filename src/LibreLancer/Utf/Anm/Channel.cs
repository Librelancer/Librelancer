/* The contents of this file a
 * re subject to the Mozilla Public License
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
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;


namespace LibreLancer.Utf.Anm
{
	public class Channel
	{
		public int FrameCount { get; private set; }
		public float Interval { get; private set; }
		public int ChannelType { get; private set; }

		public Frame[] Frames { get; private set; }

		public Channel(IntermediateNode root, bool objectmap)
		{
			byte[] frameBytes = new byte[0];

			foreach (LeafNode channelSubNode in root)
			{
				switch (channelSubNode.Name.ToLowerInvariant())
				{
					case "header":
						using (BinaryReader reader = new BinaryReader(new MemoryStream(channelSubNode.ByteArrayData)))
						{
							FrameCount = reader.ReadInt32();
							Interval = reader.ReadSingle();
							ChannelType = reader.ReadInt32();
						}
						break;
					case "frames":
						frameBytes = channelSubNode.ByteArrayData;
						break;
					default: throw new Exception("Invalid node in " + root.Name + ": " + channelSubNode.Name);
				}
			}

			Frames = new Frame[FrameCount];
			using (BinaryReader reader = new BinaryReader(new MemoryStream(frameBytes)))
			{
				for (int i = 0; i < FrameCount; i++)
				{
					Frames[i] = new Frame(reader, Interval == -1, objectmap);
				}
			}
		}
	}
}
