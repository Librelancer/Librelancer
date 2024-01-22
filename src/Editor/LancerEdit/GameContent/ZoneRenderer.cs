using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Render;
using LibreLancer.Render.Materials;
using SimpleMesh;
using Material = LibreLancer.Utf.Mat.Material;

namespace LancerEdit;

public class ZoneRenderer
{
    private static VertexBuffer Cube;
    private static int CubeTris;
    private static VertexBuffer Cylinder;
    private static int CylinderTris;
    private static VertexBuffer Sphere;
    private static int SphereTris;
    private static VertexBuffer Ring;
    private static int RingTris;

    static (VertexBuffer, int) LoadMesh(RenderContext context, string name)
    {
        using (var stream = typeof(ZoneRenderer).Assembly.GetManifestResourceStream(name))
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
            vbo.SetData(vertices);
            vbo.SetElementBuffer(elementBuf);
            return (vbo, indices.Length / 3);
        }
    }

    public static void Load(RenderContext context)
    {
        if (Cube != null) return;
        (Cube, CubeTris) = LoadMesh(context, "LancerEdit.DisplayMeshes.cube.glb");
        (Cylinder, CylinderTris) = LoadMesh(context, "LancerEdit.DisplayMeshes.cylinder.glb");
        (Sphere, SphereTris) = LoadMesh(context, "LancerEdit.DisplayMeshes.icosphere.glb");
        (Ring, RingTris) = LoadMesh(context, "LancerEdit.DisplayMeshes.ring.glb");
    }

    private static RenderContext rstate;
    private static Material material;
    private static WorldMatrixBuffer buf = new WorldMatrixBuffer();
    private static List<ZoneToDraw> draws = new List<ZoneToDraw>();
    private static ICamera camera;
    struct ZoneToDraw
    {
        public WorldMatrixHandle W;
        public VertexBuffer VBO;
        public int Triangles;
        public Color4 Color;
        public float Ring;
        public bool FlipNormals;
    }

    public static void Begin(RenderContext r, ICamera cam)
    {
        rstate = r;
        camera = cam;
    }

    public static void DrawCube(
        Vector3 position,
        Vector3 size,
        Matrix4x4 rotation,
        Color4 color,
        bool inZone)
    {
        var w = Matrix4x4.CreateScale(size) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Cube, Triangles = CubeTris, Color = color,
            FlipNormals = inZone
        });
    }

    public static void DrawCylinder(
        Vector3 position,
        float radius,
        float height,
        Matrix4x4 rotation,
        Color4 color,
        bool inZone)
    {
        var w = Matrix4x4.CreateScale(new Vector3(radius, height, radius)) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Cylinder, Triangles = CylinderTris, Color = color,
            FlipNormals = inZone,
        });
    }

    public static void DrawRing(
        Vector3 position,
        float innerRadius,
        float outerRadius,
        float height,
        Matrix4x4 rotation,
        Color4 color,
        bool inZone)
    {
        if (innerRadius / outerRadius < float.Epsilon) {
            DrawCylinder(position, outerRadius, height, rotation, color, inZone);
            return;
        }
        var w = Matrix4x4.CreateScale(new Vector3(outerRadius, height, outerRadius)) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Ring, Triangles = RingTris, Color = color,
            Ring = innerRadius / outerRadius, FlipNormals = inZone,
        });
    }

    public static void DrawSphere(
        Vector3 position,
        float radius,
        Matrix4x4 rotation,
        Color4 color,
        bool inZone)
    {
        var w = Matrix4x4.CreateScale(radius) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Sphere, Triangles = SphereTris, Color = color,
            FlipNormals = inZone
        });
    }

    public static void DrawEllipsoid(
        Vector3 position,
        Vector3 size,
        Matrix4x4 rotation,
        Color4 color,
        bool inZone)
    {
        var w = Matrix4x4.CreateScale(size) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Sphere, Triangles = SphereTris, Color = color,
            FlipNormals = inZone
        });
    }

    public static void Finish(ResourceManager rs)
    {
        rstate.Cull = true;
        rstate.DepthWrite = false;
        foreach (var draw in draws)
        {
            var r = new ZoneVolumeMaterial(rs);
            r.Dc = draw.Color;
            r.World = draw.W;
            r.RadiusRatio = draw.Ring;
            r.Use(rstate, new VertexPositionNormal(), ref Lighting.Empty, 0);
            rstate.CullFace = draw.FlipNormals ? CullFaces.Front : CullFaces.Back;
            draw.VBO.Draw(PrimitiveTypes.TriangleList, 0, 0, draw.Triangles);
        }
        rstate.CullFace = CullFaces.Back;
        buf.Reset();
        draws = new List<ZoneToDraw>();
        rstate.Cull = true;
        rstate.DepthWrite = true;
    }
}
