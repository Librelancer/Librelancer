// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


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

		public Channel(IntermediateNode root)
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

            FrameType frameType = FrameType.Float;
            if((ChannelType & 0xC0) == 0xC0) frameType = FrameType.IK;
            Frames = new Frame[FrameCount];
			using (BinaryReader reader = new BinaryReader(new MemoryStream(frameBytes)))
			{
				for (int i = 0; i < FrameCount; i++)
				{
					Frames[i] = new Frame(reader, Interval == -1, frameType);
				}
			}
		}
	}
}
