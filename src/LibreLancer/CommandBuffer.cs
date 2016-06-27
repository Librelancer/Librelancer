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
using System.Linq;
using System.Collections.Generic;
namespace LibreLancer
{
	public class CommandBuffer
	{
		//public List<RenderCommand> Commands = new List<RenderCommand>();
		RenderCommand[] Commands = new RenderCommand[4096];
		int currentCommand = 0;
		public void StartFrame()
		{
			currentCommand = 0;
		}
		public void AddCommand(RenderMaterial material, Matrix4 world, Lighting lights, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count)
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
				World = world
			};
		}
		public void AddCommand(Shader shader, Action<Shader,RenderState,RenderCommand> setup, Action<RenderState> cleanup, Matrix4 world, RenderUserData user, VertexBuffer buffer, PrimitiveTypes primitive, int baseVertex, int start, int count, bool transparent)
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
				Transparent = transparent
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
		public void DrawTransparent(RenderState state)
		{
			for (int i = 0; i < currentCommand; i++)
			{
				if (Commands[i].Transparent)
				{
					Commands[i].Run(state);
				}
			}
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
					Buffer.Draw(Primitive, Count);
				if(Cleanup != null)
					Cleanup(state);
			}
		}
	}
}

