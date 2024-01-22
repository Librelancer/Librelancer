using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.GameData.World;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Vertices;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Render;

public struct CubeDrawcall
{
    public uint MaterialCrc;
    public int StartIndex;
    public int Count;
    public int BaseVertex;
}

public class AsteroidCubeMesh : IDisposable
{
    public VertexBuffer VertexBuffer;
    public ElementBuffer ElementBuffer;
    public CubeDrawcall[] Drawcalls;
    public float Radius;

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        ElementBuffer?.Dispose();
    }
}

public class AsteroidCubeMeshBuilder
{
    List<VertexPositionNormalDiffuseTexture> verts;
    List<ushort> indices;
    List<int> hashes;
    private List<CubeDrawcall> cubeDrawCalls;
    private float radius = 0;

    public AsteroidCubeMesh CreateMesh(RenderContext context, AsteroidField field, ResourceManager resources)
    {
        verts = new List<VertexPositionNormalDiffuseTexture>();
        indices = new List<ushort>();
        hashes = new List<int>();
        cubeDrawCalls = new List<CubeDrawcall>();
        radius = 0;
        //Gather a list of all materials
        List<uint> matCrcs = new List<uint>();
        if (field.AllowMultipleMaterials)
        {
            foreach (var ast in field.Cube)
            {
                var f = (ModelFile) ast.Drawable.LoadFile(resources, MeshLoadMode.CPU);
                var l0 = f.Levels[0];
                var vms = resources.FindMeshData(l0.MeshCrc);
                for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
                {
                    var m = vms.Meshes[i].MaterialCrc;
                    if (!matCrcs.Contains(m))
                        matCrcs.Add(m);
                }
            }
        }
        else
        {
            var f = (ModelFile) field.Cube[0].Drawable.LoadFile(resources, MeshLoadMode.CPU);
            var l0 = f.Levels[0];
            var vms = resources.FindMeshData(l0.MeshCrc);
            matCrcs.Add(vms.Meshes[l0.StartMesh].MaterialCrc);
        }

        //Create the draw calls
        foreach (var mat in matCrcs)
        {
            var start = indices.Count;
            var newIndices = new List<int>();
            foreach (var ast in field.Cube)
            {
                AddAsteroidToBuffer(ast, mat, matCrcs.Count == 1, newIndices, resources, field.CubeSize);
            }

            var min = newIndices.Min();
            foreach (var i in newIndices)
                indices.Add(checked((ushort) (i - min)));
            var count = indices.Count - start;
            cubeDrawCalls.Add(new CubeDrawcall()
                {BaseVertex = min, MaterialCrc = mat, StartIndex = start, Count = count});
        }

        var cube_vbo = new VertexBuffer(context, typeof(VertexPositionNormalDiffuseTexture), verts.Count);
        var cube_ibo = new ElementBuffer(context, indices.Count);
        cube_ibo.SetData(indices.ToArray());
        cube_vbo.SetData(verts.ToArray());
        cube_vbo.SetElementBuffer(cube_ibo);
        var dcs = cubeDrawCalls.ToArray();
        verts = null;
        indices = null;
        cubeDrawCalls = null;
        return new AsteroidCubeMesh()
        {
            VertexBuffer = cube_vbo,
            ElementBuffer = cube_ibo,
            Drawcalls = dcs,
            Radius = radius
        };
    }

    VertexPositionNormalDiffuseTexture GetVertex(VMeshData vms, int index, ref Matrix4x4 world, ref Matrix4x4 normal)
    {
        VertexPositionNormalDiffuseTexture vert;
        switch (vms.FlexibleVertexFormat)
        {
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1:
                vert = vms.verticesVertexPositionNormalDiffuseTexture[index];
                break;
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1:
            {
                var v = vms.verticesVertexPositionNormalTexture[index];
                vert = new VertexPositionNormalDiffuseTexture(
                    v.Position,
                    v.Normal,
                    (uint) Color4.White.ToAbgr(),
                    v.TextureCoordinate);
                break;
            }
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX2:
            {
                var v = vms.verticesVertexPositionNormalTextureTwo[index];
                vert = new VertexPositionNormalDiffuseTexture(
                    v.Position,
                    v.Normal,
                    (uint) Color4.White.ToAbgr(),
                    v.TextureCoordinate);
                break;
            }
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX2:
            {
                var v = vms.verticesVertexPositionNormalDiffuseTextureTwo[index];
                vert = new VertexPositionNormalDiffuseTexture(
                    v.Position,
                    v.Normal,
                    v.Diffuse,
                    v.TextureCoordinate);
                break;
            }
            default:
                throw new Exception($"D3DFVF {vms.FlexibleVertexFormat} not supported");
        }

        vert.Position = Vector3.Transform(vert.Position, world);
        vert.Normal = Vector3.TransformNormal(vert.Normal, normal);
        return vert;
    }

    void AddAsteroidToBuffer(StaticAsteroid ast, uint matCrc, bool singleMat, List<int> newIndices,
        ResourceManager resources, float cubeSize)
    {
        var model = (ModelFile) ast.Drawable.LoadFile(resources, MeshLoadMode.CPU);
        var l0 = model.Levels[0];
        var vms = resources.FindMeshData(l0.MeshCrc);
        var transform = ast.RotationMatrix * Matrix4x4.CreateTranslation(ast.Position * cubeSize);
        var norm = transform;
        Matrix4x4.Invert(norm, out norm);
        norm = Matrix4x4.Transpose(norm);
        for (int i = l0.StartMesh; i < l0.StartMesh + l0.MeshCount; i++)
        {
            var m = vms.Meshes[i];
            if (m.MaterialCrc != matCrc && !singleMat) continue;
            var baseVertex = l0.StartVertex + m.StartVertex;
            int indexStart = m.TriangleStart;
            int indexCount = m.NumRefVertices;
            for (int j = indexStart; j < indexStart + indexCount; j++)
            {
                var idx = baseVertex + vms.Indices[j];
                var vtx = GetVertex(vms, idx, ref transform, ref norm);
                var hash = vtx.GetHashCode();
                int x = hashes.IndexOf(hash);
                if (x == -1 || verts[x] != vtx)
                {
                    x = verts.Count;
                    verts.Add(vtx);
                    hashes.Add(hash);
                    var d = vtx.Position.Length();
                    if (d > radius)
                        radius = d;
                }
                newIndices.Add(x);
            }
        }
    }
}
