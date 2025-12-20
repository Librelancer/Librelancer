// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer.Data;
using SimpleMesh;
using LibreLancer.Utf.Vms;

namespace LibreLancer.ContentEdit.Model
{

    public class GeometryWriter
    {
        public static byte[] NullVMeshRef()
        {
            var bytes = new byte[60];
            BitConverter.TryWriteBytes(bytes, (uint)60);
            return bytes;
        }
        public static byte[] VMeshRef(Geometry g, string nodename)
        {
            using(var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);

                writer.Write((uint)60); //HeaderSize
                writer.Write(CrcTool.FLModelCrc(nodename));
                //Fields used for referencing sections of VMeshData
                writer.Write((ushort)0); //StartVertex - BaseVertex in drawcall
                writer.Write((ushort)g.Vertices.Length); //VertexCount (idk?)
                writer.Write((ushort)0); //StartIndex
                writer.Write((ushort)g.Indices.Length); //IndexCount
                writer.Write((ushort)0); //StartMesh
                writer.Write((ushort)g.Groups.Length); //MeshCount
                //Write rendering things
                writer.Write(g.Max.X);
                writer.Write(g.Min.X);
                writer.Write(g.Max.Y);
                writer.Write(g.Min.Y);
                writer.Write(g.Max.Z);
                writer.Write(g.Min.Z);


                writer.Write(g.Center.X);
                writer.Write(g.Center.Y);
                writer.Write(g.Center.Z);

                writer.Write(g.Radius);
                return stream.ToArray();
            }
        }

        public static D3DFVF FVF(Geometry g, bool withTangents)
        {
            D3DFVF fvf = D3DFVF.XYZ;
            if ((g.Attributes & VertexAttributes.Normal) == VertexAttributes.Normal)
                fvf |= D3DFVF.NORMAL;
            if ((g.Attributes & VertexAttributes.Diffuse) == VertexAttributes.Diffuse)
                fvf |= D3DFVF.DIFFUSE;
            int texCount = 0;
            if ((g.Attributes & VertexAttributes.Texture2) == VertexAttributes.Texture2)
                texCount = 2;
            else if ((g.Attributes & VertexAttributes.Texture1) == VertexAttributes.Texture1)
                texCount = 1;
            if ((g.Attributes & VertexAttributes.Tangent) == VertexAttributes.Tangent)
                texCount += 2;
            fvf |= texCount switch
            {
                1 => D3DFVF.TEX1,
                2 => D3DFVF.TEX2,
                3 => D3DFVF.TEX3,
                4 => D3DFVF.TEX4,
                _ => 0,
            };
            return fvf;
        }
        public static byte[] VMeshData(Geometry g, bool withTangents, D3DFVF? overrideFVF = null)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((uint)0x01); //MeshType
                writer.Write((uint)0x04); //SurfaceType
                writer.Write((ushort)(g.Groups.Length)); //MeshCount
                writer.Write((ushort)(g.Indices.Length)); //IndexCount
                D3DFVF fvf = overrideFVF ?? FVF(g, withTangents);
                writer.Write((ushort)fvf); //FVF
                writer.Write((ushort)g.Vertices.Length); //VertexCount

                int startTri = 0;
                foreach(var dc in g.Groups) {
                    //drawcalls must be sequential (start index isn't in VMeshData)
                    //this error shouldn't ever throw
                    if (startTri != dc.StartIndex) throw new Exception("Invalid start index");
                    //write TMeshHeader
                    var crc = dc.Material != null ? CrcTool.FLModelCrc(dc.Material.Name) : 0;
                    writer.Write(crc);
                    writer.Write((ushort)dc.BaseVertex);
                    int max = 0;
                    for (int i = 0; i < dc.IndexCount; i++)
                    {
                        max = Math.Max(max, g.Indices.Indices16[i + dc.StartIndex]);
                    }

                    max += dc.BaseVertex;
                    writer.Write((ushort)max);
                    writer.Write((ushort)dc.IndexCount); //NumRefVertices
                    writer.Write((ushort)0xCC); //Padding
                    //validation
                    startTri += dc.IndexCount;
                }

                var desc = new FVFVertex(fvf);


                foreach (var idx in g.Indices.Indices16) writer.Write(idx);
                foreach(var v in g.Vertices) {
                    writer.Write(v.Position.X);
                    writer.Write(v.Position.Y);
                    writer.Write(v.Position.Z);
                    if((fvf & D3DFVF.NORMAL) == D3DFVF.NORMAL) {
                        writer.Write(v.Normal.X);
                        writer.Write(v.Normal.Y);
                        writer.Write(v.Normal.Z);
                    }
                    if ((fvf & D3DFVF.DIFFUSE) == D3DFVF.DIFFUSE) {
                        writer.Write((VertexDiffuse)v.Diffuse.ToSrgb());
                    }

                    if (desc.TexCoords > 0)
                    {
                        writer.Write(v.Texture1.X);
                        writer.Write(v.Texture1.Y);
                    }
                    if (desc.TexCoords > 1 && desc.TexCoords != 3)
                    {
                        writer.Write(v.Texture2.X);
                        writer.Write(v.Texture2.Y);
                    }
                    if (desc.TexCoords == 3 || desc.TexCoords == 4)
                    {
                        writer.Write(v.Tangent.X);
                        writer.Write(v.Tangent.Y);
                        writer.Write(v.Tangent.Z);
                        writer.Write(v.Tangent.W);
                    }
                }
                return stream.ToArray();
            }
        }
    }


}
