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

        public float[] Times { get; private set; }
        public Vector3[] Positions { get; private set; }
        public float[] Angles { get; private set; }
        public Quaternion[] Quaternions { get; private set; }

        public bool HasPosition => Positions != null;
        public bool HasOrientation => Quaternions != null;
        public bool HasAngle => Angles != null;
        

        int GetIndex(float time, out float t0, out float t1)
        {
            if (Times != null)
            {
                for (int i = 0; i < Times.Length - 1; i++)
                {
                    if (Times[i + 1] >= time)
                    {
                        t0 = Times[i];
                        t1 = Times[i + 1];
                        return i;
                    }
                }
                t0 = t1 = 0;
                return Times.Length - 1;
            }
            var idx = MathHelper.Clamp((int) Math.Floor(time / Interval), 0, FrameCount - 1);
            t0 = idx * Interval;
            t1 = (idx + 1) * Interval;
            return idx;
        }

        public float Duration
        {
            get
            {
                if (Times != null) return Times[Times.Length - 1];
                return Interval * FrameCount;
            }
        }
        
        public Vector3 PositionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return Positions[FrameCount - 1];
            var a = Positions[idx];
            var b = Positions[idx + 1];
            var blend = (time - t0) / (t1 - t0);
            return a + ((b - a) * blend);
        }

        public Quaternion QuaternionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return Quaternions[FrameCount - 1];
            var a = Quaternions[idx];
            var b = Quaternions[idx + 1];
            return Quaternion.Slerp(a, b, (time - t0) / (t1 - t0));
        }

        public float AngleAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return Angles[FrameCount - 1];
            var a = Angles[idx];
            var b = Angles[idx + 1];
            var dist = Math.Abs(b - a);
            if (Math.Abs(t1 - t0) < 0.5f && dist > 1f) return b;
            var blend = (time - t0) / (t1 - t0);
            return MathHelper.Lerp(a, b, blend);
        }
        
        private const int BIT_NORM = 128;
        private const int BIT_FLOAT = 0x1;
        private const int BIT_VEC = 0x2;
        private const int BIT_QUAT = 0x4;

        unsafe void ReadHeader(LeafNode node)
        {
            if (node.DataSegment.Count < 12) throw new Exception("Anm Header malformed");
            fixed (byte* bytes = node.DataSegment.Array)
            {
                FrameCount = *(int*) (&bytes[node.DataSegment.Offset]);
                Interval = *(float*) (&bytes[node.DataSegment.Offset + 4]);
                ChannelType = *(int*) (&bytes[node.DataSegment.Offset + 8]);
            }
        }
		public Channel(IntermediateNode root)
		{
            //Avoid allocating extra arrays
            ArraySegment<byte> data = new ArraySegment<byte>();
            //Fetch from nodes
			foreach (LeafNode channelSubNode in root)
			{
				switch (channelSubNode.Name.ToLowerInvariant())
				{
					case "header":
						ReadHeader(channelSubNode);
						break;
					case "frames":
						data = channelSubNode.DataSegment;
						break;
					default: throw new Exception("Invalid node in " + root.Name + ": " + channelSubNode.Name);
				}
			}
            FrameType frameType = FrameType.Float;
            QuaternionMethod = QuaternionMethod.Full;
            switch (ChannelType)
            {
                case BIT_NORM:
                case 0x50:
                case 0x40:
                    frameType = FrameType.Quaternion;
                    QuaternionMethod = QuaternionMethod.HalfAngle;
                    Quaternions = new Quaternion[FrameCount];
                    break;
                case BIT_VEC:
                case 0x22:
                    frameType = FrameType.Vector3;
                    Positions = new Vector3[FrameCount];
                    break;
                case BIT_QUAT:
                    frameType = FrameType.Quaternion;
                    Quaternions = new Quaternion[FrameCount];
                    break;
                case BIT_VEC | BIT_QUAT:
                    frameType = FrameType.VecWithQuat;
                    Positions = new Vector3[FrameCount];
                    Quaternions = new Quaternion[FrameCount];
                    break;
                case BIT_VEC | BIT_NORM:
                case BIT_VEC | 0x40:  //special case normal? unsure
                    frameType = FrameType.VecWithQuat;
                    QuaternionMethod = QuaternionMethod.HalfAngle;
                    Positions = new Vector3[FrameCount];
                    Quaternions = new Quaternion[FrameCount];
                    break;
                default:
                    Angles = new float[FrameCount];
                    break;
            }
            InterpretedType = frameType;
            if (Interval == -1) Times = new float[FrameCount];
			using (BinaryReader reader = new BinaryReader(data.GetReadStream()))
			{
				for (int i = 0; i < FrameCount; i++)
                {
                    if (Interval == -1) Times[i] = reader.ReadSingle();
                    switch (frameType)
                    {
                        case FrameType.Vector3:
                            Positions[i] = PosVec(reader);
                            break;
                        case FrameType.VecWithQuat:
                            Positions[i] = PosVec(reader);
                            Quaternions[i] = ReadQuaternion(reader, QuaternionMethod);
                            break;
                        case FrameType.Quaternion:
                            Quaternions[i] = ReadQuaternion(reader, QuaternionMethod);
                            break;
                        case FrameType.Float:
                            Angles[i] = reader.ReadSingle();
                            break;
                    }
                }
			}
		}

        static Vector3 PosVec(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        static Quaternion ReadQuaternion(BinaryReader reader, QuaternionMethod method)
        {
            if (method == QuaternionMethod.Full)
            {
                float w = reader.ReadSingle();
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                return new Quaternion(x,y,z,w);
            }
            else if (method == QuaternionMethod.HalfAngle)
            {
                var ha = new Vector3(
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f
                );
                return InvHalfAngle(ha);
            }
            else
                throw new InvalidOperationException();
        }

        static Quaternion InvHalfAngle(Vector3 p)
        {
            var d = Vector3.Dot(p, p);
            var s = (float) Math.Sqrt(2.0f - d);
            return new Quaternion(p * s, 1.0f - d);
        }
	}
}
