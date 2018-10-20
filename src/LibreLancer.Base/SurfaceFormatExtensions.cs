// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    public static class SurfaceFormatExtensions
    {
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
				case SurfaceFormat.Color:
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
					glFormat = GL.GL_BGRA;
					glType = GL.GL_UNSIGNED_SHORT_1_5_5_5_REVERSED;
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

