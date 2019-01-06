// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    public enum PrimitiveTypes : byte
    {
        TriangleList,
        TriangleStrip,
        LineList,
		LineStrip,
		Points
    }

    public static class PrimitiveTypeExtensions
    {
        public static int GLType(this PrimitiveTypes type)
        {
            switch (type)
            {
				case PrimitiveTypes.LineList:
					return GL.GL_LINES;
				case PrimitiveTypes.TriangleList:
					return GL.GL_TRIANGLES;
				case PrimitiveTypes.TriangleStrip:
					return GL.GL_TRIANGLE_STRIP;
				case PrimitiveTypes.LineStrip:
					return GL.GL_LINE_STRIP;
				case PrimitiveTypes.Points:
					return GL.GL_POINTS;
            }
            throw new ArgumentException();
        }

        public static int GetArrayLength(this PrimitiveTypes primitiveType, int primitiveCount)
        {
            switch (primitiveType)
            {
                case PrimitiveTypes.LineList:
                    return primitiveCount * 2;
                case PrimitiveTypes.TriangleList:
                    return primitiveCount * 3;
                case PrimitiveTypes.TriangleStrip:
                    return 3 + (primitiveCount - 1);
				case PrimitiveTypes.Points:
				case PrimitiveTypes.LineStrip:
					return primitiveCount;
            }
            throw new ArgumentException();
        }
    }
}
