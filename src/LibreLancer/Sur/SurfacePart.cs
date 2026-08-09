// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Physics;

namespace LibreLancer.Sur
{
    public class SurfacePart
    {
        //Format
        private const uint SURF = 0x66727573; //"surf"
        private const uint EXTS = 0x73747865; //"exts"
        private const uint NOT_FIXED = 0x64786621; //!fxd
        private const uint HPID = 0x64697068; //"hpid"
        private const uint SHAPES_MAGIC = 0x45504853; //"SHPE" for detecting librelancer data
        //Freelancer
        public uint Crc;
        public Vector3 Center;
        public Vector3 Inertia;
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Unknown;
        public float Radius;
        public float Scale; // Scale of sphere without hardpoints
        public List<SurfacePoint> Points = [];
        public List<uint> HardpointIds = [];
        public SurfaceNode Root = null!;
        public bool Dynamic = false;
        //Librelancer
        public List<(uint SubId, ConvexShape[] Shapes)>? Shapes;

        public SurfaceHull[] GetHulls(bool wrap)
        {
            var hulls = new List<SurfaceHull>();
            var queue = new Stack<SurfaceNode>();
            queue.Push(Root);
            SurfaceNode target;

            while (queue.Count > 0)
            {
                target = queue.Pop();
                if (target.Right != null) queue.Push(target.Right);
                if (target.Left != null) queue.Push(target.Left);
                if (target.Hull != null && target.Hull.Type == 4) hulls.Add(target.Hull);
            }

            if (wrap && Root.Hull!.Type == 5)
            {
                hulls.Add(Root.Hull);
            }

            return hulls.ToArray();
        }

        public void ConvertShapes()
        {
            Dictionary<uint, List<ConvexShape>> shapes = new();
            shapes[0] = new();
            foreach (var hp in HardpointIds)
                shapes[hp] = new();
            var hulls = GetHulls(false);
            var vertices = new Vector3[Points.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Points[i].Point;
            }
            Parallel.ForEach(hulls, (triHull, _) =>
            {
                if (triHull.Type == 5 ||
                    (triHull.HullId != Crc &&
                     !HardpointIds.Contains(triHull.HullId)))
                {
                    return;
                }
                var target = triHull.HullId == Crc ?
                    shapes[0] : shapes[triHull.HullId];
                var indices = new List<int>();
                foreach (var tri in triHull.Faces)
                {
                    indices.Add(tri.Points.A);
                    indices.Add(tri.Points.B);
                    indices.Add(tri.Points.C);
                }
                var shape = new ConvexShape(new ConvexMesh(vertices, indices.ToArray()));
                lock(target) { target.Add(shape);}
            });
            Shapes = new();
            foreach (var kv in shapes)
            {
                if (kv.Value.Count > 0)
                {
                    Shapes.Add(new(kv.Key, kv.Value.ToArray()));
                }
            }
        }

        public static SurfacePart Read(BinaryReader reader)
        {
            var surf = new SurfacePart
            {
                Crc = reader.ReadUInt32()
            };

            uint sectionCount = reader.ReadUInt32();

            while (sectionCount-- > 0)
            {
                var sectionId = reader.ReadUInt32();

                switch (sectionId)
                {
                    case SURF:
                        surf.ReadSurf(reader);
                        break;
                    case EXTS:
                        surf.Minimum = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        surf.Maximum = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case NOT_FIXED:
                        surf.Dynamic = true;
                        break;
                    case HPID:
                        uint count = reader.ReadUInt32();

                        while (count-- > 0)
                        {
                            surf.HardpointIds.Add(reader.ReadUInt32());
                        }

                        break;
                }
            }

            return surf;
        }

        public void Write(BinaryWriter writer)
        {
            int sectionCount = 2;
            if (Dynamic) sectionCount++;
            if (HardpointIds.Count > 0) sectionCount++;
            writer.Write(Crc);
            writer.Write(sectionCount);
            if (Dynamic) writer.Write(NOT_FIXED);
            writer.Write(EXTS);
            writer.Write(Minimum.X);
            writer.Write(Minimum.Y);
            writer.Write(Minimum.Z);
            writer.Write(Maximum.X);
            writer.Write(Maximum.Y);
            writer.Write(Maximum.Z);
            writer.Write(SURF);
            WriteSurf(writer);

            if (HardpointIds.Count > 0)
            {
                writer.Write(HPID);
                writer.Write(HardpointIds.Count);
                foreach (var id in HardpointIds) writer.Write(id);
            }
        }

        private void WriteSurf(BinaryWriter writer)
        {
            var startOffset = 4 + (int) writer.BaseStream.Position;
            // skip section size and header
            writer.BaseStream.Seek(52, SeekOrigin.Current);

            // Write SurfaceHulls
            var hulls = GetHulls(true);
            var offsets = new int[hulls.Length];

            for (int i = 0; i < hulls.Length; i++)
            {
                offsets[i] = (int) writer.BaseStream.Position;
                writer.Write(0U); // skip offset to point
                hulls[i].Write(writer);
            }

            // Write SurfacePoints
            var pointsStartOffset = (int) writer.BaseStream.Position;
            foreach (var p in Points)
                p.Write(writer);
            var nodesStartOffset = (int) writer.BaseStream.Position;

            // Update offsets in hulls to points start and nodes start for wrap hull
            for (int i = 0; i < hulls.Length; i++)
            {
                var offset = offsets[i];
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(pointsStartOffset - offset);
                if (hulls[i].Type == 5)
                    writer.Write(nodesStartOffset - offset);
            }

            var queue = new Stack<(int parentOffset, SurfaceNode node)>();
            queue.Push((0, Root));

            SurfaceNode currentNode;
            int currentOffset;
            int parentOffset;

            writer.BaseStream.Seek(nodesStartOffset, SeekOrigin.Begin);

            while (queue.Count > 0)
            {
                (parentOffset, currentNode) = queue.Pop();
                currentOffset = (int) writer.BaseStream.Position;

                // Current node is right child of parent
                if (parentOffset > 0)
                {
                    // Update parent right node offset
                    writer.BaseStream.Seek(parentOffset, SeekOrigin.Begin);
                    writer.Write(currentOffset - parentOffset);
                    writer.BaseStream.Seek(currentOffset, SeekOrigin.Begin);
                }

                writer.Write(0U); // right child
                // offset to hull
                if (currentNode.Hull != null)
                    writer.Write(-(currentOffset - offsets[Array.IndexOf(hulls, currentNode.Hull)]));
                else
                    writer.Write(0);

                currentNode.Write(writer);

                if (currentNode.Right != null)
                    queue.Push((currentOffset, currentNode.Right));
                if (currentNode.Left != null)
                    queue.Push((0, currentNode.Left));
            }

            var bitsEnd = (int) writer.BaseStream.Position;
            if (Shapes != null)
            {
                writer.Write(SHAPES_MAGIC);
                writer.Write(Shapes.Count);
                foreach (var sh in Shapes)
                {
                    writer.Write(sh.SubId);
                    writer.Write(sh.Shapes.Length);
                    foreach (var cv in sh.Shapes)
                    {
                        writer.Write(cv.Data.Length);
                        writer.Write(cv.Data);
                    }
                }
            }
            var endOffset = (int) writer.BaseStream.Position;

            writer.BaseStream.Seek(startOffset - 4, SeekOrigin.Begin);

            writer.Write(endOffset - startOffset);

            writer.Write(Center.X);
            writer.Write(Center.Y);
            writer.Write(Center.Z);
            writer.Write(Inertia.X);
            writer.Write(Inertia.Y);
            writer.Write(Inertia.Z);
            writer.Write(Radius);
            var scaleByte = (byte) (Scale * 0xFA);
            writer.Write((bitsEnd - startOffset) << 8 | scaleByte);
            writer.Write(nodesStartOffset - startOffset);
            writer.Write(Unknown.X);
            writer.Write(Unknown.Y);
            writer.Write(Unknown.Z);

            writer.BaseStream.Seek(endOffset, SeekOrigin.Begin);
        }

        private void ReadSurf(BinaryReader reader)
        {
            var size = reader.ReadInt32(); // size of the section from AFTER this field
            var startOffset = (int) reader.BaseStream.Position;
            var endOffset = (int) reader.BaseStream.Position + size;

            Center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Inertia = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Radius = reader.ReadSingle();
            Scale = (float) reader.ReadByte() / 0xFA;
            var bitsEnd = (int) reader.ReadUInt24();
            var bitsStart = (int) reader.ReadUInt32();
            // Unknown Vector3

            var nodesEndOffset = startOffset + bitsEnd;
            var nodesStartOffset = startOffset + bitsStart;
            var pointsStartOffset = 0;

            reader.BaseStream.Seek(12, SeekOrigin.Current);

            var nodeOffset = 0;
            var rightOffset = 0;
            var leftOffset = 0;
            var hullOffset = 0;

            SurfaceNode? parentNode = null;
            SurfaceNode? currentNode = null;

            var queue = new Stack<(SurfaceNode? node, int offset)>();
            queue.Push((null, nodesStartOffset));

            // Read nodes in order as they appear in tree
            while (queue.Count > 0)
            {
                var n = queue.Pop();
                parentNode = n.node;
                nodeOffset = n.offset;

                if (nodeOffset < nodesStartOffset || nodeOffset > nodesEndOffset)
                    throw new Exception("Node offset out of bounds " + nodeOffset);
                reader.BaseStream.Seek(nodeOffset, SeekOrigin.Begin);

                if ((rightOffset = reader.ReadInt32()) != 0) rightOffset += nodeOffset; // offset to right child
                if ((hullOffset = reader.ReadInt32()) != 0) hullOffset += nodeOffset; // offset to associated hull

                currentNode = SurfaceNode.Read(reader);
                leftOffset = (int) reader.BaseStream.Position;

                if (parentNode == null)
                {
                    Root = currentNode;
                }
                else
                {
                    if (parentNode.Left == null) parentNode.Left = currentNode;
                    else if (parentNode.Right == null) parentNode.Right = currentNode;
                }

                if (hullOffset > 0)
                {
                    reader.BaseStream.Seek(hullOffset, SeekOrigin.Begin);
                    pointsStartOffset = reader.ReadInt32() + hullOffset;
                    currentNode.Hull = SurfaceHull.Read(reader);
                }

                if (currentNode.Hull == null || currentNode.Hull.Type == 5)
                {
                    if (rightOffset != 0) queue.Push((currentNode, rightOffset));
                    if (leftOffset != 0) queue.Push((currentNode, leftOffset));
                }
            }

            reader.BaseStream.Seek(pointsStartOffset, SeekOrigin.Begin);

            while (reader.BaseStream.Position < nodesStartOffset)
            {
                Points.Add(SurfacePoint.Read(reader));
            }

            if (size > bitsEnd)
            {
                reader.BaseStream.Seek(nodesEndOffset, SeekOrigin.Begin);
                if (reader.ReadUInt32() == SHAPES_MAGIC)
                {
                    int shapeCount = reader.ReadInt32();
                    Shapes = new(shapeCount);
                    while (shapeCount-- > 0)
                    {
                        var subId = reader.ReadUInt32();
                        var count = reader.ReadInt32();
                        var cvs = new ConvexShape[count];
                        for (int i = 0; i < cvs.Length; i++)
                        {
                            var len = reader.ReadInt32();
                            cvs[i] = new ConvexShape(reader.ReadBytes(len));
                        }
                        Shapes.Add((subId, cvs));
                    }
                }
            }

            reader.BaseStream.Seek(endOffset, SeekOrigin.Begin);
        }

        public override string ToString() => $"Surface (ID: 0x{Crc})";
    }
}
