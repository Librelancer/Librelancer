// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;

namespace LibreLancer.ContentEdit.Model
{
    public class DumpObject
    {
        public static string DumpVmeshRef(VMeshRef vms)
        {
            var builder = new StringBuilder();
            builder.AppendLine("VMeshRef");
            builder.AppendLine("========");
            builder.AppendLine($"Header Size: {vms.HeaderSize}");
            builder.AppendLine($"VMeshData: 0x{vms.MeshCrc:X}");
            builder.AppendLine($"Vertex Start: {vms.StartVertex}");
            builder.AppendLine($"Vertex Count: {vms.VertexCount}");
            builder.AppendLine($"Index Start: {vms.StartIndex}");
            builder.AppendLine($"Index Count: {vms.IndexCount}");
            builder.AppendLine($"Group Start: {vms.StartMesh}");
            builder.AppendLine($"Group Count: {vms.MeshCount}");
            builder.AppendLine("Bounding Box");
            builder.AppendLine($"Min: ({FmtFloat(vms.BoundingBox.Min.X)}, {FmtFloat(vms.BoundingBox.Min.Y)}, {FmtFloat(vms.BoundingBox.Min.Z)})");
            builder.AppendLine($"Max: ({FmtFloat(vms.BoundingBox.Max.X)}, {FmtFloat(vms.BoundingBox.Max.Y)}, {FmtFloat(vms.BoundingBox.Max.Z)})");
            builder.AppendLine("Bounding Sphere");
            builder.AppendLine($"Center: ({FmtFloat(vms.Center.X)}, {FmtFloat(vms.Center.Y)}, {FmtFloat(vms.Center.Z)})");
            builder.AppendLine($"Radius: {FmtFloat(vms.Radius)}");
            return builder.ToString();
        }

        static string FmtFloat(float f) => f.ToString("#0.########");
        static string FmtNorm(float x) => x.ToString("0.00000").PadLeft(9);


        //TODO: This breaks due to the engine adding extra normals where they shouldn't be
        public static string DumpVmeshData(VMeshData vms)
        {
            var writer = new StringWriter();
            writer.WriteLine("---- HEADER ----\n");
            writer.WriteLine("Number of Meshes          = {0}", vms.Meshes.Length);
            writer.WriteLine("Total referenced vertices = {0}", vms.Indices.Length);
            writer.WriteLine("Flexible Vertex Format    = 0x{0}", ((int)vms.VertexFormat.FVF).ToString("X"));
            writer.WriteLine("Total number of vertices  = {0}", vms.VertexCount);
            writer.WriteLine("\n---- MESHES ----\n");
            writer.WriteLine("Mesh Number  MaterialID  Start Vertex  End Vertex  Start Triangle  NumRefVertex");
            for (int i = 0; i < vms.Meshes.Length; i++)
            {
                var m = vms.Meshes[i];
                writer.WriteLine("{0}  {1}  {2}  {3}  {4}  {5}",
                    i.ToString().PadLeft(11),
                    ("0x" + m.MaterialCrc.ToString("X")).PadLeft(10),
                    m.StartVertex.ToString().PadLeft(12),
                    m.EndVertex.ToString().PadLeft(10),
                    m.TriangleStart.ToString().PadLeft(14),
                    m.NumRefVertices.ToString().PadLeft(12));
            }
            writer.WriteLine("\n---- Triangles ----\n");
            writer.WriteLine("Triangle  Vertex 1  Vertex 2  Vertex 3");
            for (int i = 0; i < vms.Indices.Length; i += 3)
            {
                writer.WriteLine("{0}  {1}  {2}  {3}",
                    (i / 3).ToString().PadLeft(8),
                    vms.Indices[i].ToString().PadLeft(8),
                    vms.Indices[i + 1].ToString().PadLeft(8),
                    vms.Indices[i + 2].ToString().PadLeft(8));
            }
            writer.WriteLine("\n---- Vertices ----\n");
            //Heading
            writer.Write("Vertex    ----X----,   ----Y----,   ----Z----");
            if (vms.VertexFormat.Normal) {
                writer.Write(",    Normal X,    Normal Y,    Normal Z");
            }
            if (vms.VertexFormat.Diffuse) {
                writer.Write("    -Diffuse-,");
            }
            for (int i = 0; i < vms.VertexFormat.TexCoords; i++) {
                writer.Write($",    ----U{i + 1}---,    ----V{i + 1}---");
            }
            writer.WriteLine();
            //Table
            for (int i = 0; i < vms.VertexCount; i++)
            {
                var pos = vms.GetPosition(i);
                writer.Write("{0}{1},{2},{3}",
                    i.ToString().PadLeft(6),
                    pos.X.ToStringInvariant().PadLeft(13),
                    pos.Y.ToStringInvariant().PadLeft(12),
                    pos.Z.ToStringInvariant().PadLeft(12));
                if (vms.VertexFormat.Normal)
                {
                    var n = vms.GetNormal(i);
                    writer.Write(",    {0},    {1},   {2}", FmtNorm(n.X), FmtNorm(n.Y), FmtNorm(n.Z));
                }
                if (vms.VertexFormat.Diffuse)
                {
                    writer.Write(",     {0}", vms.GetDiffuse(i).ToString("X8"));
                }
                for (int u = 0; u < vms.VertexFormat.TexCoords; u++)
                {
                    var t = vms.GetTexCoord(i, u);
                    writer.Write(",    {0},   {1}", FmtNorm(t.X), FmtNorm(t.Y));
                }
                writer.WriteLine();
            }
            return writer.ToString();
        }
    }
}
