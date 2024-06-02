// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Graphics.Backends.OpenGL
{
    static partial class GL
    {
        //debug
        public const int GL_DEBUG_OUTPUT_KHR = 0x92E0;
        public const int GL_DEBUG_SOURCE_SHADER_COMPILER = 0x8248;
        public const int GL_DEBUG_SOURCE_OTHER = 0x824B;
        public const int GL_DONT_CARE = 0x1100;
        public const int GL_DEBUG_SEVERITY_LOW = 0x9148;
        public const int GL_DEBUG_TYPE_PERFORMANCE = 0x8250;
        public const int GL_DEBUG_TYPE_ERROR = 0x824C;
        //clear buffers
		public const int GL_COLOR_BUFFER_BIT = 0x00004000;
		public const int GL_DEPTH_BUFFER_BIT = 0x00000100;
		//Shaders
		public const int GL_FRAGMENT_SHADER = 0x8B30;
		public const int GL_COMPUTE_SHADER = 0x91B9;
		public const int GL_VERTEX_SHADER = 0x8B31;
		public const int GL_GEOMETRY_SHADER = 0x8DD9;
		public const int GL_COMPILE_STATUS = 0x8B81;
		public const int GL_LINK_STATUS = 0x8B82;
        public const int GL_INVALID_INDEX = -1;

		public const int GL_TEXTURE_2D = 0x0DE1;
		public const int GL_TEXTURE_2D_MULTISAMPLE = 0x9100;
		public const int GL_TEXTURE_MIN_FILTER = 0x2801;
		public const int GL_TEXTURE_MAG_FILTER = 0x2800;
		public const int GL_TEXTURE_BORDER_COLOR = 0x1004;
        public const int GL_TEXTURE_MAX_LEVEL = 0x813D;
        public const int GL_LINEAR = 0x2601;
		public const int GL_LINEAR_MIPMAP_NEAREST = 0x2701;
		public const int GL_LINEAR_MIPMAP_LINEAR = 0x2703;
		public const int GL_NEAREST = 0x2600;
		public const int GL_TEXTURE0 = 0x84C0;
		public const int GL_TEXTURE_WRAP_S = 0x2802;
		public const int GL_TEXTURE_WRAP_T = 0x2803;
		public const int GL_CLAMP_TO_EDGE = 0x812F;
		public const int GL_CLAMP_TO_BORDER = 0x812D;
        public const int GL_REPEAT = 0x2901;
		public const int GL_MIRRORED_REPEAT = 0x8370;
        public const int GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;
        public const int GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FF;
		//Texture Cube
		public const int GL_TEXTURE_CUBE_MAP = 0x8513;
		public const int GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
		public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
		public const int GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
		public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
		public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
		public const int GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
		//Texture Formats
		public const int GL_RG = 0x8227;
		public const int GL_RGB = 0x1907;
		public const int GL_RGB5_A1 = 0x8057;
		public const int GL_RGBA = 0x1908;
		public const int GL_RGBA4 = 0x8056;
		public const int GL_RGBA16F = 0x881A;
		public const int GL_RGBA32F = 0x8814;
		public const int GL_BGRA = 0x80E1;
		public const int GL_R32F = 0x822E;
		public const int GL_R16F = 0x822D;
		public const int GL_RG32F = 0x8231;
		public const int GL_RG16F = 0x822F;
		public const int GL_RED = 0x1903;
		public const int GL_R8 = 0x8229;
		public const int GL_R8I = 0x8231;
		public const int GL_R8UI = 0x8232;
		public const int GL_RG8I = 0x8237;
        public const int GL_RGBA8 = 0x8058;
		public const int GL_RGBA8I = 0x8D8E;
		public const int GL_R16I = 0x8233;
		public const int GL_RG16UI = 0x823A;
		public const int GL_RGBA16UI = 0x8D76;
		public const int GL_RGB10_A2UI = 0x906F;

		public const int GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1;
		public const int GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2;
		public const int GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3;
        public const int GL_COMPRESSED_RED_RGTC1_EXT = 0x8DBB;
        public const int GL_COMPRESSED_RED_GREEN_RGTC2_EXT = 0x8DBD;
		public const int GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;

		public const int GL_UNSIGNED_BYTE = 0x1401;
		public const int GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
		public const int GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
        public const int GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
        //REVERSED not supported on GLES
		//public const int GL_UNSIGNED_SHORT_1_5_5_5_REVERSED = 0x8366;
		public const int GL_UNSIGNED_INT_10_10_10_2 = 0x8036;

		public const int GL_HALF_FLOAT = 0x140B;
		public const int GL_FLOAT = 0x1406;
		public const int GL_UNSIGNED_SHORT = 0x1403;
        public const int GL_SHORT = 0x1402;
		public const int GL_BYTE = 0x1400;

		public const int GL_ARRAY_BUFFER = 0x8892;
		public const int GL_ELEMENT_ARRAY_BUFFER = 0x8893;
		public const int GL_SHADER_STORAGE_BUFFER = 0x90D2;
        public const int GL_UNIFORM_BUFFER = 0x8A11;
        public const int GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8a34;
        public const int GL_COPY_READ_BUFFER = 0x8F36;
        public const int GL_COPY_WRITE_BUFFER = 0x8F37;

		public const int GL_STREAM_DRAW = 0x88E0;
		public const int GL_DYNAMIC_DRAW = 0x88E8;
		public const int GL_STATIC_DRAW = 0x88E4;

		public const int GL_POINTS = 0x0000;
		public const int GL_LINES = 0x0001;
		public const int GL_TRIANGLES = 0x0004;
		public const int GL_TRIANGLE_STRIP = 0x0005;
		public const int GL_LINE_STRIP = 0x0003;

		public const int GL_BLEND = 0x0BE2;
		public const int GL_DEPTH_TEST = 0x0B71;
		public const int GL_SCISSOR_TEST = 0x0C11;
		public const int GL_SRC_ALPHA = 0x0302;
		public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const int GL_DST_ALPHA = 0x304;
        public const int GL_ONE_MINUS_DST_ALPHA = 0x305;
        public const int GL_SRC_ALPHA_SATURATE = 0x308;
		public const int GL_CULL_FACE = 0x0B44;
		public const int GL_ZERO = 0;
		public const int GL_ONE = 1;

		public const int GL_EQUAL = 0x0202;
		public const int GL_LEQUAL = 0x0203;

		public const int GL_NUM_EXTENSIONS = 0x821D;
		public const int GL_EXTENSIONS = 0x1F03;

		public const int GL_FRAMEBUFFER = 0x8D40;
		public const int GL_READ_FRAMEBUFFER = 0x8CA8;
		public const int GL_DRAW_FRAMEBUFFER = 0x8CA9;
		public const int GL_RENDERBUFFER = 0x8D41;
        public const int GL_DEPTH_COMPONENT16 = 0x81A5;
		public const int GL_DEPTH_COMPONENT24 = 0x81A6;
		public const int GL_DEPTH_COMPONENT = 0x1902;
		public const int GL_DEPTH_ATTACHMENT = 0x8D00;
		public const int GL_COLOR_ATTACHMENT0 = 0x8CE0;

		public const int GL_MULTISAMPLE = 0x809D;

		public const int GL_FRONT = 0x0404;
		public const int GL_FRONT_AND_BACK = 0x0408;
		public const int GL_LINE = 0x1B01;
		public const int GL_FILL = 0x1B02;
		public const int GL_BACK = 0x0405;

		public const int GL_UNPACK_ALIGNMENT = 0x0CF5;
		public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const int GL_ONE_MINUS_DST_COLOR = 0x0307;
        public const int GL_DST_COLOR = 0x0306;
        public const int GL_SRC_COLOR = 0x0300;

        public const int GL_RENDERER = 0x1F01;
        public const int GL_VERSION = 0x1F02;
        public const int GL_MAX_SAMPLES = 0x8D57;

        public const int GL_WRITE_ONLY = 0x88B9;
		public const int GL_READ_WRITE = 0x88BA;

		public const int GL_SHADER_STORAGE_BARRIER_BIT = 0x00002000;

        public const int GL_MAP_READ_BIT = 0x0001;
        public const int GL_MAP_WRITE_BIT = 0x0002;
        public const int GL_MAP_INVALIDATE_BUFFER_BIT = 0x0008;
        public const int GL_MAP_UNSYNCHRONIZED_BIT = 0x0020;
    }
}

