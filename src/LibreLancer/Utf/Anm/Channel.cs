// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Utf.Anm
{
	public class Channel
    {
        public int FrameCount { get; private set; }
		public float Interval { get; private set; }
		public int ChannelType { get; private set; }
        public FrameType InterpretedType { get; private set; }
        public QuaternionMethod QuaternionMethod { get; private set; }
        public FloatAccessor Times { get; private set; }
        public Vector3Accessor Positions { get; private set; }
        public FloatAccessor  Angles { get; private set; }
        public QuaternionAccessor Quaternions { get; private set; }
        public bool HasPosition => Positions != null;
        public bool HasOrientation => Quaternions != null;
        public bool HasAngle => Angles != null;

        private byte[] channelData;
        private int stride;
        public abstract class Accessor
        {
            protected Channel C;
            protected int Offset;
            internal Accessor(Channel ch, int offset)
            {
                C = ch;
                Offset = offset;
            }
            protected int GetOffset(int index) => (C.stride * index) + Offset;
        }
        public sealed class FloatAccessor : Accessor
        {
            internal FloatAccessor(Channel ch, int offset) : base(ch, offset){}
            public float this[int index] => Get(index);
            unsafe float Get(int index)
            {
                fixed (byte* ptr = C.channelData)
                    return *(float*) (&ptr[GetOffset(index)]);
            }
        }
        public sealed class Vector3Accessor : Accessor
        {
            private bool blank = false;
            internal Vector3Accessor(Channel ch, int offset, bool blank) : base(ch, offset)
            {
                this.blank = blank;
            }
            public Vector3 this[int index] => Get(index);
            unsafe Vector3 Get(int index)
            {
                if(blank) return Vector3.Zero;
                fixed (byte* ptr = C.channelData)
                    return *(Vector3*) (&ptr[GetOffset(index)]);
            }
        }
        public class QuaternionAccessor : Accessor
        {
            internal QuaternionAccessor(Channel ch, int offset) : base(ch, offset) {}
            public Quaternion this[int index] => Get(index);
            protected virtual unsafe Quaternion Get(int index)
            {
                fixed (byte* ptr = C.channelData)
                {
                    float* flt = (float*) (&ptr[GetOffset(index)]);
                    return new Quaternion(flt[1], flt[2], flt[3], flt[0]);
                }
            }
        }

        class IdentityAccessor : QuaternionAccessor
        {
            internal IdentityAccessor(Channel ch, int offset) : base(ch, offset) {}
            protected override unsafe Quaternion Get(int index) => Quaternion.Identity;
        }
        class CompressedAccessor0x40 : QuaternionAccessor
        {
            internal CompressedAccessor0x40(Channel ch, int offset) : base(ch, offset) {}
            protected override unsafe Quaternion Get(int index)
            {
                fixed (byte* ptr = C.channelData)
                {
                    short* sh = (short*) (&ptr[GetOffset(index)]);
                    var ha = new Vector3(
                        sh[0] / 32767f,
                        sh[1] / 32767f,
                        sh[2] / 32767f
                    );
                    return ReconstructW(ha);
                }
            }
            static Quaternion ReconstructW(Vector3 p)
            {
                var len = p.LengthSquared();
                var w = 0f;
                if (len < 1.0f)
                {
                    w = MathF.Sqrt(1 - len);
                }
                return new Quaternion(p, w);
            }
        }
        
        class CompressedAccessor0x80 : QuaternionAccessor
        {
            internal CompressedAccessor0x80(Channel ch, int offset) : base(ch, offset) {}
            protected override unsafe Quaternion Get(int index)
            {
                fixed (byte* ptr = C.channelData)
                {
                    short* sh = (short*) (&ptr[GetOffset(index)]);
                    var ha = new Vector3(
                    sh[0] / 32767f,
                    sh[1] / 32767f,
                    sh[2] / 32767f
                    );
                    return Mapping0x80(ha);
                }
            }
            static Quaternion Mapping0x80(Vector3 p)
            {
                var s = Vector3.Dot(p, p);
                if (s <= 0)
                    return Quaternion.Identity;
                var length = p.Length();
                var something = MathF.Sin(MathF.PI * length * 0.5f);
                return new Quaternion(
                    p * (something / length),
                    MathF.Sqrt(1f - something * something)
                );
            }
        }
        
        int GetIndex(float time, out float t0, out float t1)
        {
            if (Times != null)
            {
                for (int i = 0; i < FrameCount - 1; i++)
                {
                    if (Times[i + 1] >= time)
                    {
                        t0 = Times[i];
                        t1 = Times[i + 1];
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
                if (Times != null) return Times[FrameCount - 1];
                return Math.Max(Interval * (FrameCount - 1), 0);
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
			foreach (LeafNode channelSubNode in root)
			{
                if(channelSubNode.Name.Equals("header", StringComparison.OrdinalIgnoreCase))
                    ReadHeader(channelSubNode);
                else if (channelSubNode.Name.Equals("frames", StringComparison.OrdinalIgnoreCase))
                {
                    channelData = channelSubNode.ByteArrayData;
                }
            }
            FrameType frameType = FrameType.Float;
            QuaternionMethod = QuaternionMethod.Full;
            bool vec = false;
            bool quat = false;
            bool floats = false;
            if(((ChannelType & 0x2) == 0x2) &&
               ((ChannelType & 0x10) == 0x10))
                throw new Exception("Channel has invalid vector specification");
            if (Interval < 0) {
                Times = new FloatAccessor(this, 0);
                stride += 4;
            }
            if ((ChannelType & 0x1) == 0x1) {
                Angles = new FloatAccessor(this,  stride);
                stride += 4;
            }
            if ((ChannelType & 0x2) == 0x2)
            {
                vec = true;
                Positions = new Vector3Accessor(this, stride, false);
                stride += 12;
            }
            if ((ChannelType & 0x10) == 0x10)
            {
                vec = true;
                Positions = new Vector3Accessor(this, stride, true);
            }
            if ((ChannelType & 0x40) == 0x40)
            {
                quat = true;
                QuaternionMethod = QuaternionMethod.Compressed0x40;
                Quaternions = new CompressedAccessor0x40(this, stride);
                stride += 6;
            }
            if ((ChannelType & 0x80) == 0x80)
            {
                quat = true;
                QuaternionMethod = QuaternionMethod.Compressed0x80;
                Quaternions = new CompressedAccessor0x80(this, stride);
                stride += 6;
            }
            if ((ChannelType & 0x4) == 0x4)
            {
                vec = true;
                Quaternions = new QuaternionAccessor(this, stride);
                stride += 16;
                QuaternionMethod = QuaternionMethod.Full;
            }
            if ((ChannelType & 0x20) == 0x20)
            {
                quat = true;
                Quaternions = new IdentityAccessor(this, 0);
                QuaternionMethod = QuaternionMethod.Empty;
            }
            if (vec && quat) {
                frameType = FrameType.VecWithQuat;
            } else if (vec)
            {
                frameType = FrameType.Vector3;
            } else if (quat)
            {
                frameType = FrameType.Quaternion;
            }
            InterpretedType = frameType;
        }
    }
}
