// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibreLancer.Graphics.Backends.OpenGL
{
    class GLShader : IShader
    {
        const int MAX_UNIFORM_LOC = 280;
        uint programID = 0;
        private GLRenderContext context;

        record struct GLBindingInfo(int Location, string Identifier);

        record struct UniformBlockDescription(int Location, int SizeBytes, bool Integer, string Identifier);

        record struct GLStorageBuffer(int Location, int MaxElements, string Identifier);

        struct UniformBlock
        {
            public int GLLocation;
            public IntPtr BlockSet;
            public IntPtr BlockCurrent;
            public int VersionSet;
            public int Version;
            public int SizeInBytes;
            public int ForceSize;
            public bool ForceUpdate;
        }

        private NativeBuffer uniformMemoryBuffer;

        private int blocksSet = 0;
        private int blocksInteger = 0;
        private UniformBlock[] blocks;
        private ulong[] tags;

        public unsafe GLShader(GLRenderContext context, ReadOnlySpan<byte> program)
        {
            this.context = context;

            Span<byte> sig = stackalloc byte[4];
            var reader = new SpanReader(ShaderBytecodes.GetGLSL(program));
            reader.Span.Slice(reader.Offset, sig.Length).CopyTo(sig);
            reader.Offset += 4;
            if (!sig.SequenceEqual("\0GL\0"u8))
                throw new Exception("GLSL shader corrupt");
            var inputs = new GLBindingInfo[reader.ReadVarUInt32()];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = new((int)reader.ReadVarUInt32(), reader.ReadUTF8());
            }
            var textures = new GLBindingInfo[reader.ReadVarUInt32()];
            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = new((int)reader.ReadVarUInt32(), reader.ReadUTF8());
            }
            var buffers = new GLStorageBuffer[reader.ReadVarUInt32()];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new GLStorageBuffer((int)reader.ReadVarUInt32(), (int)reader.ReadVarUInt32(),
                    reader.ReadUTF8());
            }
            var uniforms = new UniformBlockDescription[reader.ReadVarUInt32()];
            for (int i = 0; i < uniforms.Length; i++)
            {
                uniforms[i] = new UniformBlockDescription((int)reader.ReadVarUInt32(), (int)reader.ReadVarUInt32(), reader.ReadBoolean(), reader.ReadUTF8());
            }

            string vertexSource = reader.ReadUTF8();
            string fragmentSource = reader.ReadUTF8();
            var version =
                GL.GLES ? "#version 300 es\nprecision highp float;\nprecision highp int;\n" : "#version 140\n";
            //compile shaders
            var vertexHandle = GL.CreateShader (GL.GL_VERTEX_SHADER);
            var fragmentHandle = GL.CreateShader (GL.GL_FRAGMENT_SHADER);
            GL.ShaderSource(vertexHandle, version + vertexSource);
            GL.ShaderSource(fragmentHandle, version + fragmentSource);
            GL.CompileShader(vertexHandle);
            int status;
            GL.GetShaderiv (vertexHandle, GL.GL_COMPILE_STATUS, out status);
            if (status == 0) {
                Console.WriteLine (GL.GetShaderInfoLog (vertexHandle));
                throw new Exception ("Vertex shader compilation failed");
            }
            GL.CompileShader(fragmentHandle);
            GL.GetShaderiv (fragmentHandle, GL.GL_COMPILE_STATUS, out status);
            if (status == 0) {
                Console.WriteLine (GL.GetShaderInfoLog (fragmentHandle));

                throw new Exception ("Fragment shader compilation failed");
            }
            programID = GL.CreateProgram();
            GL.AttachShader(programID, vertexHandle);
            GL.AttachShader(programID, fragmentHandle);
            //bind locations
            foreach (var binding in inputs)
            {
                GL.BindAttribLocation(programID, (uint)binding.Location, binding.Identifier);
            }
            //link
            GL.LinkProgram(programID);
            GL.GetProgramiv (programID, GL.GL_LINK_STATUS, out status);
            if (status == 0) {
                Console.WriteLine (GL.GetProgramInfoLog (programID));
                throw new Exception ("Program link failed");
            }
            // Set up uniforms
            int totalSz = 0;
            int blockMax = 0;
            foreach (var uniform in uniforms)
            {
                var loc = GetLocation(uniform.Identifier);
                if (loc == -1) // Blocks can be eliminated by implementation
                {
                    FLLog.Debug("Shader", $"Block removed {uniform.Identifier}");
                    continue;
                }
                blockMax = Math.Max(blockMax, uniform.Location + 1);
                blocksSet |= (1 << uniform.Location);
            }
            blocks = new UniformBlock[blockMax];
            tags = new ulong[blockMax];

            // Array indices can be eliminated by the GLSL compiler, check new lengths
            foreach (var uniform in uniforms)
            {
                if ((blocksSet & (1 << uniform.Location)) == 0)
                    continue;
                int length = uniform.SizeBytes / 16;
                for (int i = 0; i < length; i++)
                {
                    // Foolproof. There is a GL call to get the length of an array,
                    // but it has been reported online as unreliable on some old drivers.
                    var identifier = $"{uniform.Identifier}[{i}]";
                    if (GetLocation(identifier) == -1)
                    {
                        FLLog.Debug("Shader", $"{uniform.Identifier}[{uniform.SizeBytes / 16}] => {identifier}");
                        length = i;
                        break;
                    }
                }
                blocks[uniform.Location].GLLocation =  GetLocation(uniform.Identifier);
                blocks[uniform.Location].SizeInBytes = length * 16;
                if (uniform.Integer)
                {
                    blocksInteger |= (1 << uniform.Location);
                }
                // alloc staging + actual buffer
                totalSz += (length * 16) * 2;
            }
            uniformMemoryBuffer = UnsafeHelpers.Allocate(totalSz);
            var uniformMemory = (IntPtr)uniformMemoryBuffer;
            int memStart = 0;
            Unsafe.InitBlockUnaligned((void*)uniformMemory, 0, (uint)totalSz);
            //uniforms
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].BlockSet = uniformMemory + memStart;
                blocks[i].BlockCurrent = uniformMemory + memStart + blocks[i].SizeInBytes;
                memStart += blocks[i].SizeInBytes * 2;
            }

            // Bind Textures
            context.ApplyShader(this);
            foreach (var binding in textures)
            {
                GL.Uniform1i(GetLocation(binding.Identifier), binding.Location);
            }

            // Bind storage
            foreach (var buf in buffers)
            {
                var index = GL.GetUniformBlockIndex(programID, buf.Identifier);
                if (index != GL.GL_INVALID_INDEX)
                {
                    GL.UniformBlockBinding(programID, (uint)index, (uint)buf.Location);
                }
            }
        }

        public unsafe void SetUniformBlock<T>(int index, ref T data, bool forceUpdate = false, int forceSize = -1) where T : unmanaged
        {
            if ((blocksSet & 1 << index) == 0)
                return;
            var dst = new Span<byte>((void*)blocks[index].BlockCurrent, blocks[index].SizeInBytes);
            var src = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1));
            if (forceSize > -1)
            {
                src = src.Slice(0, Math.Min(forceSize, dst.Length));
                blocks[index].ForceSize = Math.Min(forceSize, dst.Length);
            }
            if (forceUpdate)
            {
                blocks[index].ForceUpdate = true;
            }
            blocks[index].Version++;
            if (src.Length > dst.Length)
            {
                src.Slice(0, dst.Length).CopyTo(dst);
            }
            else
            {
                src.CopyTo(dst);
            }
        }

        public bool HasUniformBlock(int index) => index  >= 0 && index <= 31 && (blocksSet & 1 << index) != 0;
        public ref ulong UniformBlockTag(int index) => ref tags[index];

        int GetLocation(string name)
		{
			return GL.GetUniformLocation(programID, name);
		}

        public unsafe void UseProgram()
        {
            GLBind.UseProgram(programID);
            for (int i = 0; i < blocks.Length; i++)
            {
                if ((blocksSet & (1 << i)) == 0) //Block does not exist (array)
                    continue;
                ref var b = ref blocks[i];
                if (!b.ForceUpdate && b.Version == b.VersionSet) // SetUniformBlock<T> not called
                    continue;
                b.VersionSet = b.Version;
                int size = b.ForceSize > 0 ? b.ForceSize : b.SizeInBytes;
                b.ForceSize = 0;
                var blockSet = new Span<byte>((void*)b.BlockSet, size);
                var blockCurrent = new Span<byte>((void*)b.BlockCurrent, size);
                if (!b.ForceUpdate && blockSet.SequenceEqual(blockCurrent))
                    continue;
                blockCurrent.CopyTo(blockSet);
                if ((blocksInteger & (1 << i)) != 0)
                {
                    GL.Uniform4iv(b.GLLocation, size / 16, b.BlockSet);
                }
                else
                {
                    GL.Uniform4fv(b.GLLocation, size / 16, b.BlockSet);
                }
            }
        }
    }
}
