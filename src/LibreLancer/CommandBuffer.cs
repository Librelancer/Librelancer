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
namespace LibreLancer
{
	public class CommandBuffer
	{
		const int MAX_COMMANDS = 8192;
		//public List<RenderCommand> Commands = new List<RenderCommand>();
		RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
		int currentCommand = 0;
		Action _transparentSort;
		public void StartFrame()
		{
			currentCommand = 0;
			_transparentSort = SortTransparent;
		}
		public void AddCommand(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				Material = material,
				Lights = lights,
				Buffer = buffer,
				BaseVertex = baseVertex,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.Material,
				UseBaseVertex = true,
				Transparent = material.IsTransparent,
				World = world,
				SortLayer = layer,
				Z = z
			};
		}
		public unsafe void AddCommandFade(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, int layer, Vector2 fadeParams, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				Material = material,
				Lights = lights,
				Buffer = buffer,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.Material,
				UseBaseVertex = false,
				Fade = true,
				BaseVertex = *(int*)(&fadeParams.X),
				Index = *(int*)(&fadeParams.Y),
				Transparent = material.IsTransparent,
				World = world,
				SortLayer = layer,
				Z = z
			};
		}
		public void AddCommand(Shader shader, Action<Shader,RenderState,RenderCommand> setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				Shader = shader,
				ShaderSetup = setup,
				World = world,
				UserData = user,
				Cleanup = cleanup,
				Buffer = buffer,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.Shader,
				UseBaseVertex = true,
				Transparent = transparent,
				SortLayer = layer,
				Z = z
			};
		}
		public void AddCommand(Shader shader, Action<Shader, RenderState, RenderCommand> setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, bool transparent, int layer, float z = 0)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				Shader = shader,
				ShaderSetup = setup,
				World = world,
				UserData = user,
				Cleanup = cleanup,
				Buffer = buffer,
				Start = start,
				Count = count,
				Primitive = primitive,
				CmdType = RenderCmdType.Shader,
				UseBaseVertex = false,
				Transparent = transparent,
				SortLayer = layer,
				Z = z
			};
		}
		public void AddCommand(Billboards billboards, int hash, int index, int sortLayer, float z)
		{
			Commands[currentCommand++] = new RenderCommand()
			{
				CmdType = RenderCmdType.Billboard,
				Billboards = billboards,
				Hash = hash,
				Index = index,
				SortLayer = sortLayer,
				Z = z,
				Transparent = true
			};
		}
		bool _sorted = false;
		public void DrawOpaque(RenderState state)
		{
			_sorted = false;
			AsyncManager.RunTask (_transparentSort);
			for (int i = 0; i < currentCommand; i++)
			{
				if (!Commands[i].Transparent)
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
				if (Commands[i].Transparent)
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
                    Commands[cmdptr[i]].Billboards.AddIndices(Commands[cmdptr[i]].Index);
                }
            }
                Billboards lastbb = null;
			for (int i = transparentCount - 1; i >= 0; i--)
			{
				if (lastbb != null && Commands[cmdptr[i]].CmdType != RenderCmdType.Billboard)
				{
					lastbb.FlushCommands(state);
					lastbb = null;
				}
				lastbb = Commands[cmdptr[i]].Billboards;
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
		Billboard
	}
	public struct RenderCommand
	{
		public PrimitiveTypes Primitive;
		public RenderMaterial Material;
		public Matrix4 World;
		public RenderUserData UserData;
		public Shader Shader;
		public Action<Shader, RenderState,RenderCommand> ShaderSetup;
		public Action<RenderState> Cleanup;
		public VertexBuffer Buffer;
		public int BaseVertex;
		public RenderCmdType CmdType;
		public bool UseBaseVertex;
		public int Start;
		public int Count;
		public bool Transparent;
		public Lighting Lights;
		public float Z;
		public int SortLayer;
		public Billboards Billboards;
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
				if (UseBaseVertex)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Count);
				if (Fade)
				{
					Material.Fade = false;
				}
			}
			else if (CmdType == RenderCmdType.Shader)
			{
				ShaderSetup(Shader, state, this);
				Shader.UseProgram();
				if (UseBaseVertex)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
				if (Cleanup != null)
					Cleanup(state);
			}
			else if (CmdType == RenderCmdType.Billboard)
			{
				Billboards.RenderStandard(Index, Hash, state);
			}
		}
	}
}

