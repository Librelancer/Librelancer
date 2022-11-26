// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render
{
	public delegate void ShaderAction(Shader shdr, RenderContext res, ref RenderCommand cmd);
	public class CommandBuffer
	{
		const int MAX_COMMANDS = 16384;
		const int MAX_TRANSPARENT_COMMANDS = 16384;

		//public List<RenderCommand> Commands = new List<RenderCommand>();
		//RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
		RenderCommand[] Transparents = new RenderCommand[MAX_TRANSPARENT_COMMANDS];
		int currentCommand = 0;
		int transparentCommand = 0;
		Action _transparentSort;
        RenderContext rstate;
        public UniformBuffer BonesBuffer;
        public WorldMatrixBuffer WorldBuffer;
        public CommandBuffer()
        {
            //TODO: This needs to be managed a lot better, leaks memory right now
            BonesBuffer = new UniformBuffer(800, 64, typeof(Matrix4x4));
            WorldBuffer = new WorldMatrixBuffer();
        }
        public void StartFrame(RenderContext rstate)
		{
			currentCommand = 0;
			transparentCommand = 0;
			_transparentSort = SortTransparent;
            WorldBuffer.Reset();
            this.rstate = rstate;
		}
		public void AddCommand(RenderMaterial material, MaterialAnim anim, WorldMatrixHandle world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, int layer, float z = 0, DfmSkinning skinning = null)
		{
			if (material.IsTransparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
                    Key = RenderCommand.MakeKey(RenderType.Transparent, material.Key, layer, z, 0),
					Source = material,
					MaterialAnim = anim,
					Lights = lights,
					Buffer = buffer,
					BaseVertex = baseVertex,
					Start = start,
					Count = count,
					Primitive = primitive,
					CmdType = RenderCmdType.Material,
					World = world,
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
                material.Use(rstate, buffer.VertexType, ref lights);
                if (material.DoubleSided) {
                    rstate.Cull = false;
                }
                buffer.Draw(primitive, baseVertex, start, count);
                if (material.DoubleSided) {
                    rstate.Cull = true;
                }
            }
		}
		//TODO: Implement MaterialAnim for asteroids
		public unsafe void AddCommandFade(RenderMaterial material, WorldMatrixHandle world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, int layer, Vector2 fadeParams, float z = 0)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
                Key = RenderCommand.MakeKey(RenderType.Transparent, material.Key, layer, z, 0),
				Source = material,
				MaterialAnim = null,
				Lights = lights,
				Buffer = buffer,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.MaterialFade,
				BaseVertex = *(int*)(&fadeParams.X),
				Index = *(int*)(&fadeParams.Y),
				World = world,
            };
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderContext> cleanup, WorldMatrixHandle world, Lighting lt, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0, int renIndex = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
                    Key = RenderCommand.MakeKey(RenderType.Transparent, 0, layer, z, renIndex),
					Source = shader,
					ShaderSetup = setup,
					World = world,
					UserData = user,
					Cleanup = cleanup,
					Buffer = buffer,
					Lights = lt,
					Start = start,
					Count = count,
					Primitive = primitive,
					CmdType = RenderCmdType.Shader,
                };
			}
			else
			{
                throw new InvalidOperationException();
			}
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderContext> cleanup, WorldMatrixHandle world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0, int renIndex = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
                    Key = RenderCommand.MakeKey(RenderType.Transparent, 0, layer, z, renIndex),
					Source = shader,
					ShaderSetup = setup,
					World = world,
					UserData = user,
					Cleanup = cleanup,
					Buffer = buffer,
					Start = start,
					Count = count,
					Primitive = primitive,
					CmdType = RenderCmdType.Shader,
                };
			}
			else
			{
                throw new InvalidOperationException();
			}
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderContext> cleanup, WorldMatrixHandle world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, bool transparent, int layer, float z = 0, int renIndex = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
                    Key = RenderCommand.MakeKey(RenderType.Transparent, 0, layer, z, renIndex),
					Source = shader,
					ShaderSetup = setup,
					World = world,
					UserData = user,
					Cleanup = cleanup,
					Buffer = buffer,
					Start = start,
					Count = count,
					Primitive = primitive,
					CmdType = RenderCmdType.Shader,
					BaseVertex = -1,
                };
			}
			else
			{
                throw new InvalidOperationException();
			}
		}
		public void AddCommand(Billboards billboards, int hash, int index, int sortLayer, float z)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
                Key = RenderCommand.MakeKey(RenderType.Transparent, 0, sortLayer, z, 0),
				CmdType = RenderCmdType.Billboard,
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
			Array.Sort<int>(cmdptr, 0, transparentCommand, new ZComparer(Transparents));
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

	}
	class ZComparer : IComparer<int>
	{
		RenderCommand[] cmds;
		public ZComparer(RenderCommand[] commands)
		{
			cmds = commands;
		}
		public int Compare(int x, int y)
		{
            /*if (cmds[x].CmdType == RenderCmdType.Billboard && 
                cmds[y].CmdType == RenderCmdType.Billboard)
            {
                var b = (Billboards)cmds[x].Source;
                //Batch additive billboards (lights)
                if (b.GetBlendMode(cmds[x].Index) == BlendMode.Additive &&
                    b.GetBlendMode(cmds[y].Index) == BlendMode.Additive)
                {
                    return b.GetTextureID(cmds[x].Index).CompareTo(b.GetTextureID(cmds[y].Index));
                }
            }*/
            return cmds[x].Key.CompareTo(cmds[y].Key);
		}
	}
	public enum RenderCmdType : byte
	{
		Material,
        MaterialFade,
		Shader,
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

        public ulong Key;
		public PrimitiveTypes Primitive;
		public object Source;
        public WorldMatrixHandle World;
		public RenderUserData UserData;
		public ShaderAction ShaderSetup;
		public object Cleanup;
		public VertexBuffer Buffer;
		public int BaseVertex;
		public RenderCmdType CmdType;
		public int Start;
		public int Count;
        public Lighting Lights;
        public MaterialAnim MaterialAnim;
		public int Hash;
		public int Index;
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
					var fn = BaseVertex;
					var ff = Index;
					Material.FadeNear = *(float*)(&fn);
					Material.FadeFar = *(float*)(&ff);
				}
                if (Material.DisableCull || Material.DoubleSided) context.Cull = false;
				Material.Use(context, Buffer.VertexType, ref Lights);
				if ((CmdType != RenderCmdType.MaterialFade) && BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Count);
                if (Material.DisableCull || Material.DoubleSided) context.Cull = true;
			}
			else if (CmdType == RenderCmdType.Shader)
			{
				var Shader = (Shader)Source;
				ShaderSetup(Shader, context, ref this);
				Shader.UseProgram();
				if (BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
				if (Cleanup != null)
					((Action<RenderContext>)Cleanup)(context);
			}
			else if (CmdType == RenderCmdType.Billboard)
			{
				var Billboards = (Billboards)Source;
				Billboards.Render(Index, Hash, context);
			}
		}
	}
}

