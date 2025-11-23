// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Shaders;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Render
{
	public class LineRenderer :  IDisposable, Physics.IDebugRenderer
	{
		const int MAX_LINES = 8192;
		VertexPositionColor[] lines = new VertexPositionColor[MAX_LINES * 2];
		VertexBuffer linebuffer;
		int lineVertices = 0;
        public string SkeletonHighlight;

		Shader shader;
		public LineRenderer(RenderContext rstate)
        {
            AllShaders.CompilePhysicsDebug(rstate);
            shader = AllShaders.PhysicsDebug.Get(0);
			linebuffer = new VertexBuffer(rstate, typeof(VertexPositionColor), MAX_LINES * 2, true);
		}

		RenderContext rstate;
		public void StartFrame(RenderContext rs)
		{
			rstate = rs;
			lineVertices = 0;
		}

        public void DrawLine(Vector3 start, Vector3 end, Color4 color)
        {
            if (lineVertices + 2 >= lines.Length)
            {
                Render();
                lineVertices = 0;
            }
            lines[lineVertices++] = new VertexPositionColor(start, color);
            lines[lineVertices++] = new VertexPositionColor(end, color);
        }

		public void DrawPoint(Vector3 pos, Color4 color)
        {
            if ((lineVertices % 2 == 0) &&
                (lineVertices + 2) >= lines.Length)
            {
                Render();
                lineVertices = 0;
            }
            lines[lineVertices++] = new VertexPositionColor(pos, color);
        }

        public void DrawTriangleMesh(Matrix4x4 mat, Vector3[] positions, int[] indices, Color4 color)
        {
            for(int i = 1; i < indices.Length; i++)
            {
                var p1 = positions[indices[i - 1]];
                var p2 = positions[indices[i]];
                DrawLine(Vector3.Transform(p1, mat), Vector3.Transform(p2, mat), color);
            }
        }

        private static readonly Vector3[] cubeVerts = new[]
        {
            //Front
            new Vector3(-1,-1,1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
            new Vector3(1, -1, 1),
            //Back
            new Vector3(-1,-1,-1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(1, -1, -1),
        };

        private static readonly int[] cubeIndices = new[]
        {
            //Front
            0,1, 1,2, 2,3, 3,0,
            //Back
            4,5, 5,6, 6,7, 7,4,
            //Join
            0,4, 1,5, 2,6, 3,7,
        };

        public void DrawCube(Matrix4x4 world, float scale, Color4 color)
        {
            for (int i = 0; i < cubeIndices.Length; i += 2)
            {
                var a = Vector3.Transform(cubeVerts[cubeIndices[i]] * scale, world);
                var b = Vector3.Transform(cubeVerts[cubeIndices[i + 1]] * scale, world);
                DrawLine(a,b, color);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MaterialParameters
        {
            public Color4 Dc;
            public float Oc;
        }

        public void DrawVWire(VMeshWire wire, VertexResource resource, Matrix4x4 world, Color4 color)
        {
            Render();
            rstate.Cull = false;
            rstate.DepthWrite = false;
            rstate.PolygonOffset = Vector2.One;
            shader.SetUniformBlock(0, ref world);
            var p = new MaterialParameters() { Dc = color, Oc = 1 };
            shader.SetUniformBlock(3, ref p);
            rstate.Shader = shader;
            resource.VertexBuffer.DrawImmediateElements(
                PrimitiveTypes.LineList,
                wire.VertexOffset + resource.BaseVertex,
                wire.Indices
            );
            rstate.PolygonOffset = Vector2.Zero;
            rstate.DepthWrite = true;
            rstate.Cull = true;
        }

        public void Render()
		{
			if (lineVertices == 0)
				return;
			rstate.Cull = false;
            rstate.DepthWrite = false;
            rstate.PolygonOffset = Vector2.One;
            var bm = rstate.BlendMode;
            rstate.BlendMode = BlendMode.Normal;
            var w = Matrix4x4.Identity;
            shader.SetUniformBlock(0, ref w);
            var p = new MaterialParameters() { Dc = Color4.White, Oc = 0 };
            shader.SetUniformBlock(3, ref p);
            rstate.Shader = shader;
			linebuffer.SetData<VertexPositionColor>(lines.AsSpan().Slice(0, lineVertices));
			linebuffer.Draw(PrimitiveTypes.LineList, lineVertices / 2);
            rstate.PolygonOffset = Vector2.Zero;
            rstate.DepthWrite = true;
            rstate.BlendMode = bm;
            rstate.Cull = true;
        }

		public void Dispose()
		{
			linebuffer.Dispose();
		}
	}
}

