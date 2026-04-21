// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer.Graphics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render
{
	public class CommandBuffer : IDisposable
	{
        private const int MAX_COMMANDS = 16384;
        private const int MAX_TRANSPARENT_COMMANDS = 16384;

		// public List<RenderCommand> Commands = new List<RenderCommand>();
		// RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
        private RenderCommand[] Transparents = new RenderCommand[MAX_TRANSPARENT_COMMANDS];
        private int currentCommand = 0;
        private int transparentCommand = 0;
        private RenderContext rstate;
        public StorageBuffer BonesBuffer;
        public int BonesMax;
        public int BonesOffset;
        public WorldMatrixBuffer WorldBuffer;
        public ICamera? Camera;
        public CommandBuffer(RenderContext context)
        {
            rstate = context;
            BonesBuffer = new StorageBuffer(context, 65536, 64);
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
            MaterialAnim? anim,
            WorldMatrixHandle world,
            Lighting lights,
            VertexBuffer buffer,
            float opacity,
            PrimitiveTypes primitive,
            int baseVertex,
            int start,
            int count,
            int layer,
            float z = 0,
            DfmSkinning? skinning = null,
            int offset = 0,
            int userData = 0)
		{
            if (count == 0)
            {
                throw new ArgumentException("count cannot be 0");
            }
			if (material.IsTransparent || opacity < 1)
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
                    UserData = userData,
                    Hash = Unsafe.BitCast<float,int>(opacity)
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
		// TODO: Implement MaterialAnim for asteroids
		public void AddCommandFade(
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
            int offset = 0,
            int userData = 0)
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
                Hash = Unsafe.BitCast<float,int>(fadeParams.X),
				Index = Unsafe.BitCast<float, int>(fadeParams.Y),
				World = world,
                UserData = userData
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

        private void FillBillboards()
		{
			for (var i = transparentCommand - 1; i >= 0; i--)
			{
				if (Transparents[cmdptr[i]].CmdType == RenderCmdType.Billboard)
				{
					var bb = (Billboards)Transparents[cmdptr[i]].Source;
					bb.FillIbo();
					break;
				}
			}
		}

        private void SortTransparent()
		{
			for (var i = 0; i < transparentCommand; i++)
			{
				cmdptr[i] = i;
			}
			Array.Sort<int>(cmdptr, 0, transparentCommand, new KeyComparer(Transparents));
			for (var i = transparentCommand - 1; i >= 0; i--)
			{
				if (Transparents[cmdptr[i]].CmdType == RenderCmdType.Billboard)
				{
					var bb = (Billboards)Transparents[cmdptr[i]].Source;
					bb.AddIndices(Transparents[cmdptr[i]].Index);
				}
			}
		}

        private int[] cmdptr = new int[MAX_COMMANDS];
        private int transparentCount = 0;
		public void DrawTransparent(RenderContext context)
		{
		  	Billboards? lastbb = null;
			for (var i = transparentCommand - 1; i >= 0; i--)
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

    internal class KeyComparer : IComparer<int>
	{
        private RenderCommand[] cmds;
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
        MaterialOverride,
		Billboard
	}

    public enum RenderType
    {
        Opaque,
        Transparent,
    }
	public struct RenderCommand
	{
        private static ulong float2index(float f)
        {
            var i = BitConverter.SingleToInt32Bits(f);
            var mask = (uint)(-(i >> 31)) | 0x80000000;
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
                return (1UL << 62); // starsphere
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
        public MaterialAnim? MaterialAnim;
		public int Hash;
		public int Index;
        public int UserData;

        public RenderCmdType CmdType => (RenderCmdType) (Type >> 4);
        public PrimitiveTypes Primitive => (PrimitiveTypes) (Type & 0xF);
		public override string ToString()
		{
			return string.Format("[Key: {0}]", Key);
		}
		public unsafe void Run(RenderContext context)
        {
            if (CmdType >= RenderCmdType.Billboard)
            {
                var Billboards = (Billboards)Source;
                Billboards.Render(Index, Hash, context);
            }
            else
			{
				var Material = (RenderMaterial)Source;
				if (Material == null)
					return;
				Material.MaterialAnim = MaterialAnim;
				Material.World = World;
                Material.OpacityMultiplier = 1;
                if (CmdType == RenderCmdType.MaterialFade)
				{
					Material.Fade = true;
                    Material.FadeNear = Unsafe.BitCast<int, float>(Hash);
                    Material.FadeFar = Unsafe.BitCast<int, float>(Index);
                    Material.OpacityMultiplier = 1;
                }
                else
                {
                    Material.OpacityMultiplier = Unsafe.BitCast<int, float>(Hash);
                }
                if (Material.DisableCull || Material.DoubleSided) context.Cull = false;
				Material.Use(context, Buffer.VertexType, ref Lights, UserData);
				if ((CmdType != RenderCmdType.MaterialFade) && BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
                if (Material.DisableCull || Material.DoubleSided) context.Cull = true;
			}
		}
	}
}

