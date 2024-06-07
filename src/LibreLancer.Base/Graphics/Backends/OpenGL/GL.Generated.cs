// AUTOMATICALLY GENERATED
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    static unsafe partial class GL
    {
        private static delegate* unmanaged<int,void> _glEnable;
        private static delegate* unmanaged<int,void> _glDisable;
        private static delegate* unmanaged<int,int*,void> _glGetIntegerv;
        private static delegate* unmanaged<int,int,IntPtr> _glGetStringi;
        private static delegate* unmanaged<int,IntPtr> _glGetString;
        private static delegate* unmanaged<float,float,float,float,void> _glClearColor;
        private static delegate* unmanaged<int,void> _glClear;
        private static delegate* unmanaged<int,int,int,int,void> _glViewport;
        private static delegate* unmanaged<int,int,int,int,void> _glScissor;
        private static delegate* unmanaged<int,int,void> _glBlendFunc;
        private static delegate* unmanaged<int, int, int, int, void> _glBlendFuncSeparate;
        private static delegate* unmanaged<float,float,void> _glDepthRangef;
        private static delegate* unmanaged<int,int,void> _glPolygonMode;
        private static delegate* unmanaged<float,void> _glLineWidth;
        private static delegate* unmanaged<int,void> _glDepthFunc;
        private static delegate* unmanaged<int,void> _glCullFace;
        private static delegate* unmanaged<int,int,void> _glPixelStorei;
        private static delegate* unmanaged<int,void> _glDepthMask;
        private static delegate* unmanaged<int,int,int,int,void> _glColorMask;
        private static delegate* unmanaged<int,float,void> _glAlphaFunc;
        private static delegate* unmanaged<float,float,void> _glPolygonOffset;
        private static delegate* unmanaged<int,uint*,void> _glGenTextures;
        private static delegate* unmanaged<int,uint*,void> _glDeleteTextures;
        private static delegate* unmanaged<int,int,int,void> _glTexParameteri;
        private static delegate* unmanaged<int,int,float,void> _glTexParameterf;
        private static delegate* unmanaged<int,int,Vector4*,void> _glTexParameterfv;
        private static delegate* unmanaged<int,uint,void> _glBindTexture;
        private static delegate* unmanaged<int,void> _glActiveTexture;
        private static delegate* unmanaged<int,int,int,int,int,int,int,int,IntPtr,void> _glTexImage2D;
        private static delegate* unmanaged<int,int,int,int,int,int,void> _glTexStorage2DMultisample;
        private static delegate* unmanaged<int,int,int,int,int,int,void> _glTexImage2DMultisample;
        private static delegate* unmanaged<int,int,int,int,int,int,int,IntPtr,void> _glCompressedTexImage2D;
        private static delegate* unmanaged<int,int,int,int,int,int,int,int,IntPtr,void> _glTexSubImage2D;
        private static delegate* unmanaged<int,int,int,int,IntPtr,void> _glGetTexImage;
        private static delegate* unmanaged<uint,uint,uint,void> _glDispatchCompute;
        private static delegate* unmanaged<int,uint> _glCreateShader;
        private static delegate* unmanaged<uint,int,IntPtr*,IntPtr,void> _glShaderSource;
        private static delegate* unmanaged<uint,void> _glCompileShader;
        private static delegate* unmanaged<uint,int,int*,IntPtr,void> _glGetShaderInfoLog;
        private static delegate* unmanaged<uint,int,int*,IntPtr,void> _glGetProgramInfoLog;
        private static delegate* unmanaged<uint> _glCreateProgram;
        private static delegate* unmanaged<uint,uint,void> _glAttachShader;
        private static delegate* unmanaged<uint,uint,IntPtr,void> _glBindAttribLocation;
        private static delegate* unmanaged<uint,uint,IntPtr,void> _glBindFragDataLocation;
        private static delegate* unmanaged<uint,void> _glLinkProgram;
        private static delegate* unmanaged<uint,void> _glUseProgram;
        private static delegate* unmanaged<uint,int,int*,void> _glGetShaderiv;
        private static delegate* unmanaged<uint,int,int*,void> _glGetProgramiv;
        private static delegate* unmanaged<uint,IntPtr,int> _glGetUniformLocation;
        private static delegate* unmanaged<int,int,void> _glUniform1i;
        private static delegate* unmanaged<int,float,void> _glUniform1f;
        private static delegate* unmanaged<int,int,int,void> _glUniform2i;
        private static delegate* unmanaged<int,float,float,void> _glUniform2f;
        private static delegate* unmanaged<int,float,float,float,void> _glUniform3f;
        private static delegate* unmanaged<int,int,IntPtr,void> _glUniform3fv;
        private static delegate* unmanaged<int,float,float,float,float,void> _glUniform4f;
        private static delegate* unmanaged<int,int,IntPtr,void> _glUniform4fv;
        private static delegate* unmanaged<int,int,int,int,int,void> _glUniform4i;
        private static delegate* unmanaged<int,int,int,IntPtr,void> _glUniformMatrix4fv;
        private static delegate* unmanaged<uint,uint,uint,void> _glUniformBlockBinding;
        private static delegate* unmanaged<uint,IntPtr,int> _glGetUniformBlockIndex;
        private static delegate* unmanaged<int,uint*,void> _glGenBuffers;
        private static delegate* unmanaged<int,uint*,void> _glDeleteBuffers;
        private static delegate* unmanaged<int,uint,void> _glBindBuffer;
        private static delegate* unmanaged<int,uint,uint,IntPtr,IntPtr,void> _glBindBufferRange;
        private static delegate* unmanaged<int,IntPtr,IntPtr,int,void> _glBufferData;
        private static delegate* unmanaged<int,IntPtr,IntPtr,IntPtr,void> _glBufferSubData;
        private static delegate* unmanaged<int,uint*,void> _glGenVertexArrays;
        private static delegate* unmanaged<int,uint*,void> _glDeleteVertexArrays;
        private static delegate* unmanaged<uint,void> _glBindVertexArray;
        private static delegate* unmanaged<int,void> _glEnableVertexAttribArray;
        private static delegate* unmanaged<uint,int,int,int,int,IntPtr,void> _glVertexAttribPointer;
        private static delegate* unmanaged<uint,int,int,int,IntPtr,void> _glVertexAttribIPointer;
        private static delegate* unmanaged<int,void> _glDrawBuffer;
        private static delegate* unmanaged<int,int*,void> _glDrawBuffers;
        private static delegate* unmanaged<uint,uint,IntPtr> _glMapBuffer;
        private static delegate* unmanaged<uint,IntPtr,IntPtr,uint,IntPtr> _glMapBufferRange;
        private static delegate* unmanaged<uint,int> _glUnmapBuffer;
        private static delegate* unmanaged<uint,uint,uint,void> _glBindBufferBase;
        private static delegate* unmanaged<int,void> _glMemoryBarrier;
        private static delegate* unmanaged<int,int,int,IntPtr,void> _glDrawElements;
        private static delegate* unmanaged<int,int,int,void> _glDrawArrays;
        private static delegate* unmanaged<int,int,int,IntPtr,int,void> _glDrawElementsBaseVertex;
        private static delegate* unmanaged<int,uint*,void> _glGenFramebuffers;
        private static delegate* unmanaged<int,int> _glCheckFramebufferStatus;
        private static delegate* unmanaged<int,uint,void> _glBindFramebuffer;
        private static delegate* unmanaged<int,int,int,int,int,int,int,int,int,int,void> _glBlitFramebuffer;
        private static delegate* unmanaged<int,uint*,void> _glGenRenderbuffers;
        private static delegate* unmanaged<int,uint,void> _glBindRenderbuffer;
        private static delegate* unmanaged<int,int,int,int,void> _glRenderbufferStorage;
        private static delegate* unmanaged<int,int,int,int,int,void> _glRenderbufferStorageMultisample;
        private static delegate* unmanaged<int,int,int,uint,void> _glFramebufferRenderbuffer;
        private static delegate* unmanaged<int,int,int,uint,int,void> _glFramebufferTexture2D;
        private static delegate* unmanaged<int,uint*,void> _glDeleteFramebuffers;
        private static delegate* unmanaged<int,uint*,void> _glDeleteRenderbuffers;
        private static delegate* unmanaged<int,void> _glReadBuffer;
        private static delegate* unmanaged<int,int,int,int,int,int,IntPtr,void> _glReadPixels;
        private static delegate* unmanaged<int> _glGetError;
        private static delegate* unmanaged<IntPtr,IntPtr,void> _glDebugMessageCallback;
        private static delegate* unmanaged<int,int,int,int,IntPtr,int,void> _glDebugMessageControl;
        private static delegate* unmanaged<int, int, IntPtr, IntPtr, IntPtr, void> _glCopyBufferSubData;

        public static void Load(Func<string,IntPtr> getProcAddress, bool isGles)
        {
            _glEnable = (delegate* unmanaged<int,void>)getProcAddress("glEnable");
            _glDisable = (delegate* unmanaged<int,void>)getProcAddress("glDisable");
            _glGetIntegerv = (delegate* unmanaged<int,int*,void>)getProcAddress("glGetIntegerv");
            _glGetStringi = (delegate* unmanaged<int,int,IntPtr>)getProcAddress("glGetStringi");
            _glGetString = (delegate* unmanaged<int,IntPtr>)getProcAddress("glGetString");
            _glClearColor = (delegate* unmanaged<float,float,float,float,void>)getProcAddress("glClearColor");
            _glClear = (delegate* unmanaged<int,void>)getProcAddress("glClear");
            _glViewport = (delegate* unmanaged<int,int,int,int,void>)getProcAddress("glViewport");
            _glScissor = (delegate* unmanaged<int,int,int,int,void>)getProcAddress("glScissor");
            _glBlendFunc = (delegate* unmanaged<int,int,void>)getProcAddress("glBlendFunc");
            _glBlendFuncSeparate = (delegate* unmanaged<int, int, int, int, void>)getProcAddress("glBlendFuncSeparate");
            _glDepthRangef = (delegate* unmanaged<float,float,void>)getProcAddress("glDepthRangef");
            _glPolygonMode = (delegate* unmanaged<int,int,void>)getProcAddress("glPolygonMode");
            _glLineWidth = (delegate* unmanaged<float,void>)getProcAddress("glLineWidth");
            _glDepthFunc = (delegate* unmanaged<int,void>)getProcAddress("glDepthFunc");
            _glCullFace = (delegate* unmanaged<int,void>)getProcAddress("glCullFace");
            _glPixelStorei = (delegate* unmanaged<int,int,void>)getProcAddress("glPixelStorei");
            _glDepthMask = (delegate* unmanaged<int,void>)getProcAddress("glDepthMask");
            _glColorMask = (delegate* unmanaged<int,int,int,int,void>)getProcAddress("glColorMask");
            _glAlphaFunc = (delegate* unmanaged<int,float,void>)getProcAddress("glAlphaFunc");
            _glPolygonOffset = (delegate* unmanaged<float,float,void>)getProcAddress("glPolygonOffset");
            _glGenTextures = (delegate* unmanaged<int,uint*,void>)getProcAddress("glGenTextures");
            _glDeleteTextures = (delegate* unmanaged<int,uint*,void>)getProcAddress("glDeleteTextures");
            _glTexParameteri = (delegate* unmanaged<int,int,int,void>)getProcAddress("glTexParameteri");
            _glTexParameterf = (delegate* unmanaged<int,int,float,void>)getProcAddress("glTexParameterf");
            _glTexParameterfv = (delegate* unmanaged<int,int,Vector4*,void>)getProcAddress("glTexParameterfv");
            _glBindTexture = (delegate* unmanaged<int,uint,void>)getProcAddress("glBindTexture");
            _glActiveTexture = (delegate* unmanaged<int,void>)getProcAddress("glActiveTexture");
            _glTexImage2D = (delegate* unmanaged<int,int,int,int,int,int,int,int,IntPtr,void>)getProcAddress("glTexImage2D");
            _glTexStorage2DMultisample = (delegate* unmanaged<int,int,int,int,int,int,void>)getProcAddress("glTexStorage2DMultisample");
            _glTexImage2DMultisample = (delegate* unmanaged<int,int,int,int,int,int,void>)getProcAddress("glTexImage2DMultisample");
            _glCompressedTexImage2D = (delegate* unmanaged<int,int,int,int,int,int,int,IntPtr,void>)getProcAddress("glCompressedTexImage2D");
            _glTexSubImage2D = (delegate* unmanaged<int,int,int,int,int,int,int,int,IntPtr,void>)getProcAddress("glTexSubImage2D");
            _glGetTexImage = (delegate* unmanaged<int,int,int,int,IntPtr,void>)getProcAddress("glGetTexImage");
            _glDispatchCompute = (delegate* unmanaged<uint,uint,uint,void>)getProcAddress("glDispatchCompute");
            _glCreateShader = (delegate* unmanaged<int,uint>)getProcAddress("glCreateShader");
            _glShaderSource = (delegate* unmanaged<uint,int,IntPtr*,IntPtr,void>)getProcAddress("glShaderSource");
            _glCompileShader = (delegate* unmanaged<uint,void>)getProcAddress("glCompileShader");
            _glGetShaderInfoLog = (delegate* unmanaged<uint,int,int*,IntPtr,void>)getProcAddress("glGetShaderInfoLog");
            _glGetProgramInfoLog = (delegate* unmanaged<uint,int,int*,IntPtr,void>)getProcAddress("glGetProgramInfoLog");
            _glCreateProgram = (delegate* unmanaged<uint>)getProcAddress("glCreateProgram");
            _glAttachShader = (delegate* unmanaged<uint,uint,void>)getProcAddress("glAttachShader");
            _glBindAttribLocation = (delegate* unmanaged<uint,uint,IntPtr,void>)getProcAddress("glBindAttribLocation");
            _glBindFragDataLocation = (delegate* unmanaged<uint,uint,IntPtr,void>)getProcAddress("glBindFragDataLocation");
            _glLinkProgram = (delegate* unmanaged<uint,void>)getProcAddress("glLinkProgram");
            _glUseProgram = (delegate* unmanaged<uint,void>)getProcAddress("glUseProgram");
            _glGetShaderiv = (delegate* unmanaged<uint,int,int*,void>)getProcAddress("glGetShaderiv");
            _glGetProgramiv = (delegate* unmanaged<uint,int,int*,void>)getProcAddress("glGetProgramiv");
            _glGetUniformLocation = (delegate* unmanaged<uint,IntPtr,int>)getProcAddress("glGetUniformLocation");
            _glUniform1i = (delegate* unmanaged<int,int,void>)getProcAddress("glUniform1i");
            _glUniform1f = (delegate* unmanaged<int,float,void>)getProcAddress("glUniform1f");
            _glUniform2i = (delegate* unmanaged<int,int,int,void>)getProcAddress("glUniform2i");
            _glUniform2f = (delegate* unmanaged<int,float,float,void>)getProcAddress("glUniform2f");
            _glUniform3f = (delegate* unmanaged<int,float,float,float,void>)getProcAddress("glUniform3f");
            _glUniform3fv = (delegate* unmanaged<int,int,IntPtr,void>)getProcAddress("glUniform3fv");
            _glUniform4f = (delegate* unmanaged<int,float,float,float,float,void>)getProcAddress("glUniform4f");
            _glUniform4fv = (delegate* unmanaged<int,int,IntPtr,void>)getProcAddress("glUniform4fv");
            _glUniform4i = (delegate* unmanaged<int,int,int,int,int,void>)getProcAddress("glUniform4i");
            _glUniformMatrix4fv = (delegate* unmanaged<int,int,int,IntPtr,void>)getProcAddress("glUniformMatrix4fv");
            _glUniformBlockBinding = (delegate* unmanaged<uint,uint,uint,void>)getProcAddress("glUniformBlockBinding");
            _glGetUniformBlockIndex = (delegate* unmanaged<uint,IntPtr,int>)getProcAddress("glGetUniformBlockIndex");
            _glGenBuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glGenBuffers");
            _glDeleteBuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glDeleteBuffers");
            _glBindBuffer = (delegate* unmanaged<int,uint,void>)getProcAddress("glBindBuffer");
            _glBindBufferRange = (delegate* unmanaged<int,uint,uint,IntPtr,IntPtr,void>)getProcAddress("glBindBufferRange");
            _glBufferData = (delegate* unmanaged<int,IntPtr,IntPtr,int,void>)getProcAddress("glBufferData");
            _glBufferSubData = (delegate* unmanaged<int,IntPtr,IntPtr,IntPtr,void>)getProcAddress("glBufferSubData");
            _glGenVertexArrays = (delegate* unmanaged<int,uint*,void>)getProcAddress("glGenVertexArrays");
            _glDeleteVertexArrays = (delegate* unmanaged<int,uint*,void>)getProcAddress("glDeleteVertexArrays");
            _glBindVertexArray = (delegate* unmanaged<uint,void>)getProcAddress("glBindVertexArray");
            _glEnableVertexAttribArray = (delegate* unmanaged<int,void>)getProcAddress("glEnableVertexAttribArray");
            _glVertexAttribPointer = (delegate* unmanaged<uint,int,int,int,int,IntPtr,void>)getProcAddress("glVertexAttribPointer");
            _glVertexAttribIPointer = (delegate* unmanaged<uint,int,int,int,IntPtr,void>)getProcAddress("glVertexAttribIPointer");
            _glDrawBuffer = (delegate* unmanaged<int,void>)getProcAddress("glDrawBuffer");
            _glDrawBuffers = (delegate* unmanaged<int,int*,void>)getProcAddress("glDrawBuffers");
            _glMapBuffer = (delegate* unmanaged<uint,uint,IntPtr>)getProcAddress("glMapBuffer");
            _glMapBufferRange = (delegate* unmanaged<uint,IntPtr,IntPtr,uint,IntPtr>)getProcAddress("glMapBufferRange");
            _glUnmapBuffer = (delegate* unmanaged<uint,int>)getProcAddress("glUnmapBuffer");
            _glBindBufferBase = (delegate* unmanaged<uint,uint,uint,void>)getProcAddress("glBindBufferBase");
            _glMemoryBarrier = (delegate* unmanaged<int,void>)getProcAddress("glMemoryBarrier");
            _glDrawElements = (delegate* unmanaged<int,int,int,IntPtr,void>)getProcAddress("glDrawElements");
            _glDrawArrays = (delegate* unmanaged<int,int,int,void>)getProcAddress("glDrawArrays");
            _glDrawElementsBaseVertex = (delegate* unmanaged<int,int,int,IntPtr,int,void>)getProcAddress("glDrawElementsBaseVertex");
            _glGenFramebuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glGenFramebuffers");
            _glCheckFramebufferStatus = (delegate* unmanaged<int,int>)getProcAddress("glCheckFramebufferStatus");
            _glBindFramebuffer = (delegate* unmanaged<int,uint,void>)getProcAddress("glBindFramebuffer");
            _glBlitFramebuffer = (delegate* unmanaged<int,int,int,int,int,int,int,int,int,int,void>)getProcAddress("glBlitFramebuffer");
            _glGenRenderbuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glGenRenderbuffers");
            _glBindRenderbuffer = (delegate* unmanaged<int,uint,void>)getProcAddress("glBindRenderbuffer");
            _glRenderbufferStorage = (delegate* unmanaged<int,int,int,int,void>)getProcAddress("glRenderbufferStorage");
            _glRenderbufferStorageMultisample = (delegate* unmanaged<int,int,int,int,int,void>)getProcAddress("glRenderbufferStorageMultisample");
            _glFramebufferRenderbuffer = (delegate* unmanaged<int,int,int,uint,void>)getProcAddress("glFramebufferRenderbuffer");
            _glFramebufferTexture2D = (delegate* unmanaged<int,int,int,uint,int,void>)getProcAddress("glFramebufferTexture2D");
            _glDeleteFramebuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glDeleteFramebuffers");
            _glDeleteRenderbuffers = (delegate* unmanaged<int,uint*,void>)getProcAddress("glDeleteRenderbuffers");
            _glReadBuffer = (delegate* unmanaged<int,void>)getProcAddress("glReadBuffer");
            _glReadPixels = (delegate* unmanaged<int,int,int,int,int,int,IntPtr,void>)getProcAddress("glReadPixels");
            _glGetError = (delegate* unmanaged<int>)getProcAddress("glGetError");
            _glDebugMessageCallback = (delegate* unmanaged<IntPtr,IntPtr,void>)getProcAddress("glDebugMessageCallback");
            _glDebugMessageControl = (delegate* unmanaged<int,int,int,int,IntPtr,int,void>)getProcAddress("glDebugMessageControl");
            _glCopyBufferSubData = (delegate* unmanaged<int, int, IntPtr, IntPtr, IntPtr, void>) getProcAddress("glCopyBufferSubData");
        }
        public static void Enable(int flags)
        {
            _glEnable(flags);
            ErrorCheck();
        }
        public static void Disable(int flags)
        {
            _glDisable(flags);
            ErrorCheck();
        }
        public static void GetIntegerv(int val, out int param)
        {
            fixed (int* _param_ptr = &param)
            {
                _glGetIntegerv(val, _param_ptr);
            }
            ErrorCheck();
        }
        public static string GetStringi(int name, int index)
        {
            string retval;
            var _retval_ptr = _glGetStringi(name, index);
            retval =  Marshal.PtrToStringUTF8(_retval_ptr);
            ErrorCheck();
            return retval;
        }
        public static string GetString(int name)
        {
            string retval;
            var _retval_ptr = _glGetString(name);
            retval =  Marshal.PtrToStringUTF8(_retval_ptr);
            ErrorCheck();
            return retval;
        }
        public static void ClearColor(float r, float g, float b, float a)
        {
            _glClearColor(r, g, b, a);
            ErrorCheck();
        }
        public static void Clear(int flags)
        {
            _glClear(flags);
            ErrorCheck();
        }
        public static void Viewport(int x, int y, int width, int height)
        {
            _glViewport(x, y, width, height);
            ErrorCheck();
        }
        public static void Scissor(int x, int y, int width, int height)
        {
            _glScissor(x, y, width, height);
            ErrorCheck();
        }
        public static void BlendFunc(int sfactor, int dfactor)
        {
            _glBlendFunc(sfactor, dfactor);
            ErrorCheck();
        }

        public static void BlendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha)
        {
            _glBlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
            ErrorCheck();
        }
        public static void DepthRange(float near, float far)
        {
            _glDepthRangef(near, far);
            ErrorCheck();
        }
        public static void PolygonMode(int faces, int mode)
        {
            _glPolygonMode(faces, mode);
            ErrorCheck();
        }
        public static void LineWidth(float width)
        {
            _glLineWidth(width);
            ErrorCheck();
        }
        public static void DepthFunc(int func)
        {
            _glDepthFunc(func);
            ErrorCheck();
        }
        public static void CullFace(int face)
        {
            _glCullFace(face);
            ErrorCheck();
        }
        public static void PixelStorei(int pname, int param)
        {
            _glPixelStorei(pname, param);
            ErrorCheck();
        }
        public static void DepthMask(bool flag)
        {
            _glDepthMask((flag ? 1 : 0));
            ErrorCheck();
        }
        public static void ColorMask(bool r, bool g, bool b, bool a)
        {
            _glColorMask((r ? 1 : 0), (g ? 1 : 0), (b ? 1 : 0), (a ? 1 : 0));
            ErrorCheck();
        }
        public static void AlphaFunc(int func, float _ref)
        {
            _glAlphaFunc(func, _ref);
            ErrorCheck();
        }
        public static void PolygonOffset(float factor, float units)
        {
            _glPolygonOffset(factor, units);
            ErrorCheck();
        }
        public static void GenTextures(int n, out uint textures)
        {
            fixed (uint* _textures_ptr = &textures)
            {
                _glGenTextures(n, _textures_ptr);
            }
            ErrorCheck();
        }
        public static void DeleteTextures(int n, ref uint textures)
        {
            fixed (uint* _textures_ptr = &textures)
            {
                _glDeleteTextures(n, _textures_ptr);
            }
            ErrorCheck();
        }
        public static void TexParameteri(int target, int pname, int param)
        {
            _glTexParameteri(target, pname, param);
            ErrorCheck();
        }
        public static void TexParameterf(int target, int pname, float param)
        {
            _glTexParameterf(target, pname, param);
            ErrorCheck();
        }
        public static void TexParameterfv(int target, int pname, ref Vector4 param)
        {
            fixed (Vector4* _param_ptr = &param)
            {
                _glTexParameterfv(target, pname, _param_ptr);
            }
            ErrorCheck();
        }
        public static void BindTexture(int target, uint id)
        {
            _glBindTexture(target, id);
            ErrorCheck();
        }
        public static void ActiveTexture(int unit)
        {
            _glActiveTexture(unit);
            ErrorCheck();
        }
        public static void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data)
        {
            _glTexImage2D(target, level, internalFormat, width, height, border, format, type, data);
            ErrorCheck();
        }
        public static void TexStorage2DMultisample(int target, int samples, int internalFormat, int width, int height, bool fixedsamplelocations)
        {
            _glTexStorage2DMultisample(target, samples, internalFormat, width, height, (fixedsamplelocations ? 1 : 0));
            ErrorCheck();
        }
        public static void TexImage2DMultisample(int target, int samples, int internalFormat, int width, int height, bool fixedsamplelocations)
        {
            _glTexImage2DMultisample(target, samples, internalFormat, width, height, (fixedsamplelocations ? 1 : 0));
            ErrorCheck();
        }
        public static void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data)
        {
            _glCompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data);
            ErrorCheck();
        }
        public static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data)
        {
            _glTexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, data);
            ErrorCheck();
        }
        public static void GetTexImage(int target, int level, int format, int type, IntPtr pixels)
        {
            _glGetTexImage(target, level, format, type, pixels);
            ErrorCheck();
        }
        public static void DispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z)
        {
            _glDispatchCompute(num_groups_x, num_groups_y, num_groups_z);
            ErrorCheck();
        }
        public static uint CreateShader(int shaderType)
        {
            uint retval;
            retval = _glCreateShader(shaderType);
            ErrorCheck();
            return retval;
        }
        public static void CompileShader(uint shader)
        {
            _glCompileShader(shader);
            ErrorCheck();
        }
        public static uint CreateProgram()
        {
            uint retval;
            retval = _glCreateProgram();
            ErrorCheck();
            return retval;
        }
        public static void AttachShader(uint program, uint shader)
        {
            _glAttachShader(program, shader);
            ErrorCheck();
        }
        public static void BindAttribLocation(uint program, uint index, string name)
        {
            var _name_ptr = StringToPtrUTF8(name);
            _glBindAttribLocation(program, index, _name_ptr);
            Marshal.FreeHGlobal(_name_ptr);
            ErrorCheck();
        }
        public static void BindFragDataLocation(uint program, uint colorNumber, string name)
        {
            var _name_ptr = StringToPtrUTF8(name);
            _glBindFragDataLocation(program, colorNumber, _name_ptr);
            Marshal.FreeHGlobal(_name_ptr);
            ErrorCheck();
        }
        public static void LinkProgram(uint program)
        {
            _glLinkProgram(program);
            ErrorCheck();
        }
        public static void UseProgram(uint program)
        {
            _glUseProgram(program);
            ErrorCheck();
        }
        public static void GetShaderiv(uint shader, int pname, out int param)
        {
            fixed (int* _param_ptr = &param)
            {
                _glGetShaderiv(shader, pname, _param_ptr);
            }
            ErrorCheck();
        }
        public static void GetProgramiv(uint program, int pname, out int param)
        {
            fixed (int* _param_ptr = &param)
            {
                _glGetProgramiv(program, pname, _param_ptr);
            }
            ErrorCheck();
        }
        public static int GetUniformLocation(uint program, string name)
        {
            int retval;
            var _name_ptr = StringToPtrUTF8(name);
            retval = _glGetUniformLocation(program, _name_ptr);
            Marshal.FreeHGlobal(_name_ptr);
            ErrorCheck();
            return retval;
        }
        public static void Uniform1i(int location, int v0)
        {
            _glUniform1i(location, v0);
            ErrorCheck();
        }
        public static void Uniform1f(int location, float v0)
        {
            _glUniform1f(location, v0);
            ErrorCheck();
        }
        public static void Uniform2i(int location, int v1, int v2)
        {
            _glUniform2i(location, v1, v2);
            ErrorCheck();
        }
        public static void Uniform2f(int location, float v0, float v1)
        {
            _glUniform2f(location, v0, v1);
            ErrorCheck();
        }
        public static void Uniform3f(int location, float v0, float v1, float v2)
        {
            _glUniform3f(location, v0, v1, v2);
            ErrorCheck();
        }
        public static void Uniform3fv(int location, int count, IntPtr values)
        {
            _glUniform3fv(location, count, values);
            ErrorCheck();
        }
        public static void Uniform4f(int location, float v0, float v1, float v2, float v3)
        {
            _glUniform4f(location, v0, v1, v2, v3);
            ErrorCheck();
        }
        public static void Uniform4fv(int location, int count, IntPtr values)
        {
            _glUniform4fv(location, count, values);
            ErrorCheck();
        }
        public static void Uniform4i(int location, int v0, int v1, int v2, int v3)
        {
            _glUniform4i(location, v0, v1, v2, v3);
            ErrorCheck();
        }
        public static void UniformMatrix4fv(int location, int count, bool transpose, IntPtr value)
        {
            _glUniformMatrix4fv(location, count, (transpose ? 1 : 0), value);
            ErrorCheck();
        }
        public static void UniformBlockBinding(uint program, uint uniformBlockIndex, uint uniformBlockBinding)
        {
            _glUniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
            ErrorCheck();
        }
        public static int GetUniformBlockIndex(uint program, string name)
        {
            int retval;
            var _name_ptr = StringToPtrUTF8(name);
            retval = _glGetUniformBlockIndex(program, _name_ptr);
            Marshal.FreeHGlobal(_name_ptr);
            ErrorCheck();
            return retval;
        }
        public static void GenBuffers(int n, out uint buffers)
        {
            fixed (uint* _buffers_ptr = &buffers)
            {
                _glGenBuffers(n, _buffers_ptr);
            }
            ErrorCheck();
        }
        public static void DeleteBuffers(int n, ref uint id)
        {
            fixed (uint* _id_ptr = &id)
            {
                _glDeleteBuffers(n, _id_ptr);
            }
            ErrorCheck();
        }
        public static void BindBuffer(int target, uint id)
        {
            _glBindBuffer(target, id);
            ErrorCheck();
        }
        public static void BindBufferRange(int target, uint index, uint buffer, IntPtr offset, IntPtr size)
        {
            _glBindBufferRange(target, index, buffer, offset, size);
            ErrorCheck();
        }
        public static void BufferData(int target, IntPtr size, IntPtr data, int usage)
        {
            _glBufferData(target, size, data, usage);
            ErrorCheck();
        }
        public static void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data)
        {
            _glBufferSubData(target, offset, size, data);
            ErrorCheck();
        }

        public static void CopyBufferSubData(int readtarget, int writetarget, IntPtr readoffset, IntPtr writeoffset,
            IntPtr size)
        {
            _glCopyBufferSubData(readtarget, writetarget, readoffset, writeoffset, size);
            ErrorCheck();
        }
        public static void GenVertexArrays(int n, out uint arrays)
        {
            fixed (uint* _arrays_ptr = &arrays)
            {
                _glGenVertexArrays(n, _arrays_ptr);
            }
            ErrorCheck();
        }
        public static void DeleteVertexArrays(int n, ref uint id)
        {
            fixed (uint* _id_ptr = &id)
            {
                _glDeleteVertexArrays(n, _id_ptr);
            }
            ErrorCheck();
        }
        public static void BindVertexArray(uint array)
        {
            _glBindVertexArray(array);
            ErrorCheck();
        }
        public static void EnableVertexAttribArray(int index)
        {
            _glEnableVertexAttribArray(index);
            ErrorCheck();
        }
        public static void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr data)
        {
            _glVertexAttribPointer(index, size, type, (normalized ? 1 : 0), stride, data);
            ErrorCheck();
        }
        public static void VertexAttribIPointer(uint index, int size, int type, int stride, IntPtr data)
        {
            _glVertexAttribIPointer(index, size, type, stride, data);
            ErrorCheck();
        }
        public static IntPtr MapBuffer(uint target, uint access)
        {
            IntPtr retval;
            retval = _glMapBuffer(target, access);
            ErrorCheck();
            return retval;
        }
        public static IntPtr MapBufferRange(uint target, IntPtr offset, IntPtr length, uint access)
        {
            IntPtr retval;
            retval = _glMapBufferRange(target, offset, length, access);
            ErrorCheck();
            return retval;
        }
        public static bool UnmapBuffer(uint target)
        {
            bool retval;
            retval = (_glUnmapBuffer(target) != 0);
            ErrorCheck();
            return retval;
        }
        public static void BindBufferBase(uint target, uint index, uint buffer)
        {
            _glBindBufferBase(target, index, buffer);
            ErrorCheck();
        }
        public static void MemoryBarrier(int barriers)
        {
            _glMemoryBarrier(barriers);
            ErrorCheck();
        }
        public static void DrawElements(int mode, int count, int type, IntPtr indices)
        {
            _glDrawElements(mode, count, type, indices);
            ErrorCheck();
        }
        public static void DrawArrays(int mode, int first, int count)
        {
            _glDrawArrays(mode, first, count);
            ErrorCheck();
        }
        public static void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex)
        {
            _glDrawElementsBaseVertex(mode, count, type, indices, basevertex);
            ErrorCheck();
        }
        public static void GenFramebuffers(int n, out uint framebuffers)
        {
            fixed (uint* _framebuffers_ptr = &framebuffers)
            {
                _glGenFramebuffers(n, _framebuffers_ptr);
            }
            ErrorCheck();
        }
        public static int CheckFramebufferStatus(int target)
        {
            int retval;
            retval = _glCheckFramebufferStatus(target);
            ErrorCheck();
            return retval;
        }
        public static void BindFramebuffer(int target, uint framebuffer)
        {
            _glBindFramebuffer(target, framebuffer);
            ErrorCheck();
        }
        public static void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter)
        {
            _glBlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
            ErrorCheck();
        }
        public static void GenRenderbuffers(int n, out uint renderbuffers)
        {
            fixed (uint* _renderbuffers_ptr = &renderbuffers)
            {
                _glGenRenderbuffers(n, _renderbuffers_ptr);
            }
            ErrorCheck();
        }
        public static void BindRenderbuffer(int target, uint renderbuffer)
        {
            _glBindRenderbuffer(target, renderbuffer);
            ErrorCheck();
        }
        public static void RenderbufferStorage(int target, int internalformat, int width, int height)
        {
            _glRenderbufferStorage(target, internalformat, width, height);
            ErrorCheck();
        }
        public static void RenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height)
        {
            _glRenderbufferStorageMultisample(target, samples, internalformat, width, height);
            ErrorCheck();
        }
        public static void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer)
        {
            _glFramebufferRenderbuffer(target, attachment, renderbuffertarget, renderbuffer);
            ErrorCheck();
        }
        public static void FramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level)
        {
            _glFramebufferTexture2D(target, attachment, textarget, texture, level);
            ErrorCheck();
        }
        public static void DeleteFramebuffers(int n, ref uint framebuffers)
        {
            fixed (uint* _framebuffers_ptr = &framebuffers)
            {
                _glDeleteFramebuffers(n, _framebuffers_ptr);
            }
            ErrorCheck();
        }
        public static void DeleteRenderbuffers(int n, ref uint renderbuffers)
        {
            fixed (uint* _renderbuffers_ptr = &renderbuffers)
            {
                _glDeleteRenderbuffers(n, _renderbuffers_ptr);
            }
            ErrorCheck();
        }
        public static void ReadBuffer(int buffer)
        {
            _glReadBuffer(buffer);
            ErrorCheck();
        }
        public static void ReadPixels(int x, int y, int width, int height, int format, int type, IntPtr data)
        {
            _glReadPixels(x, y, width, height, format, type, data);
            ErrorCheck();
        }
        public static int GetError()
        {
            int retval;
            retval = _glGetError();
            return retval;
        }
        public static void DebugMessageControl(int source, int type, int severity, int count, IntPtr ids, bool enabled)
        {
            _glDebugMessageControl(source, type, severity, count, ids, (enabled ? 1 : 0));
            ErrorCheck();
        }
        private static IntPtr StringToPtrUTF8(string str)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            ((byte*) ptr)[bytes.Length] = 0;
            return ptr;
        }
    }
}
