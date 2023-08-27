// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
// Based on Starchart code - Copyright (c) Malte Rupprecht

using System;
using System.Diagnostics;
using System.Numerics;

namespace LibreLancer.Utf.Anm
{
	public class Channel
    {
        private const uint OFFSET_MASK = 0x0FFFFFFF;
        private const uint TYPE_MASK = 0xF0000000;
        
        private const uint TYPE_QUATERNION = (1U << 28);
        private const uint TYPE_0x40 = (2U << 28);
        private const uint TYPE_0x80 = (3U << 28);
        private const uint TYPE_IDENTITY = (4U << 28);

        private const uint TYPE_VECEMPTY = (5U << 28);
        private const uint TYPE_VEC3 = (6U << 28);

        private const uint TYPE_FLOAT = (7U << 28);
        
        private byte[] channelData;
        private uint stride;

        private uint quaternions;
        private uint vectors;
        private uint angles;
        
        public int FrameCount { get; private set; }
		public float Interval { get; private set; }
		public int ChannelType { get; private set; }

        public FrameType InterpretedType
        {
            get
            {
                if (quaternions != 0 && vectors != 0)
                    return FrameType.VecWithQuat;
                else if (quaternions != 0)
                    return FrameType.Quaternion;
                else if (vectors != 0)
                    return FrameType.Vector3;
                else
                    return FrameType.Float;
            }
        }

        public QuaternionMethod QuaternionMethod => (quaternions & TYPE_MASK) switch
        {
            TYPE_IDENTITY => QuaternionMethod.Empty,
            TYPE_QUATERNION => QuaternionMethod.Full,
            TYPE_0x40 => QuaternionMethod.Compressed0x40,
            TYPE_0x80 => QuaternionMethod.Compressed0x80,
            _ => QuaternionMethod.None
        };

        public bool HasPosition => vectors != 0;
        public bool HasOrientation => quaternions != 0;
        public bool HasAngle => angles != 0;

        public unsafe float GetTime(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if (Interval >= 0)
                return 0;
            var offset = (stride * index);
            fixed (byte* ptr = channelData)
                return *(float*) (&ptr[offset]);
        }

        public unsafe float GetAngle(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((angles & TYPE_MASK) != TYPE_FLOAT)
                return 0;
            var offset = (stride * index) + (angles & OFFSET_MASK);
            fixed (byte* ptr = channelData)
                return *(float*) (&ptr[offset]);
        }
        
        public unsafe Vector3 GetPosition(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((vectors & TYPE_MASK) != TYPE_VEC3)
                return Vector3.Zero;
            var offset = (stride * index) + (vectors & OFFSET_MASK);
            fixed (byte* ptr = channelData)
                return *(Vector3*) (&ptr[offset]);
        }

        public Quaternion GetQuaternion(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            var offset = (int) ((stride * index) + (quaternions & OFFSET_MASK));
            switch (quaternions & TYPE_MASK)
            {
                case TYPE_QUATERNION:
                    return GetFullQuat(offset);
                case TYPE_0x80:
                    return GetQuat0x80(offset);
                case TYPE_0x40:
                    return GetQuat0x40(offset);
                case TYPE_IDENTITY:
                case 0:
                    return Quaternion.Identity;
                default:
                    throw new InvalidOperationException();
            }
        }

        unsafe Quaternion GetFullQuat(int offset)
        {
            fixed (byte* ptr = channelData)
            {
                float* flt = (float*) (&ptr[offset]);
                return new Quaternion(flt[1], flt[2], flt[3], flt[0]);
            }
        }

        unsafe Quaternion GetQuat0x40(int offset)
        {
            fixed (byte* ptr = channelData)
            {
                short* sh = (short*) (&ptr[offset]);
                var ha = new Vector3(
                    sh[0] / 32767f,
                    sh[1] / 32767f,
                    sh[2] / 32767f
                );
                var len = ha.LengthSquared();
                var w = 0f;
                if (len < 1.0f)
                {
                    w = MathF.Sqrt(1 - len);
                }
                return new Quaternion(ha, w);
            }
        }

        unsafe Quaternion GetQuat0x80(int offset)
        {
            fixed (byte* ptr = channelData)
            {
                short* sh = (short*) (&ptr[offset]);
                var ha = new Vector3(
                    sh[0] / 32767f,
                    sh[1] / 32767f,
                    sh[2] / 32767f
                );
                var s = Vector3.Dot(ha, ha);
                if (s <= 0)
                    return Quaternion.Identity;
                var length = ha.Length();
                var something = MathF.Sin(MathF.PI * length * 0.5f);
                return new Quaternion(
                    ha * (something / length),
                    MathF.Sqrt(1f - something * something)
                );
            }
        }
        
        int GetIndex(float time, out float t0, out float t1)
        {
            if (Interval < 0)
            {
                for (int i = 0; i < FrameCount - 1; i++)
                {
                    if (GetTime(i + 1) >= time)
                    {
                        t0 = GetTime(i);
                        t1 = GetTime(i + 1);
                        return i;
                    }
                }
                t0 = t1 = 0;
                return FrameCount - 1;
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
                if (Interval < 0) return GetTime(FrameCount - 1);
                return Math.Max(Interval * (FrameCount - 1), 0);
            }
        }
        
        public Vector3 PositionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return GetPosition(FrameCount - 1);
            var a = GetPosition(idx);
            var b = GetPosition(idx + 1);
            var blend = (time - t0) / (t1 - t0);
            return a + ((b - a) * blend);
        }

        public Quaternion QuaternionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return GetQuaternion(FrameCount - 1);
            var a = GetQuaternion(idx);
            var b = GetQuaternion(idx + 1);
            return Quaternion.Slerp(a, b, (time - t0) / (t1 - t0));
        }

        public float AngleAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return GetAngle(FrameCount - 1);
            var a = GetAngle(idx);
            var b = GetAngle(idx + 1);
            var dist = Math.Abs(b - a);
            if (Math.Abs(t1 - t0) < 0.5f && dist > 1f) return b;
            var blend = (time - t0) / (t1 - t0);
            return MathHelper.Lerp(a, b, blend);
        }
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
            //Fetch from nodes
            byte[] cdata = null;
			foreach (LeafNode channelSubNode in root)
			{
                if(channelSubNode.Name.Equals("header", StringComparison.OrdinalIgnoreCase))
                    ReadHeader(channelSubNode);
                else if (channelSubNode.Name.Equals("frames", StringComparison.OrdinalIgnoreCase))
                {
                    cdata = channelSubNode.ByteArrayData;
                }
            }
            /* Pad data to avoid ARM data alignment errors */
            if((ChannelType & 0x80) == 0x80 ||
               (ChannelType & 0x40) == 0x40) {
                int startStride = 0;
                if(Interval < 0) startStride += 4;
                if((ChannelType & 0x1) == 0x1) startStride += 4;
                if((ChannelType & 0x2) == 0x2) startStride += 12;
                int fullStride = startStride + 8;
                int compStride = startStride + 6;
                channelData = new byte[fullStride * FrameCount];
                for(int i = 0; i < FrameCount; i++) {
                    int src = compStride * i;
                    int dst = fullStride * i;
                    for(int j = 0; j < compStride; j++) {
                        channelData[dst + j] = cdata[src + j];
                    }
                }
            } else {
                channelData = cdata;
            }
            
            if(((ChannelType & 0x2) == 0x2) &&
               ((ChannelType & 0x10) == 0x10))
                throw new Exception("Channel has invalid vector specification");
            if (Interval < 0) {
                stride += 4;
            }
            if ((ChannelType & 0x1) == 0x1)
            {
                angles = TYPE_FLOAT | stride;
                stride += 4;
            }
            if ((ChannelType & 0x2) == 0x2)
            {
                vectors = TYPE_VEC3 | stride;
                stride += 12;
            }
            if ((ChannelType & 0x10) == 0x10)
            {
                vectors = TYPE_VECEMPTY;
            }
            if ((ChannelType & 0x40) == 0x40)
            {
                quaternions = TYPE_0x40 | stride;
                stride += 8;
            }
            if ((ChannelType & 0x80) == 0x80)
            {
                quaternions = TYPE_0x80 | stride;
                stride += 8;
            }
            if ((ChannelType & 0x4) == 0x4)
            {
                quaternions = TYPE_QUATERNION | stride;
                stride += 16;
            }
            if ((ChannelType & 0x20) == 0x20)
            {
                quaternions = TYPE_IDENTITY;
            }
        }
    }
}
