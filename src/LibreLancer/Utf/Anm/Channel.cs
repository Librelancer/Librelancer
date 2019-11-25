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

        public FrameType InterpretedType { get; private set; }
        public QuaternionMethod QuaternionMethod { get; private set; }
		public Frame[] Frames { get; private set; }

        private const int BIT_NORM = 128;
        private const int BIT_FLOAT = 0x1;
        private const int BIT_VEC = 0x2;
        private const int BIT_QUAT = 0x4;
        
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
            QuaternionMethod = QuaternionMethod.Full;
            switch (ChannelType)
            {
                case BIT_NORM:
                    frameType = FrameType.Quaternion;
                    QuaternionMethod = QuaternionMethod.HalfAngle;
                    break;
                case BIT_VEC:
                    frameType = FrameType.Vector3;
                    break;
                case BIT_QUAT:
                    frameType = FrameType.Quaternion;
                    break;
                case BIT_VEC | BIT_QUAT:
                    frameType = FrameType.VecWithQuat;
                    break;
                case BIT_VEC | BIT_NORM:
                case BIT_VEC | 0x40:  //special case normal? unsure
                    frameType = FrameType.VecWithQuat;
                    QuaternionMethod = QuaternionMethod.HalfAngle;
                    break;
            }
            InterpretedType = frameType;
            Frames = new Frame[FrameCount];
			using (BinaryReader reader = new BinaryReader(new MemoryStream(frameBytes)))
			{
				for (int i = 0; i < FrameCount; i++)
                {
                    Frames[i] = new Frame(reader, Interval == -1, frameType, QuaternionMethod);
                }
			}
		}
	}
}
