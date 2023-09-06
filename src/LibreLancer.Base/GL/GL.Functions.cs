﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

#pragma warning disable 0649
namespace LibreLancer
{
    static unsafe partial class GL
	{
		
		public static unsafe void ShaderSource(uint shader, string s)
		{
			var bytes = new byte[s.Length + 1];
			Encoding.ASCII.GetBytes (s, 0, s.Length, bytes, 0);
			bytes [s.Length] = 0;
			int len = s.Length;
			fixed(byte* ptr = bytes) {
				var intptr = (IntPtr)ptr;
				_glShaderSource (shader, 1,  &intptr, IntPtr.Zero);
			}
		}
		
		public static uint GenBuffer()
		{
			uint buf;
			GenBuffers (1, out buf);
			return buf;
		}
		
		public static void DeleteBuffer(uint buffer)
		{
			DeleteBuffers (1, ref buffer);
		}
        public static void DeleteVertexArray(uint buffer)
		{
			DeleteVertexArrays (1, ref buffer);
		}
		
        public static unsafe void DrawBuffer(int buffer)
        {
            if (GL.GLES)
                _glDrawBuffers(1, &buffer);
            else
                _glDrawBuffer(buffer);
        }
        public static uint GenFramebuffer()
		{
			uint fbo;
			GenFramebuffers (1, out fbo);
			return fbo;
		}
        public static uint GenRenderbuffer()
		{
			uint rbo;
			GenRenderbuffers (1, out rbo);
			return rbo;
		}
        public static void DeleteFramebuffer(uint framebuffer)
		{
			DeleteFramebuffers (1, ref framebuffer);
		}
		
		public static void DeleteRenderbuffer(uint renderbuffer)
		{
			DeleteRenderbuffers (1, ref renderbuffer);
		}
        
        public static uint GenTexture()
        {
            uint tex;
            GenTextures(1, out tex);
            return tex;
        }

        public static void DeleteTexture(uint tex)
        {
            DeleteTextures(1, ref tex);
        }
        
		
        [SuppressUnmanagedCodeSecurity]
        public delegate void GlDebugProcKHR(int source, int type, uint id, int severity, int length, IntPtr message,
            IntPtr userparam);
        
        public static bool GLES = false;
		static Dictionary<int, string> errors;
        public static bool ErrorChecking = false;
        public static void LoadSDL()
		{
            tid = Thread.CurrentThread.ManagedThreadId;
            errors = new Dictionary<int, string>();
            errors.Add(0x0500, "Invalid Enum");
            errors.Add(0x0501, "Invalid Value");
            errors.Add(0x0502, "Invalid Operation");
            errors.Add(0x0503, "Stack Overflow");
            errors.Add(0x0504, "Stack Underflow");
            errors.Add(0x0505, "Out Of Memory");
            errors.Add(0x0506, "Invalid Framebuffer Operation");
            Load(SDL.SDL_GL_GetProcAddress, GLES);
            if (GLExtensions.DebugInfo)
            {
                Enable(GL_DEBUG_OUTPUT_KHR);
                DebugMessageControl(GL_DEBUG_SOURCE_SHADER_COMPILER, GL_DONT_CARE, GL_DONT_CARE, 0, IntPtr.Zero, false);
                DebugMessageControl(GL_DEBUG_SOURCE_OTHER, GL_DONT_CARE, GL_DONT_CARE, 0, IntPtr.Zero, false);
                DebugMessageControl(GL_DONT_CARE, GL_DEBUG_TYPE_PERFORMANCE, GL_DONT_CARE, 0, IntPtr.Zero, false);
                DebugMessageControl(GL_DONT_CARE, GL_DONT_CARE, GL_DEBUG_SEVERITY_LOW, 0, IntPtr.Zero, false);
                _glDebugMessageCallback(Marshal.GetFunctionPointerForDelegate(DebugCallback), IntPtr.Zero);
            }
		}

        private static GlDebugProcKHR DebugCallback = DebugCallbackHandler;
        static void DebugCallbackHandler(int source, int type, uint id, int severity, int length, IntPtr message,
            IntPtr userparam)
        {
            //higher severity = lower enum value (why khronos?)
            if (type == GL_DEBUG_TYPE_ERROR && 
                severity < GL_DEBUG_SEVERITY_LOW) {
                FLLog.Info("GL_KHR_debug", $"{Marshal.PtrToStringUTF8(message)}");
            }
            else {
                FLLog.Debug("GL_KHR_debug", $"{Marshal.PtrToStringUTF8(message)}");
            }
        }
        
        
        public static bool CheckStringSDL(bool checkGles = false)
        {
            _glGetString = (delegate*unmanaged<int, IntPtr>) SDL.SDL_GL_GetProcAddress("glGetString");
            var str = GetString(GL.GL_VERSION);
            FLLog.Info("GL", "Version String: " + GetString(GL.GL_VERSION));
            if (checkGles) return str.StartsWith("OpenGL ES 3");
            var major = int.Parse(str[0].ToString());
            return major >= 3;
        }
        
        public static string GetProgramInfoLog(uint program)
        {
            int len;
            var ptr = Marshal.AllocHGlobal (4096);
            _glGetProgramInfoLog (program, 4096,  &len, ptr);
            var str = Marshal.PtrToStringAnsi (ptr, len);
            Marshal.FreeHGlobal (ptr);
            return str;
        }
        
        public static string GetShaderInfoLog(uint shader)
        {
            int len;
            var ptr = Marshal.AllocHGlobal (4096);
            _glGetShaderInfoLog (shader, 4096,  &len, ptr);
            var str = Marshal.PtrToStringAnsi (ptr, len);
            Marshal.FreeHGlobal (ptr);
            return str;
        }

        static int tid;
		public static void ErrorCheck()
		{
            if (ErrorChecking)
            {
                if (Thread.CurrentThread.ManagedThreadId != tid)
                    throw new InvalidOperationException("Called GL off the main thread");
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
#pragma warning restore 0649
