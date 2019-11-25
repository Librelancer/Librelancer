// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using Neo.IronLua;

namespace LibreLancer.Utf.Anm
{
    public enum FrameType
    {
        Matrix,
        Float,
        Vector3,
        Quaternion,
        VecWithQuat
        
    }
    public enum QuaternionMethod
    {
        Full,
        HalfAngle
    }
    public class Frame
    {
        public float? Time { get; private set; }
		public float JointValue { get; private set; }
        public Vector3 NormalValue { get; private set; }
        public Matrix4 ObjectValue { get; private set; }
        public Vector3 VectorValue { get; private set; }
        public Quaternion QuatValue { get; private set; }
		public Frame(BinaryReader reader, bool time, FrameType type, QuaternionMethod quatMethod)
        {
            if (time) Time = reader.ReadSingle();
			if (type == FrameType.Matrix)
			{
				ObjectValue = ConvertData.ToMatrix3x3(reader);
			}
            else if (type == FrameType.Vector3)
            {
                VectorValue = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            else if (type == FrameType.Quaternion)
            {
                QuatValue = ReadQuaternion(reader, quatMethod);
            }
            else if (type == FrameType.VecWithQuat)
            {
                VectorValue = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                QuatValue = ReadQuaternion(reader, quatMethod);
            }
            else
			{
				JointValue = reader.ReadSingle();
			}
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
 
        public override string ToString()
        {
			return "Frame";
        }
    }
}
