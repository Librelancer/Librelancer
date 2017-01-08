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
using System.Security;
using System.Runtime.InteropServices;
namespace LibreLancer.GLDelegates
{
	class MapsToAttribute : Attribute
	{
		public string Target;
		public MapsToAttribute(string target)
		{
			Target = target;
		}
	}
	//General state
    [SuppressUnmanagedCodeSecurity]
	delegate void Enable(int flags);
    [SuppressUnmanagedCodeSecurity]
    delegate void Disable(int flags);
    [SuppressUnmanagedCodeSecurity]
    delegate void GetIntegerv(int val, out int param);
    [SuppressUnmanagedCodeSecurity]
    delegate IntPtr GetStringi(int name, int index);
    [SuppressUnmanagedCodeSecurity]
    delegate IntPtr GetString(int name);
    [SuppressUnmanagedCodeSecurity]
    delegate void ClearColor(float r, float g, float b, float a);
    [SuppressUnmanagedCodeSecurity]
    delegate void Clear(int flags);
    [SuppressUnmanagedCodeSecurity]
    delegate void Viewport(int x, int y, int width, int height);
	[SuppressUnmanagedCodeSecurity]
	delegate void Scissor(int x, int y, int width, int height);
    [SuppressUnmanagedCodeSecurity]
    delegate void BlendFunc(int sfactor, int dfactor);
    [SuppressUnmanagedCodeSecurity]
    delegate void BlendFunci(int index, int sfactor, int dfactor);
    [SuppressUnmanagedCodeSecurity]
    delegate void BlendFuncSeparate(int srcRGB, int drcRGB, int srcAlpha, int dstAlpha);
    [SuppressUnmanagedCodeSecurity]
    delegate void PolygonMode(int faces, int mode);
    [SuppressUnmanagedCodeSecurity]
    delegate void DepthFunc(int func);
    [SuppressUnmanagedCodeSecurity]
    delegate void CullFace(int face);
    [SuppressUnmanagedCodeSecurity]
    delegate void PixelStorei(int pname, int param);
    [SuppressUnmanagedCodeSecurity]
    delegate void DepthMask(bool flag);
    [SuppressUnmanagedCodeSecurity]
    delegate void AlphaFunc(int func, float _ref);
    //Textures
    [SuppressUnmanagedCodeSecurity]
    delegate void GenTextures(int n, out uint textures);
    [SuppressUnmanagedCodeSecurity]
    delegate void DeleteTextures(int n, ref uint textures);
    [SuppressUnmanagedCodeSecurity]
    delegate void TexParameteri(int target, int pname, int param);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindTexture(int target, uint id);
    [SuppressUnmanagedCodeSecurity]
    delegate void ActiveTexture(int unit);
    [SuppressUnmanagedCodeSecurity]
    delegate void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    delegate void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    delegate void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data);
    //Shaders
    [SuppressUnmanagedCodeSecurity]
    delegate uint CreateShader(int shaderType);
    [SuppressUnmanagedCodeSecurity]
    delegate void ShaderSource(uint shader, int count, ref IntPtr str, IntPtr length);
    [SuppressUnmanagedCodeSecurity]
    delegate void CompileShader(uint shader);
    [SuppressUnmanagedCodeSecurity]
    delegate void GetShaderInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
    [SuppressUnmanagedCodeSecurity]
    delegate void GetProgramInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
    [SuppressUnmanagedCodeSecurity]
    delegate uint CreateProgram();
    [SuppressUnmanagedCodeSecurity]
    delegate void AttachShader(uint program, uint shader);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindAttribLocation(uint program, uint index, string name);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindFragDataLocation(uint program, uint colorNumber, string name);
    [SuppressUnmanagedCodeSecurity]
    delegate void LinkProgram(uint program);
    [SuppressUnmanagedCodeSecurity]
    delegate void UseProgram(uint program);
    [SuppressUnmanagedCodeSecurity]
    delegate void GetShaderiv(uint shader, int pname, out int param);
    [SuppressUnmanagedCodeSecurity]
    delegate void GetProgramiv(uint program, int pname, out int param);
    [SuppressUnmanagedCodeSecurity]
    delegate int GetUniformLocation(uint program, string name);
    [SuppressUnmanagedCodeSecurity]
    delegate void Uniform1i(int location, int v0);
    [SuppressUnmanagedCodeSecurity]
    delegate void Uniform1f(int location, float v0);
    [SuppressUnmanagedCodeSecurity]
    delegate void Uniform2f(int location, float v0, float v1);
    [SuppressUnmanagedCodeSecurity]
    delegate void Uniform3f(int location, float v0, float v1, float v2);
    [SuppressUnmanagedCodeSecurity]
    delegate void Uniform4f(int location, float v0, float v1, float v2, float v3);
    [SuppressUnmanagedCodeSecurity]
    delegate void UniformMatrix4fv(int location, int count, bool transpose, ref Matrix4 value);
    //Buffers
    [SuppressUnmanagedCodeSecurity]
    delegate void GenBuffers(int n, out uint buffers);
    [SuppressUnmanagedCodeSecurity]
    delegate void DeleteBuffers(int n, ref uint id);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindBuffer(int target, uint id);
    [SuppressUnmanagedCodeSecurity]
    delegate void BufferData(int target, IntPtr size, IntPtr data, int usage);
    [SuppressUnmanagedCodeSecurity]
    delegate void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    delegate void GenVertexArrays(int n, out uint arrays);
    [SuppressUnmanagedCodeSecurity]
    delegate void DeleteVertexArrays(int n, ref uint id);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindVertexArray(uint array);
    [SuppressUnmanagedCodeSecurity]
    delegate void EnableVertexAttribArray(int index);
    [SuppressUnmanagedCodeSecurity]
    delegate void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    delegate void DrawBuffers(int n, IntPtr bufs);
    //Drawing
    [SuppressUnmanagedCodeSecurity]
    delegate void DrawElements(int mode, int count, int type, IntPtr indices);
    [SuppressUnmanagedCodeSecurity]
    delegate void DrawArrays(int mode, int first, int count);
    [SuppressUnmanagedCodeSecurity]
    delegate void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex);
    //Framebuffers
    [SuppressUnmanagedCodeSecurity]
    delegate void GenFramebuffers(int n, out uint framebuffers);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindFramebuffer(int target, uint framebuffer);
    [SuppressUnmanagedCodeSecurity]
    delegate void GenRenderbuffers(int n, out uint renderbuffers);
    [SuppressUnmanagedCodeSecurity]
    delegate void BindRenderbuffer(int target, uint renderbuffer);
    [SuppressUnmanagedCodeSecurity]
    delegate void RenderbufferStorage(int target, int internalformat, int width, int height);
    [SuppressUnmanagedCodeSecurity]
    delegate void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer);
    [SuppressUnmanagedCodeSecurity]
    delegate void FramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level);
    [SuppressUnmanagedCodeSecurity]
    delegate void DeleteFramebuffers(int n, ref uint framebuffers);
    [SuppressUnmanagedCodeSecurity]
    delegate void DeleteRenderbuffers(int n, ref uint renderbuffers);
	[SuppressUnmanagedCodeSecurity]
	delegate void ReadBuffer(int buffer);
	[SuppressUnmanagedCodeSecurity]
	delegate void ReadPixels(int x, int y, int width, int height, int format, int type, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    delegate int GetError();
}

