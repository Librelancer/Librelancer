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
        const uint VECTOR_MASK = 0x2 | 0x10;
        const uint QUATERNION_MASK = 0x4 | 0x20 | 0x40 | 0x80;

        private uint header = 0;
        private int startIdx = 0;

        private AnmBuffer buffer;

        public int FrameCount { get; private set; }
        public float Interval { get; private set; }

        public int ChannelType => (int)(header & 0xFF);

        public FrameType InterpretedType
        {
            get
            {
                if ((header & QUATERNION_MASK) != 0 && (header & VECTOR_MASK) != 0)
                    return FrameType.VecWithQuat;
                else if ((header & QUATERNION_MASK) != 0)
                    return FrameType.Quaternion;
                else if ((header & VECTOR_MASK) != 0)
                    return FrameType.Vector3;
                else
                    return FrameType.Float;
            }
        }

        public QuaternionMethod QuaternionMethod => (header & QUATERNION_MASK) switch
        {
            0x20 => QuaternionMethod.Empty,
            0x4 => QuaternionMethod.Full,
            0x40 => QuaternionMethod.Compressed0x40,
            0x80 => QuaternionMethod.Compressed0x80,
            _ => QuaternionMethod.None
        };

        public bool HasPosition => (header & VECTOR_MASK) != 0;
        public bool HasOrientation => (header & QUATERNION_MASK) != 0;
        public bool HasAngle => (header & 0x1) != 0;

        public readonly float GetTime(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if (Interval >= 0)
                return 0;
            var stride = (header >> 8);
            var offset = startIdx + (int)(stride * index);
            return Unsafe.ReadUnaligned<float>(ref buffer.Buffer[offset]);
        }

        public readonly float GetAngle(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((header & 0x1) == 0)
                return 0;
            var stride = (header >> 8);
            var field = (Interval <= 0 ? 4 : 0);
            var offset = startIdx + (int)(stride * index + field);
            return Unsafe.ReadUnaligned<float>(ref buffer.Buffer[offset]);
        }


        public readonly Vector3 GetPosition(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            if ((header & 0x2) == 0)
                return Vector3.Zero;
            var stride = (header >> 8);
            var field = (Interval <= 0 ? 4 : 0);
            var offset = startIdx + (int)(stride * index + field);
            return Unsafe.ReadUnaligned<Vector3>(ref buffer.Buffer[offset]);
        }


        public readonly Quaternion GetQuaternion(int index)
        {
            if (index < 0 || index >= FrameCount) throw new IndexOutOfRangeException();
            var stride = (header >> 8);
            var field = (Interval <= 0 ? 4 : 0) + ((header & 0x2) == 0x2 ? 12 : 0);
            var offset = startIdx + (int)(stride * index + field);
            switch (header & QUATERNION_MASK)
            {
                case 0x4:
                    return GetFullQuat(offset);
                case 0x80:
                    return GetQuat0x80(offset);
                case 0x40:
                    return GetQuat0x40(offset);
                default:
                    return Quaternion.Identity;
            }
        }

        readonly Quaternion GetFullQuat(int offset) => Unsafe.ReadUnaligned<Quaternion>(ref buffer.Buffer[offset]);

        readonly Quaternion GetQuat0x40(int offset)
        {
            var ha = new Vector3(
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset]) / 32767f,
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset + 2]) / 32767f,
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset + 4]) / 32767f
            );
            var len = ha.LengthSquared();
            var w = 0f;
            if (len < 1.0f)
            {
                w = MathF.Sqrt(1 - len);
            }

            return new Quaternion(ha, w);
        }

        readonly Quaternion GetQuat0x80(int offset)
        {
            var ha = new Vector3(
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset]) / 32767f,
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset + 2]) / 32767f,
                Unsafe.ReadUnaligned<short>(ref buffer.Buffer[offset + 4]) / 32767f
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

        readonly int GetIndex(float time, out float t0, out float t1)
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

            var idx = MathHelper.Clamp((int)Math.Floor(time / Interval), 0, FrameCount - 1);
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

        public readonly Vector3 PositionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return GetPosition(FrameCount - 1);
            var a = GetPosition(idx);
            var b = GetPosition(idx + 1);
            var blend = (time - t0) / (t1 - t0);
            return a + ((b - a) * blend);
        }

        public readonly Quaternion QuaternionAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1) return GetQuaternion(FrameCount - 1);
            var a = GetQuaternion(idx);
            var b = GetQuaternion(idx + 1);
            return Quaternion.Slerp(a, b, (time - t0) / (t1 - t0));
        }

        public readonly ChannelFloat FloatAtTime(float time)
        {
            var idx = GetIndex(time, out float t0, out float t1);
            if (idx == FrameCount - 1)
            {
                return GetAngle(FrameCount - 1);
            }

            var a = GetAngle(idx);
            var b = GetAngle(idx + 1);
            var blend = (time - t0) / (t1 - t0);
            return new ChannelFloat(a, b, blend);
        }

        void CalculateStride()
        {
            uint channelType = (header & 0xFF);
            int stride = 0;
            if (Interval < 0)
            {
                stride += 4;
            }

            if ((channelType & 0x1) == 0x1)
            {
                stride += 4;
            }

            if ((channelType & 0x2) == 0x2)
            {
                stride += 12;
            }

            if ((channelType & 0x40) == 0x40)
            {
                stride += 6;
            }

            if ((channelType & 0x80) == 0x80)
            {
                stride += 6;
            }

            if ((channelType & 0x4) == 0x4)
            {
                stride += 16;
            }

            header = (uint)(stride << 8) | (channelType & 0xFF);
        }


        public Channel(IntermediateNode root, AnmBuffer buffer)
        {
            //Fetch from nodes
            this.buffer = buffer;
            ArraySegment<byte> cdata = new ArraySegment<byte>();
            foreach (LeafNode node in root)
            {
                if (node.Name.Equals("header", StringComparison.OrdinalIgnoreCase))
                {
                    if (node.DataSegment.Count < 12) throw new Exception("Anm Header malformed");
                    FrameCount = Unsafe.ReadUnaligned<int>(ref node.DataSegment.Array![node.DataSegment.Offset]);
                    Interval = Unsafe.ReadUnaligned<float>(ref node.DataSegment.Array![node.DataSegment.Offset + 4]);
                    header = Unsafe.ReadUnaligned<uint>(ref node.DataSegment.Array![node.DataSegment.Offset + 8]);
                }
                else if (node.Name.Equals("frames", StringComparison.OrdinalIgnoreCase))
                {
                    cdata = node.DataSegment;
                }
            }

            startIdx = buffer.Take(cdata.Count);
            cdata.CopyTo(buffer.Buffer, startIdx);

            if (((header & 0x1) == 0x1) &&
                (header != 0x1))
            {
                throw new Exception("Channel specification error: angles cannot combine with other types");
            }

            if (BitOperations.PopCount(header & VECTOR_MASK) > 1)
            {
                throw new Exception("Channel specification error: more than one vector type");
            }

            if (BitOperations.PopCount(header & QUATERNION_MASK) > 1)
            {
                throw new Exception("Channel specification error: more than one quaternion type");
            }

            CalculateStride();
        }
    }
}
