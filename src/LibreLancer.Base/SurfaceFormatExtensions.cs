using System;
using OpenTK.Graphics.OpenGL;
namespace LibreLancer
{
    public static class SurfaceFormatExtensions
    {
        internal static void GetGLFormat(this SurfaceFormat format,
                                          out PixelInternalFormat glInternalFormat,
                                          out PixelFormat glFormat,
                                          out PixelType glType)
        {
            glInternalFormat = PixelInternalFormat.Rgba;
            glFormat = PixelFormat.Rgba;
            glType = PixelType.UnsignedByte;

            switch (format)
            {
                case SurfaceFormat.Color:
                    glInternalFormat = PixelInternalFormat.Rgba;
                    glFormat = PixelFormat.Bgra;
                    glType = PixelType.UnsignedByte;
                    break;
				case SurfaceFormat.R8:
					glInternalFormat = PixelInternalFormat.R8;
					glFormat = PixelFormat.Red;
					glType = PixelType.UnsignedByte;
					break;
                case SurfaceFormat.Bgr565:
                    glInternalFormat = PixelInternalFormat.Rgb;
                    glFormat = PixelFormat.Rgb;
                    glType = PixelType.UnsignedShort565;
                    break;
                case SurfaceFormat.Bgra4444:
                    glInternalFormat = PixelInternalFormat.Rgba4;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedShort4444;
                    break;
                case SurfaceFormat.Bgra5551:
					glInternalFormat = PixelInternalFormat.Rgb5A1;
                    glFormat = PixelFormat.Bgra;
                    glType = PixelType.UnsignedShort1555Reversed;
                    break;
                /*case SurfaceFormat.Alpha8: luminance removed in GL 3.1
                    glInternalFormat = PixelInternalFormat.Luminance;
                    glFormat = PixelFormat.Luminance;
                    glType = PixelType.UnsignedByte;
                    break;*/
                case SurfaceFormat.Dxt1:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;
                case SurfaceFormat.Dxt3:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;
                case SurfaceFormat.Dxt5:
                    glInternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    glFormat = (PixelFormat)All.CompressedTextureFormats;
                    break;

                case SurfaceFormat.Single:
                    glInternalFormat = PixelInternalFormat.R32f;
                    glFormat = PixelFormat.Red;
                    glType = PixelType.Float;
                    break;

                case SurfaceFormat.HalfVector2:
                    glInternalFormat = PixelInternalFormat.Rg16f;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.HalfFloat;
                    break;

                // HdrBlendable implemented as HalfVector4 (see http://blogs.msdn.com/b/shawnhar/archive/2010/07/09/surfaceformat-hdrblendable.aspx)
                case SurfaceFormat.HdrBlendable:
                case SurfaceFormat.HalfVector4:
                    glInternalFormat = PixelInternalFormat.Rgba16f;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.HalfFloat;
                    break;

                case SurfaceFormat.HalfSingle:
                    glInternalFormat = PixelInternalFormat.R16f;
                    glFormat = PixelFormat.Red;
                    glType = PixelType.HalfFloat;
                    break;

                case SurfaceFormat.Vector2:
                    glInternalFormat = PixelInternalFormat.Rg32f;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.Float;
                    break;

                case SurfaceFormat.Vector4:
                    glInternalFormat = PixelInternalFormat.Rgba32f;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.Float;
                    break;

                case SurfaceFormat.NormalizedByte2:
                    glInternalFormat = PixelInternalFormat.Rg8i;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.Byte;
                    break;

                case SurfaceFormat.NormalizedByte4:
                    glInternalFormat = PixelInternalFormat.Rgba8i;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.Byte;
                    break;

                case SurfaceFormat.Rg32:
                    glInternalFormat = PixelInternalFormat.Rg16ui;
                    glFormat = PixelFormat.Rg;
                    glType = PixelType.UnsignedShort;
                    break;

                case SurfaceFormat.Rgba64:
                    glInternalFormat = PixelInternalFormat.Rgba16ui;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedShort;
                    break;

                case SurfaceFormat.Rgba1010102:
                    glInternalFormat = PixelInternalFormat.Rgb10A2ui;
                    glFormat = PixelFormat.Rgba;
                    glType = PixelType.UnsignedInt1010102;
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
        internal static int GetSize(this SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Dxt1:
                    return 8;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                    return 16;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

