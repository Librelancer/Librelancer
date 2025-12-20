using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class Tangents
{
    public static EditResult<bool> GenerateForUtf(EditableUtf utf)
    {
        var md = EditResult<Dictionary<uint, VMeshEntries>>.TryCatch(() =>
        {
            var meshData = new Dictionary<uint, VMeshEntries>();
            var vmsroot =
                utf.Root.Children.FirstOrDefault(x =>
                    x.Name.Equals("vmeshlibrary", StringComparison.OrdinalIgnoreCase));
            if (vmsroot != null)
                GatherVMeshLibrary(meshData, vmsroot);
            foreach (var child in utf.Root.Children)
            {
                if (child.Children == null)
                    continue;
                var vms = child.Children.FirstOrDefault(x =>
                    x.Name.Equals("vmeshlibrary", StringComparison.OrdinalIgnoreCase));
                if (vms != null)
                    GatherVMeshLibrary(meshData, vms);
            }

            GatherVMeshRef(meshData, utf.Root);
            foreach (var child in utf.Root.Children)
            {
                GatherVMeshRef(meshData, child);
            }

            return meshData;
        });
        if (md.IsError)
            return new EditResult<bool>(false, md.Messages);
        foreach(var ent in md.Data.Values)
            GenerateVMeshLibrary(ent);
        return true.AsResult();
    }

    static void GenerateVMeshLibrary(VMeshEntries ent)
    {
        var data = ent.Data;
        var allTangents = new Vector4[data.VertexCount];
        if (!data.VertexFormat.Normal ||
            data.VertexFormat.TexCoords == 0)
        {
            FLLog.Warning("Tangents", $"Could not generate for {ent.Name}. Must have at least NORMAL and TEX1");
            return;
        }

        foreach (var vmsref in ent.Refs)
        {
            for (int i = vmsref.StartMesh; i < vmsref.StartMesh + vmsref.MeshCount; i++)
            {
                var tangentGen = new VMeshTangents(data, data.Meshes[i], vmsref.StartVertex, allTangents);
                TangentGeneration.GenerateMikkTSpace(tangentGen);
            }
        }

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

            writer.Write(allTangents[i].X);
            writer.Write(allTangents[i].Y);
            writer.Write(allTangents[i].Z);
            writer.Write(allTangents[i].W);
        }

        ent.DataNode.Data = mem.ToArray();
        FLLog.Info("Tangents", $"Generated for {ent.Name}");
    }

    record VMeshEntries(VMeshData Data, LUtfNode DataNode, List<VMeshRef> Refs, string Name);

    static void GatherVMeshRef(Dictionary<uint, VMeshEntries> data, LUtfNode modelNode)
    {
        if (modelNode.Children == null)
            return;
        var vmeshpart = modelNode.Children.FirstOrDefault(x =>
            x.Name.Equals("vmeshpart", StringComparison.OrdinalIgnoreCase));
        if (vmeshpart != null)
        {
            var vmeshref = new VMeshRef(vmeshpart.Children[0].Data);
            if (data.TryGetValue(vmeshref.MeshCrc, out var entries))
                entries.Refs.Add(vmeshref);
            else
                FLLog.Warning("Tangents", $"VMeshRef CRC not present {vmeshref.MeshCrc}");
            return;
        }

        var multilevel = modelNode.Children.FirstOrDefault(x =>
            x.Name.Equals("multilevel", StringComparison.OrdinalIgnoreCase));
        if (multilevel == null)
            return;
        foreach (var node in multilevel.Children.Where(x => x.Name.StartsWith(
                     "level", StringComparison.OrdinalIgnoreCase)))
        {
            var levelPart = node.Children[0];
            var vmeshref = new VMeshRef(levelPart.Children[0].Data);
            if (data.TryGetValue(vmeshref.MeshCrc, out var entries))
                entries.Refs.Add(vmeshref);
            else
                FLLog.Warning("Tangents", $"VMeshRef CRC not present {vmeshref.MeshCrc}");
        }
    }

    static void GatherVMeshLibrary(Dictionary<uint, VMeshEntries> data, LUtfNode vmeshLibrary)
    {
        foreach (var child in vmeshLibrary.Children)
        {
            var vms = child.Children.FirstOrDefault(x =>
                x.Name.Equals("vmeshdata", StringComparison.OrdinalIgnoreCase));
            if (vms == null)
                continue;
            var v = new VMeshData(vms.Data, child.Name);
            data[CrcTool.FLModelCrc(child.Name)] = new(v, vms, new List<VMeshRef>(), child.Name);
        }
    }

    class VMeshTangents(VMeshData d, TMeshHeader header, int startVertex, Vector4[] tangents)
        : ITangentGeometry
    {
        public int GetNumFaces() => header.NumRefVertices / 3;

        public int GetNumVerticesOfFace(int index) => 3;

        int GetIndex(int faceIndex, int faceVertex) =>
            startVertex + header.StartVertex + d.Indices[header.TriangleStart + (faceIndex * 3) + faceVertex];

        public Vector3 GetPosition(int faceIndex, int faceVertex)
            => d.GetPosition(GetIndex(faceIndex, faceVertex));

        public Vector3 GetNormal(int faceIndex, int faceVertex)
            => d.GetNormal(GetIndex(faceIndex, faceVertex));

        public Vector2 GetTexCoord(int faceIndex, int faceVertex)
            => d.GetTexCoord(GetIndex(faceIndex, faceVertex), 0);

        public void SetTangent(Vector4 tangent, int faceIndex, int faceVertex)
            => tangents[GetIndex(faceIndex, faceVertex)] = tangent;
    }
}
