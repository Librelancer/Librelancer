using System;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Utf.Vms;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class Tangents
{
    public static void GenerateForUtf(EditableUtf utf)
    {
        var vmsroot =
            utf.Root.Children.FirstOrDefault(x => x.Name.Equals("vmeshlibrary", StringComparison.OrdinalIgnoreCase));
        if(vmsroot != null)
            GenerateVMeshLibrary(vmsroot);
        foreach (var child in utf.Root.Children)
        {
            if (child.Children == null)
                continue;
            var vms = child.Children.FirstOrDefault(x =>
                x.Name.Equals("vmeshlibrary", StringComparison.OrdinalIgnoreCase));
            if(vms != null)
                GenerateVMeshLibrary(vms);
        }
    }

    class VMeshTangents : ITangentGeometry
    {
        public VMeshData Data;
        public Vector4[] Tangents;

        public VMeshTangents(VMeshData d)
        {
            Data = d;
            Tangents = new Vector4[d.VertexCount];
        }

        public int GetNumFaces() => Data.Indices.Length / 3;

        public int GetNumVerticesOfFace(int index) => 3;

        int GetIndex(int faceIndex, int faceVertex) =>
            Data.Indices[(faceIndex * 3) + faceVertex];

        public Vector3 GetPosition(int faceIndex, int faceVertex)
            => Data.GetPosition(GetIndex(faceIndex, faceVertex));

        public Vector3 GetNormal(int faceIndex, int faceVertex)
            => Data.GetNormal(GetIndex(faceIndex, faceVertex));

        public Vector2 GetTexCoord(int faceIndex, int faceVertex)
            => Data.GetTexCoord(GetIndex(faceIndex, faceVertex), 0);

        public void SetTangent(Vector4 tangent, int faceIndex, int faceVertex)
            => Tangents[GetIndex(faceIndex, faceVertex)] = tangent;
    }

    static void GenerateVMeshLibrary(LUtfNode vmeshlib)
    {
        foreach (var child in vmeshlib.Children)
        {
            var vms = child.Children.FirstOrDefault(x =>
                x.Name.Equals("vmeshdata", StringComparison.OrdinalIgnoreCase));
            if (vms == null)
                continue;
            var data = new VMeshData(vms.Data, child.Name);
            if (!data.VertexFormat.Normal ||
                data.VertexFormat.TexCoords == 0)
            {
                FLLog.Warning("Tangents", $"Could not generate for {child.Name}. Must have at least NORMAL and TEX1");
                continue;
            }

            var tangentGen = new VMeshTangents(data);
            TangentGeneration.GenerateMikkTSpace(tangentGen);

            int ogTexCoords = data.VertexFormat.TexCoords;
            if (ogTexCoords == 4)
                ogTexCoords = 2;
            if (ogTexCoords == 3)
                ogTexCoords = 1;
            var newFVF = D3DFVF.XYZ | D3DFVF.NORMAL;
            if (ogTexCoords == 1)
                newFVF |= D3DFVF.TEX3;
            if (ogTexCoords == 2)
                newFVF |= D3DFVF.TEX4;
            if (data.VertexFormat.Diffuse)
                newFVF |= D3DFVF.DIFFUSE;
            var mem = new MemoryStream();
            var writer = new BinaryWriter(mem);
            writer.Write((uint)0x01); //MeshType
            writer.Write((uint)0x04); //SurfaceType
            writer.Write((ushort)(data.Meshes.Length)); //MeshCount
            writer.Write((ushort)(data.Indices.Length)); //IndexCount
            writer.Write((ushort)newFVF); //FVF
            writer.Write((ushort)data.VertexCount); //VertexCount
            foreach (var t in data.Meshes)
            {
                writer.Write(t.MaterialCrc);
                writer.Write(t.StartVertex);
                writer.Write(t.EndVertex);
                writer.Write(t.NumRefVertices);
                writer.Write(t.Padding);
            }
            foreach (var idx in data.Indices) writer.Write(idx);
            for (int i = 0; i < data.VertexCount; i++)
            {
                var p = data.GetPosition(i);
                var n = data.GetNormal(i);
                writer.Write(p.X);
                writer.Write(p.Y);
                writer.Write(p.Z);
                writer.Write(n.X);
                writer.Write(n.Y);
                writer.Write(n.Z);
                if (data.VertexFormat.Diffuse)
                {
                    writer.Write(data.GetDiffuse(i));
                }
                for (int uv = 0; uv < ogTexCoords; uv++)
                {
                    var t = data.GetTexCoord(i, uv);
                    writer.Write(t.X);
                    writer.Write(t.Y);
                }
                writer.Write(tangentGen.Tangents[i].X);
                writer.Write(tangentGen.Tangents[i].Y);
                writer.Write(tangentGen.Tangents[i].Z);
                writer.Write(tangentGen.Tangents[i].W);
            }

            vms.Data = mem.ToArray();
            FLLog.Debug("Tangents", $"Generated for {child.Name}");
        }
    }
}
