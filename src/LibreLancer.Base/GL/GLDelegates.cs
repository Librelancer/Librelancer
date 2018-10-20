// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Security;
using System.Runtime.InteropServices;
namespace LibreLancer.GLDelegates
{
	public class MapsToAttribute : Attribute
	{
		public string Target;
		public MapsToAttribute(string target)
		{
			Target = target;
		}
	}
	//General state
    [SuppressUnmanagedCodeSecurity]
	public delegate void Enable(int flags);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Disable(int flags);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GetIntegerv(int val, out int param);
    [SuppressUnmanagedCodeSecurity]
    public delegate IntPtr GetStringi(int name, int index);
    [SuppressUnmanagedCodeSecurity]
    public delegate IntPtr GetString(int name);
    [SuppressUnmanagedCodeSecurity]
    public delegate void ClearColor(float r, float g, float b, float a);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Clear(int flags);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Viewport(int x, int y, int width, int height);
	[SuppressUnmanagedCodeSecurity]
	public delegate void Scissor(int x, int y, int width, int height);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BlendFunc(int sfactor, int dfactor);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BlendFunci(int index, int sfactor, int dfactor);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BlendFuncSeparate(int srcRGB, int drcRGB, int srcAlpha, int dstAlpha);
    [SuppressUnmanagedCodeSecurity]
    public delegate void PolygonMode(int faces, int mode);
	[SuppressUnmanagedCodeSecurity]
	public delegate void LineWidth(float width);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DepthFunc(int func);
    [SuppressUnmanagedCodeSecurity]
    public delegate void CullFace(int face);
    [SuppressUnmanagedCodeSecurity]
    public delegate void PixelStorei(int pname, int param);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DepthMask(bool flag);
    [SuppressUnmanagedCodeSecurity]
    public delegate void ColorMask(bool r, bool g, bool b, bool a);
    [SuppressUnmanagedCodeSecurity]
    public delegate void AlphaFunc(int func, float _ref);
	[SuppressUnmanagedCodeSecurity]
	public delegate void PolygonOffset(float factor, float units);
    //Textures
    [SuppressUnmanagedCodeSecurity]
    public delegate void GenTextures(int n, out uint textures);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DeleteTextures(int n, ref uint textures);
    [SuppressUnmanagedCodeSecurity]
    public delegate void TexParameteri(int target, int pname, int param);
    [SuppressUnmanagedCodeSecurity]
    public delegate void TexParameterf(int target, int pname, float param);
	[SuppressUnmanagedCodeSecurity]
	public delegate void TexParameterfv(int target, int pname, ref Vector4 param);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindTexture(int target, uint id);
    [SuppressUnmanagedCodeSecurity]
    public delegate void ActiveTexture(int unit);
    [SuppressUnmanagedCodeSecurity]
    public delegate void TexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr data);
	[SuppressUnmanagedCodeSecurity]
	public delegate void TexImage2DMultisample(int target, int samples, int internalFormat, int width, int height, bool fixedsamplelocations);
    [SuppressUnmanagedCodeSecurity]
    public delegate void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    public delegate void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, IntPtr data);
	[SuppressUnmanagedCodeSecurity]
	public delegate void GetTexImage(int target, int level, int format, int type, IntPtr pixels);
	//Shaders
	[SuppressUnmanagedCodeSecurity]
	public delegate void DispatchCompute(uint num_groups_x, uint num_groups_y, uint num_groups_z);
    [SuppressUnmanagedCodeSecurity]
    public delegate uint CreateShader(int shaderType);
    [SuppressUnmanagedCodeSecurity]
    public delegate void ShaderSource(uint shader, int count, ref IntPtr str, IntPtr length);
    [SuppressUnmanagedCodeSecurity]
    public delegate void CompileShader(uint shader);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GetShaderInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GetProgramInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
    [SuppressUnmanagedCodeSecurity]
    public delegate uint CreateProgram();
    [SuppressUnmanagedCodeSecurity]
    public delegate void AttachShader(uint program, uint shader);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindAttribLocation(uint program, uint index, string name);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindFragDataLocation(uint program, uint colorNumber, string name);
    [SuppressUnmanagedCodeSecurity]
    public delegate void LinkProgram(uint program);
    [SuppressUnmanagedCodeSecurity]
    public delegate void UseProgram(uint program);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GetShaderiv(uint shader, int pname, out int param);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GetProgramiv(uint program, int pname, out int param);
    [SuppressUnmanagedCodeSecurity]
    public delegate int GetUniformLocation(uint program, string name);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform1i(int location, int v0);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform1f(int location, float v0);
	[SuppressUnmanagedCodeSecurity]
	public delegate void Uniform2i(int location, int v1, int v2);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform2f(int location, float v0, float v1);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform3f(int location, float v0, float v1, float v2);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform4f(int location, float v0, float v1, float v2, float v3);
    [SuppressUnmanagedCodeSecurity]
    public delegate void Uniform4i(int location, int v0, int v1, int v2, int v3);
    [SuppressUnmanagedCodeSecurity]
    public delegate void UniformMatrix4fv(int location, int count, bool transpose, ref Matrix4 value);
    //Buffers
    [SuppressUnmanagedCodeSecurity]
    public delegate void GenBuffers(int n, out uint buffers);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DeleteBuffers(int n, ref uint id);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindBuffer(int target, uint id);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BufferData(int target, IntPtr size, IntPtr data, int usage);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BufferSubData(int target, IntPtr offset, IntPtr size, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GenVertexArrays(int n, out uint arrays);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DeleteVertexArrays(int n, ref uint id);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindVertexArray(uint array);
    [SuppressUnmanagedCodeSecurity]
    public delegate void EnableVertexAttribArray(int index);
    [SuppressUnmanagedCodeSecurity]
    public delegate void VertexAttribPointer(uint index, int size, int type, bool normalized, int stride, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DrawBuffers(int n, IntPtr bufs);
	[SuppressUnmanagedCodeSecurity]
	public delegate void DrawBuffer(int buf);
	[SuppressUnmanagedCodeSecurity]
	public delegate IntPtr MapBuffer(uint target, uint access);
	[SuppressUnmanagedCodeSecurity]
	public delegate bool UnmapBuffer(uint target);
	[SuppressUnmanagedCodeSecurity]
	public delegate void BindBufferBase(uint target, uint index, uint buffer);
	[SuppressUnmanagedCodeSecurity]
	public delegate void MemoryBarrier(int barriers);
    //Drawing
    [SuppressUnmanagedCodeSecurity]
    public delegate void DrawElements(int mode, int count, int type, IntPtr indices);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DrawArrays(int mode, int first, int count);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DrawElementsBaseVertex(int mode, int count, int type, IntPtr indices, int basevertex);
    //Framebuffers
    [SuppressUnmanagedCodeSecurity]
    public delegate void GenFramebuffers(int n, out uint framebuffers);
	[SuppressUnmanagedCodeSecurity]
	public delegate int CheckFramebufferStatus(int target);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindFramebuffer(int target, uint framebuffer);
	[SuppressUnmanagedCodeSecurity]
	public delegate void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter);
    [SuppressUnmanagedCodeSecurity]
    public delegate void GenRenderbuffers(int n, out uint renderbuffers);
    [SuppressUnmanagedCodeSecurity]
    public delegate void BindRenderbuffer(int target, uint renderbuffer);
    [SuppressUnmanagedCodeSecurity]
    public delegate void RenderbufferStorage(int target, int internalformat, int width, int height);
	[SuppressUnmanagedCodeSecurity]
	public delegate void RenderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height);
    [SuppressUnmanagedCodeSecurity]
    public delegate void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, uint renderbuffer);
    [SuppressUnmanagedCodeSecurity]
    public delegate void FramebufferTexture2D(int target, int attachment, int textarget, uint texture, int level);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DeleteFramebuffers(int n, ref uint framebuffers);
    [SuppressUnmanagedCodeSecurity]
    public delegate void DeleteRenderbuffers(int n, ref uint renderbuffers);
	[SuppressUnmanagedCodeSecurity]
	public delegate void ReadBuffer(int buffer);
	[SuppressUnmanagedCodeSecurity]
	public delegate void ReadPixels(int x, int y, int width, int height, int format, int type, IntPtr data);
    [SuppressUnmanagedCodeSecurity]
    public delegate int GetError();
}

