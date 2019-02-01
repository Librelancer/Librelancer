// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Linq;
using System.Linq.Expressions;
using LibreLancer.GLDelegates;

namespace LibreLancer
{
	public static partial class GL
	{
		//General State
		[MapsTo("glEnable")]
		public static Enable Enable;
		[MapsTo("glDisable")]
		public static Disable Disable;
		[MapsTo("glGetIntegerv")]
		public static GetIntegerv GetIntegerv;
		[MapsTo("glClearColor")]
		public static ClearColor ClearColor;
		[MapsTo("glClear")]
		public static Clear Clear;
		[MapsTo("glViewport")]
		public static Viewport Viewport;
		[MapsTo("glBlendFunc")]
		public static BlendFunc BlendFunc;
		[MapsTo("glScissor")]
		public static Scissor Scissor;
		/*[MapsTo("glBlendFunci")]
		public static BlendFunci BlendFunci;
		[MapsTo("glBlendFuncSeparate")]
		public static BlendFuncSeparate BlendFuncSeparate;*/
		[MapsTo("glGetStringi")]
		static GetStringi _getStringi;
		public static string GetString(int name, int index)
		{
			var ptr = _getStringi(name, index);
			return Marshal.PtrToStringAnsi(ptr);
		}

        [MapsTo("glGetString")]
        static GetString _getString;
        public static string GetString(int name)
        {
            var ptr = _getString(name);
            return Marshal.PtrToStringAnsi(ptr);
        }
        [MapsTo("glPolygonMode")]
		public static PolygonMode PolygonMode;
		[MapsTo("glLineWidth")]
		public static LineWidth LineWidth;
		[MapsTo("glDepthFunc")]
		public static DepthFunc DepthFunc;
		[MapsTo("glCullFace")]
		public static CullFace CullFace;
		[MapsTo("glPixelStorei")]
		public static PixelStorei PixelStorei;
		[MapsTo("glDepthMask")]
		public static DepthMask DepthMask;
        [MapsTo("glColorMask")]
        public static ColorMask ColorMask;
		[MapsTo("glPolygonOffset")]
		public static PolygonOffset PolygonOffset;
		//[MapsTo("glAlphaFunc")]
		//public static AlphaFunc AlphaFunc;
		//Textures
		[MapsTo("glGenTextures")]
		public static GenTextures GenTextures;
		public static uint GenTexture()
		{
			uint tex;
			GenTextures (1, out tex);
			return tex;
		}
		[MapsTo("glDeleteTextures")]
		public static DeleteTextures DeleteTextures;
		public static void DeleteTexture(uint texture)
		{
			DeleteTextures (1, ref texture);
		}
		[MapsTo("glActiveTexture")]
		public static ActiveTexture ActiveTexture;
		[MapsTo("glTexParameteri")]
		public static TexParameteri TexParameteri;
        [MapsTo("glTexParameterf")]
        public static TexParameterf TexParameterf;
		[MapsTo("glTexParameterfv")]
		public static TexParameterfv TexParameterfv;
		[MapsTo("glBindTexture")]
		public static BindTexture BindTexture;
		[MapsTo("glTexImage2D")]
		public static TexImage2D TexImage2D;
		[MapsTo("glTexImage2DMultisample")]
		public static TexImage2DMultisample TexImage2DMultisample;
		[MapsTo("glTexSubImage2D")]
		public static TexSubImage2D TexSubImage2D;
		[MapsTo("glCompressedTexImage2D")]
		public static CompressedTexImage2D CompressedTexImage2D;
		[MapsTo("glGetTexImage")]
		public static GetTexImage GetTexImage;
		//Shaders
		[MapsTo("glDispatchCompute")]
		public static DispatchCompute DispatchCompute;
		[MapsTo("glCreateShader")]
		public static CreateShader CreateShader;
		[MapsTo("glCompileShader")]
		public static CompileShader CompileShader;
		[MapsTo("glGetShaderiv")]
		public static GetShaderiv GetShaderiv;
		[MapsTo("glGetProgramiv")]
		public static GetProgramiv GetProgramiv;
		[MapsTo("glGetShaderInfoLog")]
		static GetShaderInfoLog _shaderInfoLog;
		public static string GetShaderInfoLog(uint shader)
		{
			int len;
			var ptr = Marshal.AllocHGlobal (4096);
			_shaderInfoLog (shader, 4096, out len, ptr);
			var str = Marshal.PtrToStringAnsi (ptr, len);
			Marshal.FreeHGlobal (ptr);
			return str;
		}
		[MapsTo("glGetProgramInfoLog")]
		static GetProgramInfoLog _programInfoLog;
		public static string GetProgramInfoLog(uint program)
		{
			int len;
			var ptr = Marshal.AllocHGlobal (4096);
			_programInfoLog (program, 4096, out len, ptr);
			var str = Marshal.PtrToStringAnsi (ptr, len);
			Marshal.FreeHGlobal (ptr);
			return str;
		}
		[MapsTo("glCreateProgram")]
		public static CreateProgram CreateProgram;
		[MapsTo("glAttachShader")]
		public static AttachShader AttachShader;
		[MapsTo("glBindAttribLocation")]
		public static BindAttribLocation BindAttribLocation;
		//[MapsTo("glBindFragDataLocation")]
		//public static BindFragDataLocation BindFragDataLocation;
		[MapsTo("glGetUniformLocation")]
		public static GetUniformLocation GetUniformLocation;
		[MapsTo("glUniform1i")]
		public static Uniform1i Uniform1i;
		[MapsTo("glUniform1f")]
		public static Uniform1f Uniform1f;
		[MapsTo("glUniform2i")]
		public static Uniform2i Uniform2i;
		[MapsTo("glUniform2f")]
		public static Uniform2f Uniform2f;
		[MapsTo("glUniform3f")]
		public static Uniform3f Uniform3f;
		[MapsTo("glUniform4f")]
		public static Uniform4f Uniform4f;
        [MapsTo("glUniform4i")]
        public static Uniform4i Uniform4i;
		[MapsTo("glUniformMatrix4fv")]
		public static UniformMatrix4fv UniformMatrix4fv;
		[MapsTo("glLinkProgram")]
		public static LinkProgram LinkProgram;
		[MapsTo("glUseProgram")]
		public static UseProgram UseProgram;
		[MapsTo("glShaderSource")]
		public static ShaderSource _shaderSource;
		public static unsafe void ShaderSource(uint shader, string s)
		{
			var bytes = new byte[s.Length + 1];
			Encoding.ASCII.GetBytes (s, 0, s.Length, bytes, 0);
			bytes [s.Length] = 0;
			int len = s.Length;
			fixed(byte* ptr = bytes) {
				var intptr = (IntPtr)ptr;
				_shaderSource (shader, 1, ref intptr, IntPtr.Zero);
			}
		}
		//Buffers
		[MapsTo("glGenBuffers")]
		public static GenBuffers GenBuffers;
		public static uint GenBuffer()
		{
			uint buf;
			GenBuffers (1, out buf);
			return buf;
		}
		[MapsTo("glDeleteBuffers")]
		public static DeleteBuffers DeleteBuffers;
		public static void DeleteBuffer(uint buffer)
		{
			DeleteBuffers (1, ref buffer);
		}
		[MapsTo("glBindBuffer")]
		public static BindBuffer BindBuffer;
		[MapsTo("glBufferData")]
		public static BufferData BufferData;
		[MapsTo("glBufferSubData")]
		public static BufferSubData BufferSubData;
		[MapsTo("glGenVertexArrays")]
		public static GenVertexArrays GenVertexArrays;
		[MapsTo("glBindVertexArray")]
		public static BindVertexArray BindVertexArray;
		[MapsTo("glDeleteVertexArrays")]
		public static DeleteVertexArrays DeleteVertexArrays;
		public static void DeleteVertexArray(uint buffer)
		{
			DeleteVertexArrays (1, ref buffer);
		}
		[MapsTo("glEnableVertexAttribArray")]
		public static EnableVertexAttribArray EnableVertexAttribArray;
		[MapsTo("glVertexAttribPointer")]
		public static VertexAttribPointer VertexAttribPointer;
		[MapsTo("glDrawBuffers")]
		static DrawBuffers _DrawBuffers;
		public static unsafe void DrawBuffers(int[] buffers)
		{
			fixed(int* ptr = buffers)
			{
				_DrawBuffers(buffers.Length, (IntPtr)ptr);
			}
		}
		[MapsTo("glDrawBuffer")]
		public static DrawBuffer DrawBuffer;
		[MapsTo("glMapBuffer")]
		public static MapBuffer MapBuffer;
		[MapsTo("glUnmapBuffer")]
		public static UnmapBuffer UnmapBuffer;
		[MapsTo("glBindBufferBase")]
		public static BindBufferBase BindBufferBase;
		[MapsTo("glMemoryBarrier")]
		public static MemoryBarrier MemoryBarrier;
		//Drawing
		[MapsTo("glDrawElements")]
		public static DrawElements DrawElements;
		[MapsTo("glDrawArrays")]
		public static DrawArrays DrawArrays;
		[MapsTo("glDrawElementsBaseVertex")]
		public static DrawElementsBaseVertex DrawElementsBaseVertex;
		//Framebuffers
		[MapsTo("glGenFramebuffers")]
		public static GenFramebuffers GenFramebuffers;
		public static uint GenFramebuffer()
		{
			uint fbo;
			GenFramebuffers (1, out fbo);
			return fbo;
		}
		[MapsTo("glCheckFramebufferStatus")]
		public static CheckFramebufferStatus CheckFramebufferStatus;
		[MapsTo("glBindFramebuffer")]
		public static BindFramebuffer BindFramebuffer;
		[MapsTo("glBlitFramebuffer")]
		public static BlitFramebuffer BlitFramebuffer;
		[MapsTo("glGenRenderbuffers")]
		public static GenRenderbuffers GenRenderbuffers;
		public static uint GenRenderbuffer()
		{
			uint rbo;
			GenRenderbuffers (1, out rbo);
			return rbo;
		}
		[MapsTo("glBindRenderbuffer")]
		public static BindRenderbuffer BindRenderbuffer;
		[MapsTo("glRenderbufferStorage")]
		public static RenderbufferStorage RenderbufferStorage;
		[MapsTo("glRenderbufferStorageMultisample")]
		public static RenderbufferStorageMultisample RenderbufferStorageMultisample;
		[MapsTo("glFramebufferRenderbuffer")]
		public static FramebufferRenderbuffer FramebufferRenderbuffer;
		[MapsTo("glFramebufferTexture2D")]
		public static FramebufferTexture2D FramebufferTexture2D;
		[MapsTo("glDeleteFramebuffers")]
		public static DeleteFramebuffers DeleteFramebuffers;
		public static void DeleteFramebuffer(uint framebuffer)
		{
			DeleteFramebuffers (1, ref framebuffer);
		}
		[MapsTo("glDeleteRenderbuffers")]
		public static DeleteRenderbuffers DeleteRenderbuffers;
		public static void DeleteRenderbuffer(uint renderbuffer)
		{
			DeleteRenderbuffers (1, ref renderbuffer);
		}
		[MapsTo("glReadBuffer")]
		public static ReadBuffer ReadBuffer;
		[MapsTo("glReadPixels")]
		public static ReadPixels ReadPixels;

        public static bool GLES = true;
		static Dictionary<int, string> errors;
        public static bool ErrorChecking = false;

		public static void LoadSDL()
		{
            GLES = false;
            Load((f, t) =>
            {
                var proc = SDL.SDL_GL_GetProcAddress(f);
                if (proc == IntPtr.Zero) return null;
                return Marshal.GetDelegateForFunctionPointer(proc, (t));
            });
		}
        public static bool CheckStringSDL()
        {
            _getString = (GetString)Marshal.GetDelegateForFunctionPointer(SDL.SDL_GL_GetProcAddress("glGetString"), typeof(GetString));
            var str = GetString(GL.GL_VERSION);
            FLLog.Info("GL", "Version String: " + GetString(GL.GL_VERSION));
            var major = int.Parse(str[0].ToString());
            return major >= 3;
        }
        public static void Load(Func<string,Type,Delegate> getprocaddress)
        {
            errors = new Dictionary<int, string>();
            errors.Add(0x0500, "Invalid Enum");
            errors.Add(0x0501, "Invalid Value");
            errors.Add(0x0502, "Invalid Operation");
            errors.Add(0x0503, "Stack Overflow");
            errors.Add(0x0504, "Stack Underflow");
            errors.Add(0x0505, "Out Of Memory");
            errors.Add(0x0506, "Invalid Framebuffer Operation");

            int loaded = 0;
            foreach (var f in typeof(GL).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string proc = null;
                foreach (var attr in f.CustomAttributes)
                {
                    if (attr.AttributeType == typeof(MapsToAttribute))
                    {
                        proc = (string)attr.ConstructorArguments[0].Value;
                    }
                }
                if (proc == null)
                    continue;
                //var del = Marshal.GetDelegateForFunctionPointer(getprocaddress(proc), f.FieldType);
                var del = getprocaddress(proc, f.FieldType);
                if (proc != "glGetError" && del != null)
                    del = MakeWrapper(f.FieldType, del);
                f.SetValue(null, del);
                loaded++;
            }
            FLLog.Info("OpenGL", "Loaded " + loaded + " function pointers");
        }
		static bool _isMono = Type.GetType("Mono.Runtime") != null;
		static Delegate MakeWrapper(Type t, Delegate del)
		{
			var mi = del.Method;
			var pm = mi.GetParameters ().Select ((x) => Expression.Parameter (x.ParameterType, x.Name)).ToList ();
			var checkerr = typeof(GL).GetMethod ("CheckErrors", BindingFlags.Static | BindingFlags.Public);
			Expression body;
			if (mi.ReturnType.FullName != "System.Void") {
				var variable = Expression.Variable (mi.ReturnType, "__returnvalue");
				MethodCallExpression delegateCall;
				if (_isMono)
					delegateCall = Expression.Call(mi, pm);
				else
					delegateCall = Expression.Call(Expression.Constant(del), mi, pm);
				body = Expression.Block (
					new [] { variable },
					Expression.Assign (variable, delegateCall),
					Expression.Call (null, checkerr),
					variable
				);
			} else {
				MethodCallExpression a;
				if (_isMono || mi.IsStatic) //MethodInfo is static on mono.
					a = Expression.Call(mi, pm);
				else
					a = Expression.Call(Expression.Constant(del), mi, pm);
                var b = Expression.Call(null, checkerr);
                body = Expression.Block (
					a,b
				);
			}
			return Expression.Lambda (t, body, pm).Compile ();
		}
		[MapsTo("glGetError")]
		static GetError GetError;

		public static void CheckErrors()
		{
            if (ErrorChecking)
            {
                var err = GetError();
                if (err != 0)
                {
                    string str;
                    if (!errors.TryGetValue(err, out str))
                        str = "Unknown Error";
                    throw new Exception("GL Error: " + str);
                }
            }
		}

		public static bool FrameHadErrors()
		{
			bool hasErrors = false;
			var err = GetError ();
			while (err != 0) {
				hasErrors = true;
				err = GetError ();
			}
			return hasErrors;
		}

	}
}

