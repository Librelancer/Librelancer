// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Graphics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render
{
	public class CommandBuffer : IDisposable
	{
		const int MAX_COMMANDS = 16384;
		const int MAX_TRANSPARENT_COMMANDS = 16384;

		//public List<RenderCommand> Commands = new List<RenderCommand>();
		//RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
		RenderCommand[] Transparents = new RenderCommand[MAX_TRANSPARENT_COMMANDS];
		int currentCommand = 0;
		int transparentCommand = 0;
        RenderContext rstate;
        public UniformBuffer BonesBuffer;
        public int BonesMax;
        public int BonesOffset;
        public WorldMatrixBuffer WorldBuffer;
        public ICamera Camera;
        public CommandBuffer(RenderContext context)
        {
            BonesBuffer = new UniformBuffer(context, 65536, 64, typeof(Matrix4x4), true);
            WorldBuffer = new WorldMatrixBuffer();
        }
        public void StartFrame(RenderContext rstate)
		{
			currentCommand = 0;
			transparentCommand = 0;
            WorldBuffer.Reset();
            this.rstate = rstate;
		}
		public void AddCommand(
            RenderMaterial material,
            MaterialAnim anim,
            WorldMatrixHandle world,
            Lighting lights,
            VertexBuffer buffer,
            PrimitiveTypes primitive,
            int baseVertex,
            int start,
            int count,
            int layer,
            float z = 0,
            DfmSkinning skinning = null,
            int offset = 0,
            int userData = 0)
		{
			if (material.IsTransparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
                    Key = RenderCommand.MakeKey(RenderType.Transparent, material.Key, layer, z, offset),
					Source = material,
					MaterialAnim = anim,
					Lights = lights,
					Buffer = buffer,
					BaseVertex = baseVertex,
					Start = start,
					Count = count,
                    Type = RenderCommand.MakeType(RenderCmdType.Material, primitive),
                    World = world,
                    Hash = userData,
                };
			}
			else
			{
                if (skinning != null)
                {
                    material.Bones = BonesBuffer;
                    material.BufferOffset = skinning.BufferOffset;
                }
                else
                {
                    material.Bones = null;
                }
                material.MaterialAnim = anim;
                material.World = world;
                material.Use(rstate, buffer.VertexType, ref lights, userData);
                if (material.DoubleSided) {
                    rstate.Cull = false;
                }
                if(baseVertex == -1)
                    buffer.Draw(primitive, start, count);
                else
                    buffer.Draw(primitive, baseVertex, start, count);
                if (material.DoubleSided) {
                    rstate.Cull = true;
                }
            }
		}
		//TODO: Implement MaterialAnim for asteroids
		public unsafe void AddCommandFade(
            RenderMaterial material,
            WorldMatrixHandle world,
            Lighting lights,
            VertexBuffer buffer,
            PrimitiveTypes primitive,
            int baseVertex,
            int start,
            int count,
            int layer,
            Vector2 fadeParams,
            float z = 0,
            int offset = 0)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
                Key = RenderCommand.MakeKey(RenderType.Transparent, material.Key, layer, z, offset),
				Source = material,
				MaterialAnim = null,
				Lights = lights,
				Buffer = buffer,
				Start = start,
				Count = count,
                Type = RenderCommand.MakeType(RenderCmdType.MaterialFade, primitive),
                BaseVertex = baseVertex,
                Hash = *(int*)(&fadeParams.X),
				Index = *(int*)(&fadeParams.Y),
				World = world,
            };
		}

        public void AddCommand(Billboards billboards, int hash, int index, int sortLayer, float z)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
                Key = RenderCommand.MakeKey(RenderType.Transparent, 0, sortLayer, z, 0),
				Type = RenderCommand.MakeType(RenderCmdType.Billboard),
				Source = billboards,
				Hash = hash,
				Index = index,
            };
		}

		public void DrawOpaque(RenderContext context)
		{
            SortTransparent();
            FillBillboards();
		}

		void FillBillboards()
		{
			for (int i = transparentCommand - 1; i >= 0; i--)
			{
				if (Transparents[cmdptr[i]].CmdType == RenderCmdType.Billboard)
				{
					var bb = (Billboards)Transparents[cmdptr[i]].Source;
					bb.FillIbo();
					break;
				}
			}
		}

		void SortTransparent()
		{
			for (int i = 0; i < transparentCommand; i++)
			{
				cmdptr[i] = i;
			}
			Array.Sort<int>(cmdptr, 0, transparentCommand, new KeyComparer(Transparents));
			for (int i = transparentCommand - 1; i >= 0; i--)
			{
				if (Transparents[cmdptr[i]].CmdType == RenderCmdType.Billboard)
				{
					var bb = (Billboards)Transparents[cmdptr[i]].Source;
					bb.AddIndices(Transparents[cmdptr[i]].Index);
				}
			}
		}

		int[] cmdptr = new int[MAX_COMMANDS];
		int transparentCount = 0;
		public void DrawTransparent(RenderContext context)
		{
		  	Billboards lastbb = null;
			for (int i = transparentCommand - 1; i >= 0; i--)
			{
				if (lastbb != null && Transparents[cmdptr[i]].CmdType != RenderCmdType.Billboard)
				{
					lastbb.FlushCommands(context);
					lastbb = null;
				}
				lastbb = (Transparents[cmdptr[i]].Source as Billboards);
				Transparents[cmdptr[i]].Run(context);
			}
			if (lastbb != null)
				lastbb.FlushCommands(context);
		}

        public void Dispose()
        {
            BonesBuffer.Dispose();
            WorldBuffer.Dispose();
        }

	}
	class KeyComparer : IComparer<int>
	{
		RenderCommand[] cmds;
		public KeyComparer(RenderCommand[] commands)
		{
			cmds = commands;
		}
		public int Compare(int x, int y)
		{
            return cmds[x].Key.CompareTo(cmds[y].Key);
		}
	}
	public enum RenderCmdType : byte
	{
		Material,
        MaterialFade,
		Billboard
	}

    public enum RenderType
    {
        Opaque,
        Starsphere,
        Transparent
    }
	public struct RenderCommand
	{
        static ulong float2index(float f)
        {
            var i = BitConverter.SingleToInt32Bits(f);
            uint mask = (uint)(-(i >> 31)) | 0x80000000;
            return (ulong) (i ^ mask);
        }
        public static ulong MakeKey(RenderType type, int matKey, int sortlayer, float z, int index)
        {
            if (type == RenderType.Transparent)
            {
                return (2UL << 62) | ((ulong) sortlayer << 54) | float2index(-z) << 22 | ((ulong) index & 0x3fffff);
            }
            else if (type == RenderType.Opaque)
            {
                return (ulong) matKey;
            }
            else
            {
                return (1UL << 62); //starsphere
            }
        }

        public static byte MakeType(RenderCmdType cmdType, PrimitiveTypes prims = PrimitiveTypes.Points)
        {
            return (byte) (((byte)cmdType << 4) | (byte) prims);
        }


        public ulong Key;
		public byte Type;
		public object Source;
        public WorldMatrixHandle World;
		public VertexBuffer Buffer;
		public int BaseVertex;
		public int Start;
		public int Count;
        public Lighting Lights;
        public MaterialAnim MaterialAnim;
		public int Hash;
		public int Index;

        public RenderCmdType CmdType => (RenderCmdType) (Type >> 4);
        public PrimitiveTypes Primitive => (PrimitiveTypes) (Type & 0xF);
		public override string ToString()
		{
			return string.Format("[Key: {0}]", Key);
		}
		public unsafe void Run(RenderContext context)
        {
            if (CmdType == RenderCmdType.Material || CmdType == RenderCmdType.MaterialFade)
			{
				var Material = (RenderMaterial)Source;
				if (Material == null)
					return;
				Material.MaterialAnim = MaterialAnim;
				Material.World = World;
				if (CmdType == RenderCmdType.MaterialFade)
				{
					Material.Fade = true;
					var fn = Hash;
					var ff = Index;
					Material.FadeNear = *(float*)(&fn);
					Material.FadeFar = *(float*)(&ff);
				}
                if (Material.DisableCull || Material.DoubleSided) context.Cull = false;
				Material.Use(context, Buffer.VertexType, ref Lights, Hash);
				if ((CmdType != RenderCmdType.MaterialFade) && BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
                if (Material.DisableCull || Material.DoubleSided) context.Cull = true;
			}
            else if (CmdType == RenderCmdType.Billboard)
			{
				var Billboards = (Billboards)Source;
				Billboards.Render(Index, Hash, context);
			}
		}
	}
}

