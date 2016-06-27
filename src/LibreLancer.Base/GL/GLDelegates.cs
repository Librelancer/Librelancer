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
	delegate void Enable(int flags);
	delegate void Disable(int flags);
	delegate void GetIntegerv(int val, out int param);
	delegate IntPtr GetStringi(int name, int index);
	delegate void ClearColor(float r, float g, float b, float a);
	delegate void Clear(int flags);
	delegate void Viewport(int x, int y, int width, int height);
	delegate void BlendFunc(int sfactor, int dfactor);
	delegate void PolygonMode(int faces, int mode);
	delegate void DepthFunc(int func);
	delegate void CullFace(int face);
	delegate void PixelStorei(int pname, int param);
	delegate void DepthMask(bool flag);
	//Textures
	delegate void GenTextures(int n, out uint textures);
	delegate void DeleteTextures(int n, ref uint textures);
	delegate void TexParameteri(int target, int pname, int param);
	delegate void BindTexture(int target, uint id);
	delegate void ActiveTexture(int unit);
	delegate void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data);
	delegate void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data);
	delegate void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data);
	//Shaders
	delegate uint CreateShader(int shaderType);
	delegate void ShaderSource(uint shader, int count, ref IntPtr str, IntPtr length);
	delegate void CompileShader(uint shader);
	delegate void GetShaderInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
	delegate void GetProgramInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
	delegate uint CreateProgram();
	delegate void AttachShader(uint program, uint shader);
	delegate void BindAttribLocation(uint program, uint index, string name);
	delegate void LinkProgram(uint program);
	delegate void UseProgram(uint program);
	delegate void GetShaderiv(uint shader, int pname, out int param);
	delegate void GetProgramiv(uint program, int pname, out int param);
	delegate int GetUniformLocation(uint program, string name);
	delegate void Uniform1i(int location, int v0);
	delegate void Uniform1f(int location, float v0);
	delegate void Uniform3f(int location, float v0, float v1, float v2);
	delegate void Uniform4f(int location, float v0, float v1, float v2, float v3);
	delegate void UniformMatrix4fv(int location, int count, bool transpose, IntPtr value);
	//Buffers
	delegate void GenBuffers(int n, out uint buffers);
	delegate void DeleteBuffers(int n, ref uint id);
	delegate void BindBuffer(int target, uint id);
	delegate void BufferData(int target, IntPtr size, IntPtr data, int usage);
	delegate void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data);
	delegate void GenVertexArrays(int n, out uint arrays);
	delegate void DeleteVertexArrays(int n, ref uint id);
	delegate void BindVertexArray(uint array);
	delegate void EnableVertexAttribArray(int index);
	delegate void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr data);
	//Drawing
	delegate void DrawElements(int mode, int count, int type, IntPtr indices);
	delegate void DrawArrays(int mode, int first, int count);
	delegate void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex);
	//Framebuffers
	delegate void GenFramebuffers(int n, out uint framebuffers);
	delegate void BindFramebuffer(int target, uint framebuffer);
	delegate void GenRenderbuffers(int n, out uint renderbuffers);
	delegate void BindRenderbuffer(int target, uint renderbuffer);
	delegate void RenderbufferStorage(int target, int internalformat, int width, int height);
	delegate void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer);
	delegate void FramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level);
	delegate void DeleteFramebuffers(int n, ref uint framebuffers);
	delegate void DeleteRenderbuffers(int n, ref uint renderbuffers);
	delegate int GetError();
}

