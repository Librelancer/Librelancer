using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using SimpleMesh;

namespace LancerEdit.GameContent;

public class DisplayMesh
{
    public static DisplayMesh Cube;
    public static DisplayMesh Cylinder;
    public static DisplayMesh Sphere;
    public static DisplayMesh Ring;
    public static DisplayMesh Lightbulb;

    public static void LoadAll(RenderContext context)
    {
        if (Cube != null) return;
        Cube = new DisplayMesh(context, "LancerEdit.DisplayMeshes.cube.glb");
        Cylinder = new DisplayMesh(context, "LancerEdit.DisplayMeshes.cylinder.glb");
        Sphere = new DisplayMesh(context, "LancerEdit.DisplayMeshes.icosphere.glb");
        Ring = new DisplayMesh(context, "LancerEdit.DisplayMeshes.ring.glb");
        Lightbulb = new DisplayMesh(context, "LancerEdit.DisplayMeshes.lightbulb.glb");
    }
    public record struct Drawcall(int BaseVertex, int Start, int Count, Color4 Color);

    public Drawcall[] Drawcalls;
    public VertexBuffer VertexBuffer;
    public BoundingBox Bounds;

    public DisplayMesh(RenderContext context, string name)
    {
        using (var stream = typeof(DisplayMesh).Assembly.GetManifestResourceStream(name))
        {
            var msh = SimpleMesh.Model.FromStream(stream)
                .AutoselectRoot(out _)
                .ApplyRootTransforms(true);

            if ((msh.Roots[0].Geometry.Attributes & VertexAttributes.Normal) == 0)
                throw new Exception("Missing normals");
            var vertices = msh.Roots[0].Geometry.Vertices.Select(x => new VertexPositionNormal(x.Position, x.Normal)).ToArray();
            var indices = msh.Roots[0].Geometry.Indices.Indices16;

            var elementBuf = new ElementBuffer(context, indices.Length);
            elementBuf.SetData(indices);
            var vbo = new VertexBuffer(context, typeof(VertexPositionNormal), vertices.Length);
            vbo.SetData<VertexPositionNormal>(vertices);
            vbo.SetElementBuffer(elementBuf);

            List<Drawcall> dcs = new List<Drawcall>();
            foreach (var g in msh.Roots[0].Geometry.Groups)
            {
                var col = (Color4)((g.Material?.DiffuseColor ?? LinearColor.White).ToSrgb());
                dcs.Add(new(g.BaseVertex, g.StartIndex, g.IndexCount / 3, col));
            }

            msh.CalculateBounds();
            Bounds = new BoundingBox(msh.Roots[0].Geometry.Min, msh.Roots[0].Geometry.Max);

            Drawcalls = dcs.ToArray();
            VertexBuffer = vbo;
        }
    }
}
