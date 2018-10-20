// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf.Anm
{
    public class Frame
    {
        public float? Time { get; private set; }
		public float JointValue { get; private set; }
		public Matrix4 ObjectValue { get; private set; }
		public Frame(BinaryReader reader, bool time, bool matrix)
        {
            if (time) Time = reader.ReadSingle();
			if (matrix)
			{
				ObjectValue = ConvertData.ToMatrix3x3(reader);
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
