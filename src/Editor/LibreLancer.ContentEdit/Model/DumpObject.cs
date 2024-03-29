﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Text;
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
            writer.WriteLine("Flexible Vertex Format    = 0x{0}", ((int)vms.OriginalFVF).ToString("X"));
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
            if (vms.verticesVertexPosition != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----");
            if (vms.verticesVertexPositionTexture != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    ----U----,   ----V----");
            if (vms.verticesVertexPositionNormal != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z");
            if (vms.verticesVertexPositionNormalTexture != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    ----U----,   ----V----");
            if (vms.verticesVertexPositionNormalTextureTwo != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    ----U----,   ----V----,    ----U2---,    ----V2---");
            if (vms.verticesVertexPositionNormalDiffuseTexture != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    -Diffuse-,    ----U----,   ----V----");
            if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null)
                writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    -Diffuse-,    ----U----,   ----V----,    ----U2---,    ----V2---");
            //Table
            for (int i = 0; i < vms.VertexCount; i++)
            {
                if (vms.verticesVertexPosition != null)
                    writer.WriteLine("{0}{1},{2},{3}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPosition[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPosition[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPosition[i].Position.Z.ToString().PadLeft(12));
                if (vms.verticesVertexPositionTexture != null)
                    writer.WriteLine("{0}{1},{2},{3},    {4},   {5}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPositionTexture[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPositionTexture[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPositionTexture[i].Position.Z.ToString().PadLeft(12),
                        FmtNorm(vms.verticesVertexPositionTexture[i].TextureCoordinate.X),
                        FmtNorm(vms.verticesVertexPositionTexture[i].TextureCoordinate.Y));
                if (vms.verticesVertexPositionNormal != null) { }
                if (vms.verticesVertexPositionNormalTexture != null)
                    writer.WriteLine("{0}{1},{2},{3},  {4},     {5},    {6},     {7},    {8}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPositionNormalTexture[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPositionNormalTexture[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPositionNormalTexture[i].Position.Z.ToString().PadLeft(12),
                        FmtNorm(vms.verticesVertexPositionNormalTexture[i].Normal.X),
                        FmtNorm(vms.verticesVertexPositionNormalTexture[i].Normal.Y),
                        FmtNorm(vms.verticesVertexPositionNormalTexture[i].Normal.Z),
                        FmtNorm(vms.verticesVertexPositionNormalTexture[i].TextureCoordinate.X),
                        FmtNorm(vms.verticesVertexPositionNormalTexture[i].TextureCoordinate.Y));
                if (vms.verticesVertexPositionNormalTextureTwo != null)
                    writer.WriteLine("{0}{1},{2},{3},    {4},    {5},   {6},    {7},   {8}    {9},   {10}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPositionNormalTextureTwo[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPositionNormalTextureTwo[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPositionNormalTextureTwo[i].Position.Z.ToString().PadLeft(12),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].Normal.X),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].Normal.Y),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].Normal.Z),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinate.X),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinate.Y),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinateTwo.X),
                        FmtNorm(vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinateTwo.Y));
                if (vms.verticesVertexPositionNormalDiffuseTexture != null)
                    writer.WriteLine("{0}{1},{2},{3},    {4},    {5},   {6},    {7},    {8},   {9}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPositionNormalDiffuseTexture[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPositionNormalDiffuseTexture[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPositionNormalDiffuseTexture[i].Position.Z.ToString().PadLeft(12),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTexture[i].Normal.X),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTexture[i].Normal.Y),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTexture[i].Normal.Z),
                        " " + vms.verticesVertexPositionNormalDiffuseTexture[i].Diffuse.ToString("XXXXXXXX"),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTexture[i].TextureCoordinate.X),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTexture[i].TextureCoordinate.Y));
                if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null)
                {
                    writer.WriteLine("{0}{1},{2},{3},    {4},    {5},   {6},    {7},    {8},   {9},   {10},   {11}",
                        i.ToString().PadLeft(6),
                        vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Position.X.ToString().PadLeft(13),
                        vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Position.Y.ToString().PadLeft(12),
                        vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Position.Z.ToString().PadLeft(12),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Normal.X),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Normal.Y),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Normal.Z),
                        " " + vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Diffuse.ToString("XXXXXXXX"),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinate.X),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinate.Y),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinateTwo.X),
                        FmtNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinateTwo.Y));
                }
            }

            return writer.ToString();
        }
    }
}