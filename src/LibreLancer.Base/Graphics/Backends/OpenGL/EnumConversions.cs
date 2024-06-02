using System;

namespace LibreLancer.Graphics.Backends.OpenGL;

static class EnumConversions
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

    internal static void GetGLFormat(this SurfaceFormat format,
        out int glInternalFormat,
        out int glFormat,
        out int glType)
    {
        glInternalFormat = GL.GL_RGBA;
        glFormat = GL.GL_RGBA;
        glType = GL.GL_UNSIGNED_BYTE;

        switch (format)
        {
            case SurfaceFormat.Bgra8:
                glInternalFormat = GL.GL_RGBA;
                glFormat = GL.GL_BGRA;
                glType = GL.GL_UNSIGNED_BYTE;
                break;
            case SurfaceFormat.R8:
                glInternalFormat = GL.GL_R8;
                glFormat = GL.GL_RED;
                glType = GL.GL_UNSIGNED_BYTE;
                break;
            case SurfaceFormat.Bgr565:
                glInternalFormat = GL.GL_RGB;
                glFormat = GL.GL_RGB;
                glType = GL.GL_UNSIGNED_SHORT_5_6_5;
                break;
            case SurfaceFormat.Depth:
                glInternalFormat = GL.GL_DEPTH_COMPONENT;
                glFormat = GL.GL_DEPTH_COMPONENT;
                glType = GL.GL_FLOAT;
                break;
            case SurfaceFormat.Bgra4444:
                glInternalFormat = GL.GL_RGBA4;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_UNSIGNED_SHORT_4_4_4_4;
                break;
            case SurfaceFormat.Bgra5551:
                glInternalFormat = GL.GL_RGB5_A1;
                glFormat = GL.GL_RGBA;
                //converted internally. reverse not supported on GLES
                glType = GL.GL_UNSIGNED_SHORT_5_5_5_1;
                break;
            /*case SurfaceFormat.Alpha8: luminance removed in GL 3.1
                glInternalFormat = PixelInternalFormat.Luminance;
                glFormat = PixelFormat.Luminance;
                glType = PixelType.UnsignedByte;
                break;*/
            case SurfaceFormat.Dxt1:
                glInternalFormat = GL.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
                glFormat = GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS;
                break;
            case SurfaceFormat.Dxt3:
                glInternalFormat = GL.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
                glFormat = GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS;
                break;
            case SurfaceFormat.Dxt5:
                glInternalFormat = GL.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
                glFormat = GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS;
                break;
            case SurfaceFormat.Rgtc1:
                glInternalFormat = GL.GL_COMPRESSED_RED_RGTC1_EXT;
                glFormat = GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS;
                break;
            case SurfaceFormat.Rgtc2:
                glInternalFormat = GL.GL_COMPRESSED_RED_GREEN_RGTC2_EXT;
                glFormat = GL.GL_NUM_COMPRESSED_TEXTURE_FORMATS;
                break;

            case SurfaceFormat.Single:
                glInternalFormat = GL.GL_R32F;
                glFormat = GL.GL_RED;
                glType = GL.GL_FLOAT;
                break;

            case SurfaceFormat.HalfVector2:
                glInternalFormat = GL.GL_RG16F;
                glFormat = GL.GL_RG;
                glType = GL.GL_HALF_FLOAT;
                break;

            // HdrBlendable implemented as HalfVector4 (see http://blogs.msdn.com/b/shawnhar/archive/2010/07/09/surfaceformat-hdrblendable.aspx)
            case SurfaceFormat.HdrBlendable:
            case SurfaceFormat.HalfVector4:
                glInternalFormat = GL.GL_RGBA16F;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_HALF_FLOAT;
                break;

            case SurfaceFormat.HalfSingle:
                glInternalFormat = GL.GL_R16F;
                glFormat = GL.GL_RED;
                glType = GL.GL_HALF_FLOAT;
                break;

            case SurfaceFormat.Vector2:
                glInternalFormat = GL.GL_RG32F;
                glFormat = GL.GL_RG;
                glType = GL.GL_FLOAT;
                break;

            case SurfaceFormat.Vector4:
                glInternalFormat = GL.GL_RGBA32F;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_FLOAT;
                break;

            case SurfaceFormat.NormalizedByte2:
                glInternalFormat = GL.GL_RG8I;
                glFormat = GL.GL_RG;
                glType = GL.GL_BYTE;
                break;

            case SurfaceFormat.NormalizedByte4:
                glInternalFormat = GL.GL_RGBA8I;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_BYTE;
                break;

            case SurfaceFormat.Rg32:
                glInternalFormat = GL.GL_RG16UI;
                glFormat = GL.GL_RG;
                glType = GL.GL_UNSIGNED_SHORT;
                break;

            case SurfaceFormat.Rgba64:
                glInternalFormat = GL.GL_RGBA16UI;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_UNSIGNED_SHORT;
                break;

            case SurfaceFormat.Rgba1010102:
                glInternalFormat = GL.GL_RGB10_A2UI;
                glFormat = GL.GL_RGBA;
                glType = GL.GL_UNSIGNED_INT_10_10_10_2;
                break;

            default:
                throw new NotSupportedException();
        }
    }

    internal static int GetSizeEstimate(this SurfaceFormat format)
    {
        switch (format)
        {
            case SurfaceFormat.Vector4:
                return 16;
            case SurfaceFormat.HdrBlendable:
            case SurfaceFormat.HalfVector4:
            case SurfaceFormat.Vector2:
            case SurfaceFormat.Rgba64:
                return 8;
            case SurfaceFormat.HalfVector2:
            case SurfaceFormat.Single:
            case SurfaceFormat.Rg32:
            case SurfaceFormat.NormalizedByte4:
            case SurfaceFormat.Rgba1010102:
            case SurfaceFormat.Bgra8:
            case SurfaceFormat.Depth:
                return 4;
            case SurfaceFormat.HalfSingle:
            case SurfaceFormat.NormalizedByte2:
            case SurfaceFormat.Bgr565:
            case SurfaceFormat.Bgra4444:
            case SurfaceFormat.Bgra5551:
                return 2;
            case SurfaceFormat.R8:
                return 1;

            default:
                throw new InvalidOperationException();
        }
    }

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
