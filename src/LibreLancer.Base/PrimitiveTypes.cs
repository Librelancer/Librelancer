using System;
using OpenTK.Graphics.OpenGL;

namespace LibreLancer
{
    public enum PrimitiveTypes
    {
        TriangleList,
        TriangleStrip,
        LineList,
		Points
    }

    public static class PrimitiveTypeExtensions
    {
        public static PrimitiveType GLType(this PrimitiveTypes type)
        {
            switch (type)
            {
                case PrimitiveTypes.LineList:
                    return PrimitiveType.Lines;
                case PrimitiveTypes.TriangleList:
                    return PrimitiveType.Triangles;
                case PrimitiveTypes.TriangleStrip:
                    return PrimitiveType.TriangleStrip;
			case PrimitiveTypes.Points:
				return PrimitiveType.Points;
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
					return 1;
            }
            throw new ArgumentException();
        }
    }
}
