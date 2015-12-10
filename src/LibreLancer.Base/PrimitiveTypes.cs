using System;
using OpenTK.Graphics.OpenGL;

namespace LibreLancer
{
    public enum PrimitiveTypes
    {
        TriangleList,
        TriangleStrip,
        LineList
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
            }
            throw new ArgumentException();
        }
    }
}
