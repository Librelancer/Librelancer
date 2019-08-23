// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf.Anm
{
    public enum FrameType
    {
        Matrix,
        Float,
        IK
    }
    public class Frame
    {
        public float? Time { get; private set; }
		public float JointValue { get; private set; }
        public Vector3 IKValue { get; private set; }
        public Matrix4 ObjectValue { get; private set; }
		public Frame(BinaryReader reader, bool time, FrameType type)
        {
            if (time) Time = reader.ReadSingle();
			if (type == FrameType.Matrix)
			{
				ObjectValue = ConvertData.ToMatrix3x3(reader);
			}
            else if(type == FrameType.IK)
            {
                IKValue = new Vector3(
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f,
                    reader.ReadInt16() / 32767f
                );
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
