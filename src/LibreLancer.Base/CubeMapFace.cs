// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public enum CubeMapFace
    {
        PositiveX,
		NegativeX,
        PositiveY,
		NegativeY,
        PositiveZ,
        NegativeZ
    }
    static class CubeMapFaceExtensions
    {
        public static int ToGL(this CubeMapFace face)
        {
            switch (face)
            {
				case CubeMapFace.PositiveX:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_X;
                case CubeMapFace.PositiveY:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_Y;
                case CubeMapFace.PositiveZ:
					return GL.GL_TEXTURE_CUBE_MAP_POSITIVE_Z;
				case CubeMapFace.NegativeX:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_X;
				case CubeMapFace.NegativeY:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_Y;
				case CubeMapFace.NegativeZ:
					return GL.GL_TEXTURE_CUBE_MAP_NEGATIVE_Z;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

