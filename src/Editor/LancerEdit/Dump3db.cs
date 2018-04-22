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
using System.IO;
using LibreLancer.Utf.Cmp;

namespace LancerEdit
{
    public enum Dump3dbStatus 
    {
        Ok,
        ColorNotExported,
        TexCoord2NotExported,
        Fail
    }
    public class Dump3db
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
        public static Dump3dbStatus DumpObj(ModelFile model, string path)
        {
            try
            {
                var status = Dump3dbStatus.Ok;
                using (var writer = new StreamWriter(path))
                {
                    bool normals = false;
                    bool texcoords = false;
                    writer.WriteLine("# exported 3db");
                    var vms = model.Levels[0].Mesh;
                    if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null ||
                       vms.verticesVertexPositionNormalColorTexture != null)
                    {
                        writer.WriteLine("# WARNING: Color not exported");
                        status = Dump3dbStatus.ColorNotExported;
                    }
                    if (vms.verticesVertexPositionNormalTextureTwo != null)
                    {
                        writer.WriteLine("# WARNING: 2nd texcoord not exported");
                        status = Dump3dbStatus.TexCoord2NotExported;
                    }

                    for (int i = 0; i < vms.VertexCount; i++)
                    {
                        if (vms.verticesVertexPosition != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPosition[i].Position);
                        }
                        if (vms.verticesVertexPositionNormal != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormal[i].Position);
                            WriteVector(FMT_VN, writer, vms.verticesVertexPositionNormal[i].Normal);
                            normals = true;
                        }
                        if (vms.verticesVertexPositionTexture != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormalTexture[i].Position);
                            WriteVector(writer, vms.verticesVertexPositionNormalTexture[i].TextureCoordinate);
                            texcoords = true;
                        }
                        if (vms.verticesVertexPositionNormalTexture != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormalTexture[i].Position);
                            WriteVector(FMT_VN, writer, vms.verticesVertexPositionNormalTexture[i].Normal);
                            WriteVector(writer, vms.verticesVertexPositionNormalTexture[i].TextureCoordinate);
                            normals = texcoords = true;
                        }
                        if (vms.verticesVertexPositionNormalTextureTwo != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormalTextureTwo[i].Position);
                            WriteVector(FMT_VN, writer, vms.verticesVertexPositionNormalTextureTwo[i].Normal);
                            WriteVector(writer, vms.verticesVertexPositionNormalTextureTwo[i].TextureCoordinate);
                            normals = texcoords = true;
                        }
                        if (vms.verticesVertexPositionNormalColorTexture != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormalColorTexture[i].Position);
                            WriteVector(FMT_VN, writer, vms.verticesVertexPositionNormalColorTexture[i].Normal);
                            WriteVector(writer, vms.verticesVertexPositionNormalColorTexture[i].TextureCoordinate);
                            normals = texcoords = true;
                        }
                        if (vms.verticesVertexPositionNormalDiffuseTextureTwo != null)
                        {
                            WriteVector(FMT_V, writer, vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Position);
                            WriteVector(FMT_VN, writer, vms.verticesVertexPositionNormalDiffuseTextureTwo[i].Normal);
                            WriteVector(writer, vms.verticesVertexPositionNormalDiffuseTextureTwo[i].TextureCoordinate);
                            normals = texcoords = true;
                        }
                    }

                    var format = "f {0} {1} {2}";
                    if (normals && texcoords)
                        format = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
                    else if (normals)
                        format = "f {0}//{0} {1}//{1} {2}//{2}";
                    else if (texcoords)
                        format = "f {0}/{0} {1}/{1} {2}/{2}";

                    writer.WriteLine("o exported3db");
                    int partIndex = 1;
                    for (int j = model.Levels[0].StartMesh; j < model.Levels[0].StartMesh + model.Levels[0].MeshCount; j++)
                    {
                        writer.WriteLine("g vms{0}", partIndex++);
                        var header = vms.Meshes[j];
                        var ofs = header.StartVertex + model.Levels[0].StartVertex;
                        var idxOffset = model.Levels[0].StartIndex + header.TriangleStart;
                        for (int i = 0; i < header.NumRefVertices; i += 3)
                        {
                            var idxA = ofs + vms.Indices[idxOffset + i] + 1;
                            var idxB = ofs + vms.Indices[idxOffset + i + 1] + 1;
                            var idxC = ofs + vms.Indices[idxOffset + i + 2] + 1;
                            writer.WriteLine(format, idxA, idxB, idxC);
                        }
                    }
                    return status;
                }
            }
            catch (Exception)
            {
                return Dump3dbStatus.Fail;
            }
        }
    }
}
