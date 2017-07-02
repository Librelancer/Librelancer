/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Runtime.InteropServices;

namespace LibreLancer
{
    class ANGLE
    {
        static ANGLE Instance;

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string name);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr library, string name);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
        const int EGL_RED_SIZE = 0x3024;
        const int EGL_GREEN_SIZE = 0x3023;
        const int EGL_BLUE_SIZE = 0x3022;
        const int EGL_ALPHA_SIZE = 0x3021;
        const int EGL_DEPTH_SIZE = 0x3025;
        const int EGL_STENCIL_SIZE = 0x3026;
        const int EGL_SAMPLE_BUFFERS = 0x3032;
        const int EGL_NONE = 0x3038;

        delegate IntPtr _eglGetPlatformDisplayEXT(int platform, IntPtr native_display, int[] attrib_list);
        _eglGetPlatformDisplayEXT eglGetPlatformDisplayEXT;

        delegate bool _eglInitialize(IntPtr dpy, out int major, out int minor);
        _eglInitialize eglInitialize;

         delegate bool _eglGetConfigs(IntPtr dpy, IntPtr configs, int config_size, out int num_config);
        _eglGetConfigs eglGetConfigs;

        delegate bool _eglChooseConfig(IntPtr dpy, int[] attrib_list, ref IntPtr config, int config_size, out int num_config);
        _eglChooseConfig eglChooseConfig;

        delegate IntPtr _eglCreateWindowSurface(IntPtr dpy, IntPtr config, IntPtr win, int[] attrib_list);
        _eglCreateWindowSurface eglCreateWindowSurface;

        delegate IntPtr _eglCreateContext(IntPtr display, IntPtr config, IntPtr share_context, int[] attrib_list);
        _eglCreateContext eglCreateContext;

        delegate bool _eglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);
        _eglMakeCurrent eglMakeCurrent;

        delegate void _eglSwapBuffers(IntPtr display, IntPtr surface);
        _eglSwapBuffers eglSwapBuffers;

        static T LoadFunction<T>(IntPtr library, string name)
        {
            return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library, name), typeof(T));
        }

        IntPtr display;
        IntPtr context;
        IntPtr surface;

        IntPtr libgles;
        public ANGLE()
        {
            var libegl = LoadLibrary(IntPtr.Size == 8 ? "win64\\libEGL.dll" : "win32\\libEGL.dll");
            libgles = LoadLibrary(IntPtr.Size == 8 ? "win64\\libGLESv2.dll" : "win32\\libGLESv2.dll");

            eglGetPlatformDisplayEXT = LoadFunction<_eglGetPlatformDisplayEXT>(libegl, "eglGetPlatformDisplayEXT");
            eglInitialize = LoadFunction<_eglInitialize>(libegl, "eglInitialize");
            eglGetConfigs = LoadFunction<_eglGetConfigs>(libegl, "eglGetConfigs");
            eglChooseConfig = LoadFunction<_eglChooseConfig>(libegl, "eglChooseConfig");
            eglCreateWindowSurface = LoadFunction<_eglCreateWindowSurface>(libegl, "eglCreateWindowSurface");
            eglCreateContext = LoadFunction<_eglCreateContext>(libegl, "eglCreateContext");
            eglMakeCurrent = LoadFunction<_eglMakeCurrent>(libegl, "eglMakeCurrent");
            eglSwapBuffers = LoadFunction<_eglSwapBuffers>(libegl, "eglSwapBuffers");
            Instance = this;
        }


        const int MAX_VAOS = 4096;
        const int MAX_ATTRIBS = 9;

        const int EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE = 0x3207;
        const int EGL_PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
        const int EGL_PLATFORM_ANGLE_ANGLE = 0x3202;

        public void CreateContext(IntPtr hWnd)
        {
            int[] configAttribList = new int[]
            {
                EGL_RED_SIZE, 8,
                EGL_GREEN_SIZE, 8,
                EGL_BLUE_SIZE, 8,
                EGL_ALPHA_SIZE, 8,
                EGL_DEPTH_SIZE, 16,
                EGL_STENCIL_SIZE, 8,
                EGL_SAMPLE_BUFFERS, 0,
                EGL_NONE, EGL_NONE
            };
            int[] surfaceAttribList = new int[]
            {
                EGL_NONE, EGL_NONE
            };
            int[] contextAttribs = new int[]
            {
                EGL_CONTEXT_CLIENT_VERSION, 2, EGL_NONE, EGL_NONE
            };
            int[] displayAttribList = new int[]
            {
                EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE,
                EGL_NONE, EGL_NONE
            };
            display = eglGetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, GetDC(hWnd), displayAttribList);
            if (display == IntPtr.Zero)
                throw new Exception("eglGetDisplay failed");

            int majorVersion, minorVersion;
            if(!eglInitialize(display, out majorVersion, out minorVersion))
                throw new Exception("eglInitialize failed");

            int numConfigs;
            if (!eglGetConfigs(display, IntPtr.Zero, 0, out numConfigs))
                throw new Exception("eglGetConfigs failed");

            IntPtr config = IntPtr.Zero;
            if (!eglChooseConfig(display, configAttribList, ref config, 1, out numConfigs))
                throw new Exception("eglChooseConfig failed");

            surface = eglCreateWindowSurface(display, config, hWnd, surfaceAttribList);
            if (surface == IntPtr.Zero)
                throw new Exception("eglCreateWindowSurface failed");

            context = eglCreateContext(display, config, IntPtr.Zero, contextAttribs);
            if (context == IntPtr.Zero)
                throw new Exception("eglCreateContext failed");

            if (!eglMakeCurrent(display, surface, surface, context))
                throw new Exception("eglMakeCurrent failed");
            
            GL.Load(GetFunction);
            //Initialise GL 3.0 emulation
            glTexImage2D = LoadFunction<GLDelegates.TexImage2D>(libgles, "glTexImage2D");
            glTexSubImage2D = LoadFunction<GLDelegates.TexSubImage2D>(libgles, "glTexSubImage2D");
            glBindVertexArrayOES = LoadFunction<GLDelegates.BindVertexArray>(libgles, "glBindVertexArrayOES");
            glVertexAttribPointer = LoadFunction<GLDelegates.VertexAttribPointer>(libgles, "glVertexAttribPointer");
            glDrawElements = LoadFunction<GLDelegates.DrawElements>(libgles, "glDrawElements");
            glDrawArrays = LoadFunction<GLDelegates.DrawArrays>(libgles, "glDrawArrays");
            vaos = new VertexArray[MAX_VAOS];
            for(int i = 0; i < MAX_VAOS; i++)
            {
                vaos[i].Attribs = new VertexAttrib[MAX_ATTRIBS];
                for (int j = 0; j < MAX_ATTRIBS; j++)
                    vaos[i].Attribs[j].Set = false;
            }
        }
        GLDelegates.TexImage2D glTexImage2D;
        GLDelegates.TexSubImage2D glTexSubImage2D;
        GLDelegates.BindVertexArray glBindVertexArrayOES;
        GLDelegates.VertexAttribPointer glVertexAttribPointer;
        GLDelegates.DrawElements glDrawElements;
        GLDelegates.DrawArrays glDrawArrays;
        public void SwapBuffers()
        {
            eglSwapBuffers(display, surface);
        }

        Delegate GetFunction(string name, Type type)
        {
            if (name == "glPolygonMode")
                return (Delegate)new GLDelegates.PolygonMode(PolygonMode);
            if (name == "glDrawElementsBaseVertex")
                return (Delegate)new GLDelegates.DrawElementsBaseVertex(DrawElementsBaseVertex);
            if (name == "glDrawElements")
                return (Delegate)new GLDelegates.DrawElements(DrawElements);
            if (name == "glVertexAttribPointer")
                return (Delegate)new GLDelegates.VertexAttribPointer(VertexAttribPointer);
            if (name == "glGenVertexArrays")
                name = "glGenVertexArraysOES";
			if (name == "glBlitFramebuffer")
				name = "glBlitFramebufferANGLE";
			if (name == "glRenderbufferStorageMultisample")
				name = "glRenderbufferStorageMultisampleANGLE";
            if (name == "glBindVertexArray")
                return (Delegate)new GLDelegates.BindVertexArray(BindVertexArray);
            if (name == "glTexImage2D")
                return (Delegate)new GLDelegates.TexImage2D(TexImage2D);
            if (name == "glTexSubImage2D")
                return (Delegate)new GLDelegates.TexSubImage2D(TexSubImage2D);
            return Marshal.GetDelegateForFunctionPointer(GetProcAddress(libgles, name), type);
        }

        static void PolygonMode(int faces, int mode)
        {
            //This function is stubbed out for now
        }

        VertexArray[] vaos;
        int currentVAO = -1;
        struct VertexArray
        {
            public int Stride;
            public int VertexOffset;
            public VertexAttrib[] Attribs;
        }
        struct VertexAttrib
        {
            public bool Set;
            public int Size;
            public int Type;
            public bool Normalized;
            public long Offset;
        }

        void SetBaseVertex(int basevertex)
        {
            if (vaos[currentVAO].VertexOffset != basevertex)
            {
                for (int i = 0; i < MAX_ATTRIBS; i++)
                {
                    var a = vaos[currentVAO].Attribs[i];
                    if (a.Set)
                    {
                        glVertexAttribPointer((uint)i, a.Size, a.Type, a.Normalized, vaos[currentVAO].Stride, new IntPtr(basevertex * vaos[currentVAO].Stride + a.Offset));
                    }
                }
                vaos[currentVAO].VertexOffset = basevertex;
            }
        }

        static void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex)
        {
            Instance.DrawBaseVertexImpl(mode, count, type, indices, basevertex);
        }
        static void DrawElements(int mode, int count, int type, IntPtr indices)
        {
            Instance.DrawElementsImpl(mode, count, type, indices);
        }

        void DrawElementsImpl(int mode, int count, int type, IntPtr indices)
        {
            glDrawElements(mode, count, type, indices);
        }
       
        void DrawBaseVertexImpl(int mode, int count, int type, IntPtr indices, int basevertex)
        {
            SetBaseVertex(basevertex);
            glDrawElements(mode, count, type, indices);
        }

        const int GL_LUMINANCE = 0x1909;
        static void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data)
        {
            if (format == GL.GL_RED && internalFormat == GL.GL_R8)
            {
                format = GL_LUMINANCE;
                internalFormat = GL_LUMINANCE;
            }
            if (format == GL.GL_BGRA && internalFormat == GL.GL_RGBA)
            {
                internalFormat = GL.GL_BGRA;
            }
            Instance.glTexImage2D(target, level, internalFormat, width, height, border, format, type, data);
        }

        static void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data)
        {
            var fmt = format;
            if (format == GL.GL_RED)
            {
                fmt = GL_LUMINANCE;
            }
            Instance.glTexSubImage2D(target, level, xoffset, yoffset, width, height, fmt, type, data);
        }

        static void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr data)
        {
            Instance.AttribPointerImpl(index, size, type, normalized, stride, data);
        }
        void AttribPointerImpl(uint index, int size, int type, bool normalized, int stride, IntPtr data)
        {
            vaos[currentVAO].VertexOffset = 0;
            vaos[currentVAO].Stride = stride;
            vaos[currentVAO].Attribs[index] = new VertexAttrib()
            {
                Set = true,
                Size = size,
                Type = type,
                Normalized = normalized,
                Offset = (long)data
            };
            glVertexAttribPointer(index, size, type, normalized, stride, data);
        }

        static void BindVertexArray(uint array)
        {
            Instance.currentVAO = (int)array;
            Instance.glBindVertexArrayOES(array);
        }
    }
}
