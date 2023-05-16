using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer;
using LibreLancer.Render;
using LibreLancer.Render.Materials;
using LibreLancer.Utf.Mat;
using LibreLancer.Vertices;

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
    
    static (VertexBuffer, int) LoadMesh(string name)
    {
        using (var stream = typeof(ZoneRenderer).Assembly.GetManifestResourceStream(name))
        {
            var msh = SimpleMesh.Model.FromStream(stream);
            var vertices = msh.Roots[0].Geometry.Vertices.Select(x => new VertexPosition(x.Position)).ToArray();
            var indices = msh.Roots[0].Geometry.Indices.Indices16;

            var elementBuf = new ElementBuffer(indices.Length);
            elementBuf.SetData(indices);
            var vbo = new VertexBuffer(typeof(VertexPosition), vertices.Length);
            vbo.SetData(vertices);
            vbo.SetElementBuffer(elementBuf);
            return (vbo, indices.Length / 3);
        }
    }

    public static void Load()
    {
        if (Cube != null) return;
        (Cube, CubeTris) = LoadMesh("LancerEdit.DisplayMeshes.cube.obj");
        (Cylinder, CylinderTris) = LoadMesh("LancerEdit.DisplayMeshes.cylinder.obj");
        (Sphere, SphereTris) = LoadMesh("LancerEdit.DisplayMeshes.icosphere.obj");
        (Ring, RingTris) = LoadMesh("LancerEdit.DisplayMeshes.ring.obj");
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
    }
    
    public static void Begin(RenderContext r, ResourceManager res, ICamera cam)
    {
        rstate = r;
        material = new Material(res);
        material.Update(cam);
        camera = cam;
    }

    public static void DrawCube(
        Vector3 position, 
        Vector3 size, 
        Matrix4x4 rotation, 
        Color4 color
        )
    {
        var w = Matrix4x4.CreateScale(size) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Cube, Triangles = CubeTris, Color = color
        });
    }
    
    public static void DrawCylinder(
        Vector3 position, 
        float radius,
        float height,
        Matrix4x4 rotation, 
        Color4 color
    )
    {
        var w = Matrix4x4.CreateScale(new Vector3(radius, height, radius)) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Cylinder, Triangles = CylinderTris, Color = color
        });
    }
    
    public static void DrawRing(
        Vector3 position, 
        float innerRadius,
        float outerRadius,
        float height,
        Matrix4x4 rotation, 
        Color4 color
    )
    {
        if (innerRadius / outerRadius < float.Epsilon) {
            DrawCylinder(position, outerRadius, height, rotation, color);
            return;
        }
        var w = Matrix4x4.CreateScale(new Vector3(outerRadius, height, outerRadius)) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Ring, Triangles = RingTris, Color = color,
            Ring = innerRadius / outerRadius,
        });
    }
    
    public static void DrawSphere(
        Vector3 position, 
        float radius,
        Matrix4x4 rotation, 
        Color4 color
    )
    {
        var w = Matrix4x4.CreateScale(radius) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Sphere, Triangles = SphereTris, Color = color
        });
    }
    
    public static void DrawEllipsoid(
        Vector3 position, 
        Vector3 size,
        Matrix4x4 rotation, 
        Color4 color
    )
    {
        var w = Matrix4x4.CreateScale(size) *
                rotation *
                Matrix4x4.CreateTranslation(position);
        var b = buf.SubmitMatrix(ref w);
        draws.Add(new ZoneToDraw() {
            W = b, VBO = Sphere, Triangles = SphereTris, Color = color
        });
    }

    public static void Finish()
    {
        var m = (BasicMaterial) material.Render;
        rstate.Cull = false;
        rstate.DepthWrite = false;
        foreach (var draw in draws)
        {
            if (draw.Ring > 0)
            {
                //Shader to resize ring correctly
                var r = new RingMaterial();
                r.Dc = draw.Color;
                r.World = draw.W;
                r.Camera = camera;
                r.RadiusRatio = draw.Ring;
                r.Use(rstate, new VertexPosition(),ref Lighting.Empty);
            }
            else
            {
                m.Dc = draw.Color;
                m.Oc = draw.Color.A;
                m.AlphaEnabled = true;
                m.OcEnabled = true;
                m.World = draw.W;
                m.Use(rstate, new VertexPosition(), ref Lighting.Empty);
            }

            draw.VBO.Draw(PrimitiveTypes.TriangleList, 0, 0, draw.Triangles);
        }
        buf.Reset();
        draws = new List<ZoneToDraw>();
        rstate.Cull = true;
        rstate.DepthWrite = true;
    }
}