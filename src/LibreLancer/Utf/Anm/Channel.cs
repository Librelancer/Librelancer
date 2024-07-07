// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
// Based on Starchart code - Copyright (c) Malte Rupprecht

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace LibreLancer.Utf.Anm
{
	public struct Channel
    {
        //Store some pre-calculated values in here for easier switch statements &
        //Size+offset mappings

        //Bits 0-4 - stride
        private const uint STRIDE_MASK = 0x0000003F;
        //Bits 5-9 - vec3 offset, Bits 10-11 - vec3 type
        private const uint VEC_OFFSET_MASK = 0x000003E0; // >> 5
        private const uint VEC_TYPE_MASK = 0x00000C00;
        private const uint TYPE_VEC3 = 0x00000400;
        private const uint TYPE_VECEMPTY = 0x00000800;
        //Bits 12 - 16 - quat offset, Bits 17 - 19 - quat type
        private const uint QUAT_OFFSET_MASK = 0x0001F000; // >> 12
        private const uint QUAT_TYPE_MASK = 0x00060000;
        private const uint QUAT_TYPE_FULL = 0x00020000;
        private const uint QUAT_TYPE_0x40 = 0x00040000;
        private const uint QUAT_TYPE_0x80 = 0x00060000;
        private const uint QUAT_TYPE_IDENTITY = 0x00080000;
        //Bit 20 - are there angles?
        private const uint ANGLES = 0x00100000;

        private uint header = 0;
        private int startIdx = 0;

        private AnmBuffer buffer;

        public int FrameCount { get; private set; }
		public float Interval { get; private set; }

        public int ChannelType
        {
            get
            {
                int originalType = 0;
                originalType |= ((header & QUAT_TYPE_MASK) switch
                {
                    QUAT_TYPE_IDENTITY => 0x20,
                    QUAT_TYPE_FULL => 0x4,
                    QUAT_TYPE_0x40 => 0x40,
                    QUAT_TYPE_0x80 => 0x80,
                    _ => 0
                });
                originalType |= ((header & VEC_TYPE_MASK)) switch
                {
                    TYPE_VEC3 => 0x2,
                    TYPE_VECEMPTY => 0x10,
                    _ => 0
                };
                if ((header & ANGLES) != 0)
                    originalType |= 0x1;
                return originalType;
            }
        }

        public FrameType InterpretedType
        {
            get
            {
                if ((header & QUAT_TYPE_MASK) != 0 && (header & VEC_TYPE_MASK) != 0)
                    return FrameType.VecWithQuat;
                else if ((header & QUAT_TYPE_MASK) != 0)
                    return FrameType.Quaternion;
                else if ((header & VEC_TYPE_MASK) != 0)
                    return FrameType.Vector3;
                else
                    return FrameType.Float;
            }
        }

        public QuaternionMethod QuaternionMethod => (header & QUAT_TYPE_MASK) switch
        {
            QUAT_TYPE_IDENTITY => QuaternionMethod.Empty,
            QUAT_TYPE_FULL => QuaternionMethod.Full,
            QUAT_TYPE_0x40 => QuaternionMethod.Compressed0x40,
            QUAT_TYPE_0x80 => QuaternionMethod.Compressed0x80,
            _ => QuaternionMethod.None
        };

        public bool HasPosition => (header & VEC_TYPE_MASK) != 0;
        public bool HasOrientation => (header & QUAT_TYPE_MASK) != 0;
        public bool HasAngle => (header & ANGLES) != 0;

        public readonly unsafe float GetTime(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if (Interval >= 0)
                return 0;
            var offset = GetOffset(index, 0, 0);
            fixed (byte* ptr = buffer.Buffer)
                return *(float*) (&ptr[offset]);
        }

        public readonly unsafe float GetAngle(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((header & ANGLES) == 0)
                return 0;
            var offset = GetOffset(index, 0, 0) + (Interval < 0 ? 4 : 0);
            fixed (byte* ptr = buffer.Buffer)
                return *(float*) (&ptr[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetOffset(int index, uint mask, int shift)
        {
            var stride = (header & STRIDE_MASK);
            var off = (header & mask) >> shift;
            return startIdx + (int) (stride * index + off);
        }


        public unsafe Vector3 GetPosition(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((header & VEC_TYPE_MASK) != TYPE_VEC3)
                return Vector3.Zero;
            var offset = GetOffset(index, VEC_OFFSET_MASK, 5);
            fixed (byte* ptr = buffer.Buffer)
                return *(Vector3*) (&ptr[offset]);
        }


        public Quaternion GetQuaternion(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            var offset = GetOffset(index, QUAT_OFFSET_MASK, 12);
            switch (header & QUAT_TYPE_MASK)
            {
                case QUAT_TYPE_FULL:
                    return GetFullQuat(offset);
                case QUAT_TYPE_0x80:
                    return GetQuat0x80(offset);
                case QUAT_TYPE_0x40:
                    return GetQuat0x40(offset);
                case QUAT_TYPE_IDENTITY:
                case 0:
                    return Quaternion.Identity;
                default:
                    throw new InvalidOperationException();
            }
        }

        unsafe Quaternion GetFullQuat(int offset)
        {
            fixed (byte* ptr = buffer.Buffer)
            {
                float* flt = (float*) (&ptr[offset]);
                return new Quaternion(flt[1], flt[2], flt[3], flt[0]);
            }
        }

        unsafe Quaternion GetQuat0x40(int offset)
        {
            fixed (byte* ptr = buffer.Buffer)
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
            fixed (byte* ptr = buffer.Buffer)
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

        unsafe int ReadHeader(LeafNode node)
        {
            if (node.DataSegment.Count < 12) throw new Exception("Anm Header malformed");
            fixed (byte* bytes = node.DataSegment.Array)
            {
                FrameCount = *(int*) (&bytes[node.DataSegment.Offset]);
                Interval = *(float*) (&bytes[node.DataSegment.Offset + 4]);
                return *(int*) (&bytes[node.DataSegment.Offset + 8]);
            }
        }


		public Channel(IntermediateNode root, AnmBuffer buffer)
		{
            //Fetch from nodes
            this.buffer = buffer;
            ArraySegment<byte> cdata = new ArraySegment<byte>();
            int channelType = 0;
			foreach (LeafNode channelSubNode in root)
			{
                if(channelSubNode.Name.Equals("header", StringComparison.OrdinalIgnoreCase))
                    channelType = ReadHeader(channelSubNode);
                else if (channelSubNode.Name.Equals("frames", StringComparison.OrdinalIgnoreCase))
                {
                    cdata = channelSubNode.DataSegment;
                }
            }
            /* Pad data to avoid ARM data alignment errors */
            if((channelType & 0x80) == 0x80 ||
               (channelType & 0x40) == 0x40) {
                int startStride = 0;
                if(Interval < 0) startStride += 4;
                if((channelType & 0x1) == 0x1) startStride += 4;
                if((channelType & 0x2) == 0x2) startStride += 12;
                int fullStride = startStride + 8;
                int compStride = startStride + 6;
                startIdx = buffer.Take(fullStride * FrameCount);
                //channelData = new byte[fullStride * FrameCount];
                for(int i = 0; i < FrameCount; i++) {
                    int src = compStride * i;
                    int dst = startIdx + fullStride * i;
                    for(int j = 0; j < compStride; j++) {
                        buffer.Buffer[dst + j] = cdata[src + j];
                    }
                }
            } else {
                startIdx = buffer.Take(cdata.Count);
                cdata.CopyTo(buffer.Buffer, startIdx);
            }

            int stride = 0;

            if(((channelType & 0x2) == 0x2) &&
               ((channelType & 0x10) == 0x10))
                throw new Exception("Channel has invalid vector specification");
            if (Interval < 0) {
                stride += 4;
            }
            if ((channelType & 0x1) == 0x1)
            {
                header |= ANGLES;
                stride += 4;
            }
            if ((channelType & 0x2) == 0x2)
            {
                header |= TYPE_VEC3;
                header |= (uint)((stride << 5) & VEC_OFFSET_MASK);
                stride += 12;
            }
            if ((channelType & 0x10) == 0x10)
            {
                header |= TYPE_VECEMPTY;
            }
            if ((channelType & 0x40) == 0x40)
            {
                header |= QUAT_TYPE_0x40;
                header |= (uint)((stride << 12) & QUAT_OFFSET_MASK);
                stride += 8;
            }
            if ((channelType & 0x80) == 0x80)
            {
                header |= QUAT_TYPE_0x80;
                header |= (uint)((stride << 12) & QUAT_OFFSET_MASK);
                stride += 8;
            }
            if ((channelType & 0x4) == 0x4)
            {
                header |= QUAT_TYPE_FULL;
                header |= (uint)((stride << 12) & QUAT_OFFSET_MASK);
                stride += 16;
            }
            if ((channelType & 0x20) == 0x20)
            {
                header |= QUAT_TYPE_IDENTITY;
            }
            header |= (uint)(stride & STRIDE_MASK);
        }
    }
}
