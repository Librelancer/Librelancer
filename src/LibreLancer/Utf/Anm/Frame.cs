// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;
using Neo.IronLua;

namespace LibreLancer.Utf.Anm
{
    public enum FrameType
    {
        Matrix,
        Float,
        Normal,
        Vector3,
        Quaternion,
        VecWithQuat
    }
    public class Frame
    {
        public float? Time { get; private set; }
		public float JointValue { get; private set; }
        public Vector3 NormalValue { get; private set; }
        public Matrix4 ObjectValue { get; private set; }
        public Vector3 VectorValue { get; private set; }
        public Quaternion QuatValue { get; private set; }
		public Frame(BinaryReader reader, bool time, FrameType type)
        {
            if (time) Time = reader.ReadSingle();
			if (type == FrameType.Matrix)
			{
				ObjectValue = ConvertData.ToMatrix3x3(reader);
			}
            else if(type == FrameType.Normal)
            {
                NormalValue = new Vector3(
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f
                );
            }
            else if (type == FrameType.Vector3)
            {
                VectorValue = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            else if (type == FrameType.Quaternion)
            {
                float w = reader.ReadSingle();
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                QuatValue = new Quaternion(x,y,z,w);
            }
            else if (type == FrameType.VecWithQuat)
            {
                VectorValue = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                float w = reader.ReadSingle();
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                QuatValue = new Quaternion(x,y,z,w);
            }
            else
			{
				JointValue = reader.ReadSingle();
			}
        }

        public override string ToString()
        {
			return "Frame";
        }
    }
}
