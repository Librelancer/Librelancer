using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.ImUI;
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

            var geo = msh.Roots[0].Geometry;
            if (geo == null)
                throw new InvalidOperationException("No geometry");
            if ((geo.Vertices.Descriptor.Attributes & VertexAttributes.Normal) == 0)
                throw new Exception("Missing normals");
            var vertices = new VertexPositionNormal[geo.Vertices.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new(geo.Vertices.Position[i], geo.Vertices.Normal[i]);
            }
            var indices = geo.Indices.Indices16!;
            var elementBuf = new ElementBuffer(context, indices.Length);
            elementBuf.SetData(indices);
            var vbo = new VertexBuffer(context, typeof(VertexPositionNormal), vertices.Length);
            vbo.SetData<VertexPositionNormal>(vertices);
            vbo.SetElementBuffer(elementBuf);

            List<Drawcall> dcs = new List<Drawcall>();
            foreach (var g in geo.Groups)
            {
                var col = (Color4)((g.Material?.DiffuseColor ?? LinearColor.White).ToSrgb());
                dcs.Add(new(g.BaseVertex, g.StartIndex, g.IndexCount / 3, col));
            }

            msh.CalculateBounds();
            Bounds = new BoundingBox(geo.Min, geo.Max);
            Drawcalls = dcs.ToArray();
            VertexBuffer = vbo;
        }
    }
}
