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
		//public List<RenderCommand> Commands = new List<RenderCommand>();
		RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
		int currentCommand = 0;
		Action _transparentSort;

		public void StartFrame()
		{
			currentCommand = 0;
			_transparentSort = SortTransparent;
		}
		public void AddCommand(RenderMaterial material, MaterialAnim anim, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
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
				SortLayer = material.IsTransparent ? layer : SortLayers.OPAQUE,
				Z = z
			};
		}
		//TODO: Implement MaterialAnim for asteroids
		public unsafe void AddCommandFade(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, int layer, Vector2 fadeParams, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				Source = material,
				MaterialAnim = null,
				Lights = lights,
				Buffer = buffer,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.Material,
				Fade = true,
				BaseVertex = *(int*)(&fadeParams.X),
				Index = *(int*)(&fadeParams.Y),
				World = world,
				SortLayer = layer, //Fade is always transparent 
				Z = z
			};
		}
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, Lighting lt, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
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
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
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
		public void AddCommand(Shader shader, ShaderAction setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, bool transparent, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
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
		public void AddCommand(Billboards billboards, int hash, int index, int sortLayer, float z)
		{
			Commands[currentCommand++] = new RenderCommand()
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
			Commands[currentCommand++] = new RenderCommand()
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
		bool _sorted = false;
		public void DrawOpaque(RenderState state)
		{
			_sorted = false;
			AsyncManager.RunTask (_transparentSort);
			for (int i = 0; i < currentCommand; i++)
			{
				if (Commands[i].SortLayer == SortLayers.OPAQUE)
				{
					Commands[i].Run(state);
				}
				
			}
		}

		void SortTransparent()
		{
			int a = 0;
			for (int i = 0; i < currentCommand; i++)
			{
				if (Commands[i].SortLayer != SortLayers.OPAQUE)
				{
					cmdptr[a++] = i;
				}
			}
			Array.Sort<int>(cmdptr, 0, a, new ZComparer(Commands));
			transparentCount = a;
			_sorted = true;
		}

		int[] cmdptr = new int[MAX_COMMANDS];
		int transparentCount = 0;
		public void DrawTransparent(RenderState state)
		{
			while (!_sorted) {
			}
            for (int i = transparentCount - 1; i >= 0; i--)
            {
                if(Commands[cmdptr[i]].CmdType == RenderCmdType.Billboard)
                {
					var bb = (Billboards)Commands[cmdptr[i]].Source;
                    bb.AddIndices(Commands[cmdptr[i]].Index);
                }
				if (Commands[cmdptr[i]].CmdType == RenderCmdType.BillboardCustom)
				{
					var bb = (Billboards)Commands[cmdptr[i]].Source;
					bb.AddCustomIndices(Commands[cmdptr[i]].Index);
				}
            }
            Billboards lastbb = null;
			for (int i = transparentCount - 1; i >= 0; i--)
			{
				if (lastbb != null && Commands[cmdptr[i]].CmdType != RenderCmdType.Billboard && Commands[cmdptr[i]].CmdType != RenderCmdType.BillboardCustom)
				{
					lastbb.FlushCommands(state);
					lastbb = null;
				}
				lastbb = (Commands[cmdptr[i]].Source as Billboards);
				Commands[cmdptr[i]].Run(state);
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
	public enum RenderCmdType
	{
		Material,
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
		public bool Fade;
		public override string ToString()
		{
			return string.Format("[Z: {0}]", Z);
		}
		public unsafe void Run(RenderState state)
		{
			if (CmdType == RenderCmdType.Material)
			{
				var Material = (RenderMaterial)Source;
				if (Material == null)
					return;
				Material.MaterialAnim = MaterialAnim;
				Material.World = World;
				if (Fade)
				{
					Material.Fade = true;
					var fn = BaseVertex;
					var ff = Index;
					Material.FadeNear = *(float*)(&fn);
					Material.FadeFar = *(float*)(&ff);
				}
				Material.Use(state, Buffer.VertexType, Lights);
				if (!Fade && BaseVertex != -1)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Count);
				if (Material.DoubleSided)
				{
					Material.FlipNormals = true;
					state.CullFace = CullFaces.Front;
					if (!Fade && BaseVertex != -1)
						Buffer.Draw(Primitive, BaseVertex, Start, Count);
					else
						Buffer.Draw(Primitive, Count);
					Material.FlipNormals = false;
					state.CullFace = CullFaces.Back;
				}
				if (Fade)
				{
					Material.Fade = false;
				}
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

