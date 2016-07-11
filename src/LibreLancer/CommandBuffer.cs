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
		const int MAX_COMMANDS = 4096;
		//public List<RenderCommand> Commands = new List<RenderCommand>();
		RenderCommand[] Commands = new RenderCommand[MAX_COMMANDS];
		int currentCommand = 0;
		public void StartFrame()
		{
			currentCommand = 0;
		}
		public void AddCommand(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, float z = 0)
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
				UseMaterial = true,
				UseBaseVertex = true,
				Transparent = material.IsTransparent,
				World = world,
				Z = z
			};
		}
		public void AddCommand(Shader shader, Action<Shader,RenderState,RenderCommand> setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent, float z = 0)
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
				UseMaterial = false,
				UseBaseVertex = true,
				Transparent = transparent,
				Z = z
			};
		}
		public void AddCommand(Shader shader, Action<Shader, RenderState, RenderCommand> setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int start, int count, bool transparent, float z = 0)
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
				UseMaterial = false,
				UseBaseVertex = false,
				Transparent = transparent,
				Z = z
			};
		}
		public void DrawOpaque(RenderState state)
		{
			for (int i = 0; i < currentCommand; i++)
			{
				if (!Commands[i].Transparent)
				{
					Commands[i].Run(state);
				}
				
			}
		}
		int[] cmdptr = new int[MAX_COMMANDS];
		public void DrawTransparent(RenderState state)
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
			for (int i = a - 1; i >= 0; i--)
			{
				Commands[cmdptr[i]].Run(state);
			}
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
			return cmds[x].Z.CompareTo(cmds[y].Z);
		}
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
		public bool UseMaterial;
		public bool UseBaseVertex;
		public int Start;
		public int Count;
		public bool Transparent;
		public Lighting Lights;
		public float Z;
		public string Caller;
		public override string ToString()
		{
			return string.Format("[{1} - Z: {0}]", Z, Caller);
		}
		public void Run(RenderState state)
		{
			if (UseMaterial)
			{
				Material.World = World;
				Material.Use(state, Buffer.VertexType, Lights);
				if (UseBaseVertex)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Count);
			}
			else
			{
				ShaderSetup(Shader, state, this);
				Shader.UseProgram();
				if (UseBaseVertex)
					Buffer.Draw(Primitive, BaseVertex, Start, Count);
				else
					Buffer.Draw(Primitive, Start, Count);
				if(Cleanup != null)
					Cleanup(state);
			}
		}
	}
}

