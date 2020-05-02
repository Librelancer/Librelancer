// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
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
            internal Vector3Accessor(Channel ch, int offset) : base(ch, offset) {}
            public Vector3 this[int index] => Get(index);
            unsafe Vector3 Get(int index)
            {
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
        class CompressedAccessor : QuaternionAccessor
        {
            internal CompressedAccessor(Channel ch, int offset) : base(ch, offset) {}
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
                    return InvHalfAngle(ha);
                }
            }
            static Quaternion InvHalfAngle(Vector3 p)
            {
                var d = Vector3.Dot(p, p);
                var s = (float) Math.Sqrt(2.0f - d);
                return new Quaternion(p * s, 1.0f - d);
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
            bool comp = false;
            bool floats = false;
            switch (ChannelType)
            {
                case BIT_NORM:
                case 0x50:
                case 0x40:
                    comp = true;
                    break;
                case BIT_VEC:
                case 0x22:
                    vec = true;
                    break;
                case BIT_QUAT:
                    quat = true;
                    break;
                case BIT_VEC | BIT_QUAT:
                    vec = true;
                    quat = true;
                    break;
                case BIT_VEC | BIT_NORM:
                case BIT_VEC | 0x40:  //special case normal? unsure
                    frameType = FrameType.VecWithQuat;
                    QuaternionMethod = QuaternionMethod.HalfAngle;
                    vec = true;
                    comp = true;
                    break;
                case 144:
                    stride += 6; //0x90 - unimplemented 6 byte data (another quaternion?)
                    break;
                default:
                    floats = true;
                    break;
            }
            InterpretedType = frameType;
            if (Interval == -1) {
                Times = new FloatAccessor(this, 0);
                stride += 4;
            }
            if (vec) {
                Positions = new Vector3Accessor(this, stride);
                stride += 12;
            }
            if (quat) {
                Quaternions = new QuaternionAccessor(this, stride);
                stride += 16;
            }
            if (comp) {
                Quaternions = new CompressedAccessor(this, stride);
                stride += 6;
            }
            if (floats) {
                Angles = new FloatAccessor(this,  stride);
                stride += 4;
            }
		}
    }
}
