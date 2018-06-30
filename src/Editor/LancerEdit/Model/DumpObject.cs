/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Utf.Cmp;
using LibreLancer;
using LibreLancer.Utf.Vms;

namespace LancerEdit
{
    public enum DumpObjectStatus 
    {
        Ok,
        ColorNotExported,
        TexCoord2NotExported,
        Fail
    }
    public class DumpObject
    {
        const string FMT_V = "v {0} {1} {2}";
        const string FMT_VN = "vn {0} {1} {2}";

        static string FmtFloat(float f) => f.ToString("#0.########");

        static void WriteVector(string fmt, StreamWriter writer, LibreLancer.Vector3 vector)
        {
            writer.WriteLine(fmt, FmtFloat(vector.X), FmtFloat(vector.Y), FmtFloat(vector.Z));
        }
        static void WriteVector(StreamWriter writer, LibreLancer.Vector2 vector)
        {
            writer.WriteLine("vt {0} {1}", FmtFloat(vector.X), FmtFloat(vector.Y));
        }
        static Vector3 TrNorm(Vector3 n, Matrix4 mat)
        {
            return (mat * new Vector4(n, 0)).Xyz;
        }

        static void WriteVertices(ref DumpObjectStatus status, ref bool normals, ref bool texcoords, StreamWriter writer, VMeshData vms, Matrix4 mat)
        {
            if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null ||
                      vms.verticesVertexPositionNormalColorTexture != null)
            {
                writer.WriteLine("# WARNING: Color not exported");
                status = DumpObjectStatus.ColorNotExported;
            }
            if (vms.verticesVertexPositionNormalTextureTwo != null)
            {
                writer.WriteLine("# WARNING: 2nd texcoord not exported");
                status = DumpObjectStatus.TexCoord2NotExported;
            }
            var nm = mat.Inverted();
            nm.Transpose();
            for (int i = 0; i < vms.VertexCount; i++)
            {
                if (vms.verticesVertexPosition != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPosition[i].Position));
                }
                if (vms.verticesVertexPositionNormal != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormal[i].Position));
                    WriteVector(FMT_VN, writer, TrNorm(vms.verticesVertexPositionNormal[i].Normal,nm));
                    normals = true;
                }
                if (vms.verticesVertexPositionTexture != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormalTexture[i].Position));
                    WriteVector(writer, vms.verticesVertexPositionNormalTexture[i].TextureCoordinate);
                    texcoords = true;
                }
                if (vms.verticesVertexPositionNormalTexture != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormalTexture[i].Position));
                    WriteVector(FMT_VN, writer, TrNorm(vms.verticesVertexPositionNormalTexture[i].Normal,nm));
                    WriteVector(writer, vms.verticesVertexPositionNormalTexture[i].TextureCoordinate);
                    normals = texcoords = true;
                }
                if (vms.verticesVertexPositionNormalTextureTwo != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormalTextureTwo[i].Position));
                    WriteVector(FMT_VN, writer, TrNorm(vms.verticesVertexPositionNormalTextureTwo[i].Normal,nm));
                    WriteVector(writer, vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinate);
                    normals = texcoords = true;
                }
                if (vms.verticesVertexPositionNormalColorTexture != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormalColorTexture[i].Position));
                    WriteVector(FMT_VN, writer, TrNorm(vms.verticesVertexPositionNormalColorTexture[i].Normal,nm));
                    WriteVector(writer, vms.verticesVertexPositionNormalColorTexture[i].TextureCoordinate);
                    normals = texcoords = true;
                }
                if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null)
                {
                    WriteVector(FMT_V, writer, mat.Transform(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Position));
                    WriteVector(FMT_VN, writer, TrNorm(vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Normal,nm));
                    WriteVector(writer, vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinate);
                    normals = texcoords = true;
                }
            }
        }
        static void WriteIndices(PartInfo p, StreamWriter writer)
        {
            var format = "f {0} {1} {2}";
            if (p.Normals && p.Texcoords)
                format = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
            else if (p.Normals)
                format = "f {0}//{0} {1}//{1} {2}//{2}";
            else if (p.Texcoords)
                format = "f {0}/{0} {1}/{1} {2}/{2}";
            
            writer.WriteLine("o {0}", p.Name);
            int partIndex = 1;
            for (int j = p.Ref.StartMesh; j < p.Ref.StartMesh + p.Ref.MeshCount; j++)
            {
                writer.WriteLine("g {1}_vms{0}", partIndex++, p.Name);
                var header = p.Ref.Mesh.Meshes[j];
                var ofs = header.StartVertex + p.Ref.StartVertex + p.Offset;
                var idxOffset = header.TriangleStart;
                for (int i = 0; i < header.NumRefVertices; i += 3)
                {
                    var idxA = ofs + p.Ref.Mesh.Indices[idxOffset + i];
                    var idxB = ofs + p.Ref.Mesh.Indices[idxOffset + i + 1];
                    var idxC = ofs + p.Ref.Mesh.Indices[idxOffset + i + 2];
                    writer.WriteLine(format, idxA, idxB, idxC);
                }
            }
        }
        class PartInfo
        {
            public string Name;
            public bool Normals;
            public bool Texcoords;
            public int Offset;
            public VMeshRef Ref;
        }
        static string GetPath(string path, int cOffset)
        {
            if (path == null || path == "/")
                return "model_" + cOffset;
            return path;
        }
        public static DumpObjectStatus DumpObj(IDrawable model, string path)
        {
            try
            {
                var status = DumpObjectStatus.Ok;
                using (var writer = new StreamWriter(path))
                {
                    if(model is CmpFile) {
                        writer.WriteLine("# exported cmp");
                        List<PartInfo> infos = new List<PartInfo>();
                        int cOffset = 1;
                        foreach(var part in ((CmpFile)model).Parts) {
                            var vms = part.Model.Levels[0].Mesh;
                            var info = new PartInfo() { 
                                Ref = part.Model.Levels[0], 
                                Offset = cOffset,
                                Name = GetPath(part.Model.Path,cOffset)
                            };
                            var mat = part.Construct == null ? Matrix4.Identity : part.Construct.Transform;
                            WriteVertices(ref status, ref info.Normals, ref info.Texcoords, writer, vms, mat);
                            infos.Add(info);
                            cOffset += vms.VertexCount;
                        }
                        foreach (var info in infos)
                            WriteIndices(info, writer);
                    } else if(model is ModelFile ){
                        writer.WriteLine("# exported 3db");
                        var mdl = (ModelFile)model;
                        var info = new PartInfo()
                        {
                            Ref = mdl.Levels[0],
                            Offset = 1,
                            Name = GetPath(mdl.Path,1)
                        };
                        WriteVertices(ref status, ref info.Normals, ref info.Texcoords, writer, mdl.Levels[0].Mesh, Matrix4.Identity);
                        WriteIndices(info, writer);
                    } else {
                        return DumpObjectStatus.Fail;
                    }
                    return status;
                }
            }
            catch (Exception)
            {
                return DumpObjectStatus.Fail;
            }
        }
        static string FmtNorm(float x) => x.ToString("0.00000").PadLeft(9);
        public static void DumpVmeshData(string output, VMeshData vms)
        {
            using(var writer = new StreamWriter(output)) {
                writer.WriteLine("---- HEADER ----\n");
                writer.WriteLine("Mesh Type                 = {0}", vms.MeshType);
                writer.WriteLine("Surface Type              = {0}", vms.SurfaceType);
                writer.WriteLine("Number of Meshes          = {0}", vms.MeshCount);
                writer.WriteLine("Total referenced vertices = {0}", vms.IndexCount);
                writer.WriteLine("Flexible Vertex Format    = 0x{0}", ((int)vms.FlexibleVertexFormat).ToString("X"));
                writer.WriteLine("Total number of vertices  = {0}", vms.VertexCount);
                writer.WriteLine("\n---- MESHES ----\n");
                writer.WriteLine("Mesh Number  MaterialID  Start Vertex  End Vertex  Start Triangle  NumRefVertex");
                for (int i = 0; i < vms.MeshCount; i++) {
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
                for (int i = 0; i < vms.IndexCount; i+= 3) {
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
                if(vms.verticesVertexPositionTexture != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    ----U----,   ----V----");
                if (vms.verticesVertexPositionNormal != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z");
                if (vms.verticesVertexPositionNormalTexture != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    ----U----,   ----V----");
                if(vms.verticesVertexPositionNormalTextureTwo != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    ----U----,   ----V----,    ----U2---,    ----V2---");
                if (vms.verticesVertexPositionNormalColorTexture != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    -Diffuse-,    ----U----,   ----V----");
                if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null)
                    writer.WriteLine("Vertex    ----X----,   ----Y----,   ----Z----,    Normal X,    Normal Y,    Normal Z,    -Diffuse-,    ----U----,   ----V----,    ----U2---,    ----V2---");
                //Table
                for (int i = 0; i < vms.VertexCount; i++) {
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
                    if (vms.verticesVertexPositionNormalColorTexture != null)
                        writer.WriteLine("{0}{1},{2},{3},    {4},    {5},   {6},    {7},    {8},   {9}",
                                       i.ToString().PadLeft(6),
                                       vms.verticesVertexPositionNormalColorTexture[i].Position.X.ToString().PadLeft(13),
                                       vms.verticesVertexPositionNormalColorTexture[i].Position.Y.ToString().PadLeft(12),
                                        vms.verticesVertexPositionNormalColorTexture[i].Position.Z.ToString().PadLeft(12),
                                         FmtNorm(vms.verticesVertexPositionNormalColorTexture[i].Normal.X),
                                         FmtNorm(vms.verticesVertexPositionNormalColorTexture[i].Normal.Y),
                                         FmtNorm(vms.verticesVertexPositionNormalColorTexture[i].Normal.Z),
                                         " " + vms.Diffuse[i].ToString("XXXXXXXX"),
                                        FmtNorm(vms.verticesVertexPositionNormalColorTexture[i].TextureCoordinate.X),
                                        FmtNorm(vms.verticesVertexPositionNormalColorTexture[i].TextureCoordinate.Y));
                    if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null) { }
                }

            }
        }
    }
}
