// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Utf.Cmp;

namespace LibreLancer
{
	public delegate void ShaderAction(Shader shdr, RenderState res, ref RenderCommand cmd);
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
        RenderState rstate;
        public void StartFrame(RenderState rstate)
		{
			currentCommand = 0;
			transparentCommand = 0;
			_transparentSort = SortTransparent;
            this.rstate = rstate;
		}
		public void AddCommand(RenderMaterial material, MaterialAnim anim, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, int layer, float z = 0)
		{
			if (material.IsTransparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
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
					SortLayer = layer,
					Z = z
				};
			}
			else
			{
                material.MaterialAnim = anim;
                material.World = world;
                material.Use(rstate, buffer.VertexType, ref lights);
                buffer.Draw(primitive, baseVertex, start, count);
                if (material.DoubleSided)
                {
                    material.FlipNormals = true;
                    material.UpdateFlipNormals();
                    rstate.CullFace = CullFaces.Front;
                    buffer.Draw(primitive, baseVertex, start, count);
                    rstate.CullFace = CullFaces.Back;
                    material.FlipNormals = false;
                }
			}
		}
		//TODO: Implement MaterialAnim for asteroids
		public unsafe void AddCommandFade(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, int layer, Vector2 fadeParams, float z = 0)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
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
				SortLayer = layer, //Fade is always transparent 
				Z = z
			};
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, Lighting lt, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
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
					SortLayer = transparent ? layer : SortLayers.OPAQUE,
					Z = z
				};
			}
			else
			{
                throw new InvalidOperationException();
			}
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
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
					SortLayer = transparent ? layer : SortLayers.OPAQUE,
					Z = z
				};
			}
			else
			{
                throw new InvalidOperationException();
			}
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, bool transparent, int layer, float z = 0)
		{
			if (transparent)
			{
				Transparents[transparentCommand++] = new RenderCommand()
				{
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
					SortLayer = transparent ? layer : SortLayers.OPAQUE,
					Z = z
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
				CmdType = RenderCmdType.Billboard,
				Source = billboards,
				Hash = hash,
				Index = index,
				SortLayer = sortLayer,
				Z = z
			};
		}
		public void AddCommand(Billboards billboards, Shader shader, ShaderAction setup, RenderUserData userData, int indexStart, int layer, float z)
		{
			Transparents[transparentCommand++] = new RenderCommand()
			{
				CmdType = RenderCmdType.BillboardCustom,
				Source = billboards,
				ShaderSetup = setup,
				UserData = userData,
				Index = indexStart,
				Cleanup = shader,
				SortLayer = layer,
				Z = z
			};
		}
		public void DrawOpaque(RenderState state)
		{
            //_sorted = false;
            //_filled = false;
            //AsyncManager.RunTask (_transparentSort);
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
				if (Transparents[cmdptr[i]].CmdType == RenderCmdType.BillboardCustom)
				{
					var bb = (Billboards)Transparents[cmdptr[i]].Source;
					bb.AddCustomIndices(Transparents[cmdptr[i]].Index);
				}
			}
		}

		int[] cmdptr = new int[MAX_COMMANDS];
		int transparentCount = 0;
		public void DrawTransparent(RenderState state)
		{
		  	Billboards lastbb = null;
			for (int i = transparentCommand - 1; i >= 0; i--)
			{
				if (lastbb != null && Transparents[cmdptr[i]].CmdType != RenderCmdType.Billboard && Transparents[cmdptr[i]].CmdType != RenderCmdType.BillboardCustom)
				{
					lastbb.FlushCommands(state);
					lastbb = null;
				}
				lastbb = (Transparents[cmdptr[i]].Source as Billboards);
				Transparents[cmdptr[i]].Run(state);
			}
			if (lastbb != null)
				lastbb.FlushCommands(state);
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
			if (cmds[x].SortLayer > cmds[y].SortLayer)
				return -1;
			else if (cmds[x].SortLayer < cmds[y].SortLayer)
				return 1;
			else
				return cmds[x].Z.CompareTo(cmds[y].Z);
		}
	}
	public enum RenderCmdType : byte
	{
		Material,
        MaterialFade,
		Shader,
		Billboard,
		BillboardCustom
	}
	public struct RenderCommand
	{
		public PrimitiveTypes Primitive;
		public object Source;
		public Matrix4 World;
		public RenderUserData UserData;
		public ShaderAction ShaderSetup;
		public object Cleanup;
		public VertexBuffer Buffer;
		public int BaseVertex;
		public RenderCmdType CmdType;
		public int Start;
		public int Count;
        public Lighting Lights;
		public float Z;
		public int SortLayer;
		public MaterialAnim MaterialAnim;
		public int Hash;
		public int Index;
		public override string ToString()
		{
			return string.Format("[Z: {0}]", Z);
		}
		public unsafe void Run(RenderState state)
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
                if (Material.DisableCull) state.Cull = false;
				Material.Use(state, Buffer.VertexType, ref Lights);
				if ((CmdType != RenderCmdType.MaterialFade) && BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Count);
				if (Material.DoubleSided)
				{
                    Material.FlipNormals = true;
                    Material.UpdateFlipNormals();
					state.CullFace = CullFaces.Front;
					if ((CmdType != RenderCmdType.MaterialFade) && BaseVertex != -1)
						Buffer.Draw(Primitive, BaseVertex, Start, Count);
					else
						Buffer.Draw(Primitive, Count);
					state.CullFace = CullFaces.Back;
                    Material.FlipNormals = false;
				}
				if (CmdType == RenderCmdType.MaterialFade)
				{
					Material.Fade = false;
				}
                if (Material.DisableCull) state.Cull = true;
			}
			else if (CmdType == RenderCmdType.Shader)
			{
				var Shader = (Shader)Source;
				ShaderSetup(Shader, state, ref this);
				Shader.UseProgram();
				if (BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
				if (Cleanup != null)
					((Action<RenderState>)Cleanup)(state);
			}
			else if (CmdType == RenderCmdType.Billboard)
			{
				var Billboards = (Billboards)Source;
				Billboards.RenderStandard(Index, Hash, state);
			}
			else if (CmdType == RenderCmdType.BillboardCustom)
			{
				var billboards = (Billboards)Source;
				billboards.RenderCustom(state, (Shader)Cleanup, ShaderSetup, ref this);
			}
		}
	}
}

