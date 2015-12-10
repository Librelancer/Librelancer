using System;
using OpenTK.Graphics.OpenGL;
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
    public static class CubeMapFaceExtensions
    {
        public static TextureTarget GL(this CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveX:
                    return TextureTarget.TextureCubeMapPositiveX;
                case CubeMapFace.PositiveY:
                    return TextureTarget.TextureCubeMapPositiveY;
                case CubeMapFace.PositiveZ:
                    return TextureTarget.TextureCubeMapPositiveZ;
                case CubeMapFace.NegativeX:
                    return TextureTarget.TextureCubeMapNegativeX;
                case CubeMapFace.NegativeY:
                    return TextureTarget.TextureCubeMapNegativeY;
                case CubeMapFace.NegativeZ:
                    return TextureTarget.TextureCubeMapNegativeZ;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

