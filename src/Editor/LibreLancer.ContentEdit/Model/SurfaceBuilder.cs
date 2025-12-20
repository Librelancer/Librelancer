using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using LibreLancer.Data;
using LibreLancer.Sur;
using LibreLancer.Utf;
using SimpleMesh;
using SimpleMesh.Convex;

namespace LibreLancer.ContentEdit.Model;

public static class SurfaceBuilder
{
    public static bool VerifyWriteSur(SurFile f)
    {
        var originalDescription = DescribeSur(f);
        using (var ms = new MemoryStream())
        {
            f.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var f2 = SurFile.Read(ms);
            var verifyDescription = DescribeSur(f2);
            return originalDescription == verifyDescription;
        }
    }

    static string DescribeSur(SurFile f)
    {
        var builder = new StringBuilder();
        builder.AppendLine("SUR");
        foreach (var p in f.Surfaces.OrderBy((x => x.Crc)))
        {
            builder.AppendLine(">> PART >>");
            builder.AppendFmtLine(p.Crc);
            builder.AppendFmtLine(p.Center);
            builder.AppendFmtLine(p.Minimum);
            builder.AppendFmtLine(p.Maximum);
            builder.AppendFmtLine(p.Unknown);
            builder.AppendFmtLine(p.Radius);
            builder.AppendFmtLine((byte)(p.Scale * 0xFA));
            foreach (var point in p.Points)
            {
                builder.AppendLine(point.Mesh + " " + point.Point.ToString());
            }

            foreach (var id in p.HardpointIds)
            {
                builder.AppendFmtLine(id);
            }

            DescribeNode(p.Root, builder);
        }

        return builder.ToString();
    }

    static void DescribeNode(SurfaceNode node, StringBuilder builder)
    {
        if (node == null)
        {
            builder.AppendLine("->NODENULL");
            return;
        }

        builder.AppendFmtLine(node.Center);
        builder.AppendFmtLine(node.Radius);
        builder.AppendFmtLine((byte)(node.Scale.X * 0xFA));
        builder.AppendFmtLine((byte)(node.Scale.Y * 0xFA));
        builder.AppendFmtLine((byte)(node.Scale.Z * 0xFA));
        builder.AppendFmtLine(node.Unknown);
        DescribeHull(node.Hull, builder);
        DescribeNode(node.Left, builder);
        DescribeNode(node.Right, builder);
    }

    static void DescribeHull(SurfaceHull hull, StringBuilder builder)
    {
        if (hull == null)
        {
            builder.AppendLine("+>HULLNULL");
            return;
        }
        else
        {
            builder.AppendLine($">>HULL {hull.ToString()}");
        }

        builder.AppendFmtLine(hull.Unknown);
        foreach (var f in hull.Faces)
        {
            builder.AppendLine($"{f.Flag} {f.Index} {f.Opposite} {f.Points} {f.Shared} {f.Flags}");
        }
    }

    public static bool HasHulls(ImportedModel model) => NodeHasHulls(model.Root);

    static bool NodeHasHulls(ImportedModelNode node)
    {
        if (node.Hulls.Count > 0) return true;
        foreach (var c in node.Children)
            if (NodeHasHulls(c))
                return true;
        return false;
    }

    public static EditResult<SurFile> CreateSur(ImportedModel model, bool forceCompound = false)
    {
        List<EditMessage> warnings = new List<EditMessage>();
        var parts = new List<SurfacePart>();
        var nodeKind = !forceCompound && model.Root.Children.Count == 0 ? NodeKind.Node3db : NodeKind.NodeRoot;
        CreateSurfacePart(model.Root, parts, null, nodeKind, warnings);
        if (parts.Count == 0)
        {
            return EditResult<SurFile>.Error("No valid hulls", warnings);
        }

        parts[0].Dynamic = true;
        var result = new SurFile() { Surfaces = parts };
        if (!VerifyWriteSur(result))
            warnings.Add(EditMessage.Warning("Verify writing sur failed"));
        return new EditResult<SurFile>(result, warnings);
    }

    static HullData NodeToHull(ModelNode h, Matrix4x4 tr, List<EditMessage> warnings)
    {
        if (h.Geometry == null)
        {
            warnings.Add(EditMessage.Warning($"Hull node {h.Name} contains no geometry"));
            return null;
        }

        var hull = CreateHull(h, tr);
        if (hull.IsError)
        {
            warnings.Add(EditMessage.Warning($"Hull creation for {h.Name} failed: {hull.AllMessages()}"));
        }
        else
        {
            warnings.AddRange(hull.Messages);
            if (hull.Data.FaceCount > 300)
            {
                warnings.Add(
                    EditMessage.Warning($"Hull {h.Name} has > 300 triangles, this can cause issues with Freelancer"));
            }

            return hull.Data;
        }

        return null;
    }

    static Vector3 RandomVector(uint hash, float scale)
    {
        var rng = 1664525 * hash + 1013904223;

        float NextFloat()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            var d = rng * (1.0 / 4294967296.0);
            return (float)(-1.0 + d * 2.0);
        }

        return new Vector3(NextFloat(), NextFloat(), NextFloat()) * scale;
    }


    class SurfacePartContext
    {
        public List<EditMessage> Warnings;
        public SurfacePartContext Parent;
        public Matrix4x4 Transform;

        public HashSet<uint> HpIds = new HashSet<uint>();
        public uint PartHash = unchecked((uint)-2128831035);
        public List<SurfacePoint> Points = new List<SurfacePoint>();
        public List<SurfaceNode> Nodes = new List<SurfaceNode>();

        public SurfacePartContext(List<EditMessage> warnings, Matrix4x4 transform, SurfacePartContext parent)
        {
            this.Warnings = warnings;
            this.Parent = parent;
            this.Transform = transform;
        }

        public void AddChild(HullData h, uint crc, bool hpid, Matrix4x4 matrix)
        {
            var transformed = new Vector3[h.Hull.Vertices.Length];
            for (int i = 0; i < transformed.Length; i++)
                transformed[i] = Vector3.Transform(h.Hull.Vertices[i], matrix);
            var h2 = new HullData()
            {
                Source = h.Source,
                Hull = Hull.FromTriangles(transformed, h.Hull.Indices.ToArray())
            };
            AddHull(h2, crc, 4, hpid);
        }
        public void AddHull(HullData h, uint crc, byte type, bool hpid)
        {
            if (type == 4)
            {
                Parent?.AddChild(h, crc, hpid, Transform);
            }

            if (hpid)
            {
                HpIds.Add(crc);
            }

            Vector3 minimum = new Vector3(float.MaxValue);
            Vector3 maximum = new Vector3(float.MinValue);
            var indices = new int[h.Hull.Vertices.Length];
            Span<Vector3> hashVec = stackalloc Vector3[1];
            Span<byte> hashBytes = MemoryMarshal.Cast<Vector3, byte>(hashVec);
            for (int i = 0; i < h.Hull.Vertices.Length; i++)
            {
                var p = h.Hull.Vertices[i];
                Points.AddIfUnique(new SurfacePoint(p, crc), out indices[i]);
                minimum = Vector3.Min(p, minimum);
                maximum = Vector3.Max(p, maximum);
                //hash
                unchecked
                {
                    hashVec[0] = p;
                    for (int j = 0; j < hashBytes.Length; j++)
                    {
                        PartHash = (PartHash ^ hashBytes[j]) * 16777619;
                    }
                }
            }

            var n = new SurfaceNode();
            var hull = ToSurfaceHull(h, crc, type, indices);
            n.Hull = hull.Data;
            Warnings.AddRange(hull.Messages);
            n.SetBoundary(minimum, maximum);
            Nodes.Add(n);
        }
    }

    enum NodeKind
    {
        Node3db,
        NodeRoot,
        NodeNormal
    }

    static void CreateSurfacePart(ImportedModelNode node, List<SurfacePart> parts,
        SurfacePartContext parent, NodeKind nodeKind, List<EditMessage> warnings)
    {
        var modelCrc = nodeKind switch
        {
            NodeKind.Node3db => 0U,
            NodeKind.NodeRoot => CrcTool.FLModelCrc("Root"),
            _ => CrcTool.FLModelCrc(node.Name),
        };
        var convexHulls = node.Hulls.Select(x => NodeToHull(x, Matrix4x4.Identity, warnings)).Where(x => x != null).ToList();
        if (convexHulls.Count == 0)
        {
            warnings.Add(EditMessage.Warning($"Node {node.Name} has no valid collision hulls"));
        }

        var ctx = new SurfacePartContext(warnings, node.Transform.Matrix(), parent);

        foreach (var h in convexHulls)
        {
            ctx.AddHull(h, modelCrc, 4, false);
        }

        foreach (var hp in node.Hardpoints)
        {
            if (hp.Hulls.Count == 0)
                continue;
            var hullDatas = hp.Hulls.Select(x => NodeToHull(x, hp.Hardpoint.Transform.Matrix(), warnings))
                .Where(x => x != null).ToArray();
            if (hullDatas.Length == 0)
            {
                warnings.Add(EditMessage.Warning($"Node {hp.Hardpoint.Name} has no valid collision hulls"));
                continue;
            }

            var hpid = CrcTool.FLModelCrc(hp.Hardpoint.Name);

            foreach (var h in hullDatas)
            {
                ctx.AddHull(h, hpid, 4, true);
            }
        }

        var part = new SurfacePart();
        parts.Add(part);

        // Pull child hulls into closest dynamic part
        foreach (var child in node.Children)
        {
            CreateSurfacePart(child, parts, child.Construct is FixConstruct ? ctx : null, NodeKind.NodeNormal, warnings);
        }

        // Pull hpid info out after children
        part.HardpointIds = ctx.HpIds.Order().ToList(); //Ordered for reproducibility

        part.Crc = modelCrc;
        part.Points = ctx.Points;
        part.Dynamic = node.Construct is FixConstruct;

        //  Condense node list by grouping nodes into pairs until only one node remain which becomes root
        while (ctx.Nodes.Count > 1)
        {
            var unsorted = new List<SurfaceNode>();
            var lengths = new List<float>();
            var pairs = new List<(SurfaceNode left, SurfaceNode right)>();
            foreach (var leftNode in ctx.Nodes.Where(unsorted.AddIfUnique))
            {
                foreach (var rightNode in unsorted.Where(x => x != leftNode))
                {
                    pairs.Add((leftNode, rightNode));
                    lengths.Add(Vector3.Distance(leftNode.Center, rightNode.Center));
                }
            }

            // Group list into pairs until one or none are left
            ctx.Nodes = new List<SurfaceNode>();
            while (unsorted.Count > 1)
            {
                // Get pair index of shortest length
                int index = -1;
                float min = float.MaxValue;
                for (int i = 0; i < lengths.Count; i++)
                {
                    if (float.IsNaN(lengths[i]))
                        continue;
                    if (lengths[i] < min)
                    {
                        min = lengths[i];
                        index = i;
                    }
                }
                Debug.Assert(index != -1);

                // Get and remove pair from unsorted list
                var (leftNode, rightNode) = pairs[index];
                unsorted.Remove(leftNode);
                unsorted.Remove(rightNode);

                // Create new node by grouping selected pair
                ctx.Nodes.Add(SurfaceNode.GroupNodes(leftNode, rightNode));

                //  Remove left and right nodes from pairs and lengths
                for (int i = lengths.Count - 1; i >= 0; i--)
                {
                    if (pairs[i].left == leftNode ||
                        pairs[i].right == rightNode ||
                        pairs[i].left == rightNode ||
                        pairs[i].right == leftNode)
                    {
                        // Replace RemoveAt with marking values invalid
                        // the copy cost of RemoveAt can be pathological here
                        lengths[i] = float.NaN;
                        pairs[i] = default;
                    }
                }

                // Should one remain add it to next round
                if (unsorted.Count == 1) ctx.Nodes.Add(unsorted[0]);
            }
        }

        // Set resulting node to root
        if (ctx.Nodes.Count == 1)
        {
            part.Root = ctx.Nodes[0];

            // Generate wrap from points if root has no hull
            if (part.Root.Hull == null)
            {
                var h = CreateHullFromPositions(ctx.Points.Select(x => x.Point));
                warnings.AddRange(h.Messages.Select(x => x.Message).Select(EditMessage.Warning));
                if (h.IsError)
                {
                    parts.Remove(part);
                    warnings.Add(EditMessage.Warning("Could not generate wrap hull for " + node.Name));
                    return;
                }

                var indices = new int[h.Data.Hull.Vertices.Length];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = ctx.Points.FindIndex(x => x.Point == h.Data.Hull.Vertices[i]);
                var hull = ToSurfaceHull(h.Data, modelCrc, 5, indices);
                warnings.AddRange(hull.Messages.Select(x => x.Message).Select(EditMessage.Warning));
                if (hull.IsError)
                {
                    parts.Remove(part);
                    warnings.Add(EditMessage.Warning("Could not generate wrap hull for " + node.Name));
                    return;
                }

                part.Root.Hull = hull.Data;
            }

            part.Minimum = new Vector3(float.MaxValue);
            part.Maximum = new Vector3(float.MinValue);
            part.Center = Vector3.Zero;

            float minRadius = 0;
            float maxRadius = 0;

            foreach (var p in ctx.Points)
            {
                var radius = Vector3.Distance(part.Center, p.Point);
                maxRadius = Math.Max(radius, maxRadius);
                if (ctx.HpIds.Contains(p.Mesh))
                {
                    minRadius = Math.Max(minRadius, radius);
                }
                else
                {
                    part.Minimum = Vector3.Min(part.Minimum, p.Point);
                    part.Maximum = Vector3.Max(part.Maximum, p.Point);
                }
            }

            if (minRadius == 0)
                minRadius = maxRadius;
            part.Radius = maxRadius;
            // Seems to generate objects that consistently work in vanilla FL
            // Random component added in

            var d = 0.2f * maxRadius * maxRadius;
            part.Inertia = new Vector3(d) + RandomVector(ctx.PartHash, d * 0.001f);

            part.Scale = minRadius / maxRadius;
        }
        else
        {
            parts.Remove(part);
            if (convexHulls.Count != 0)
                warnings.Add(EditMessage.Warning("Could not generate BSP tree for " + node.Name));
        }
    }

    static EditResult<HullData> CreateHullFromPositions(IEnumerable<Vector3> positions)
    {
        var pos = new List<Vector3>();
        foreach (var p in positions)
            pos.AddIfUnique(p);
        return HullData.Calculate(pos);
    }


    static EditResult<HullData> CreateHull(ModelNode h, Matrix4x4 parentTransform)
    {
        var verts = new List<Vector3>();
        var indices = new List<int>();
        var tr = h.Transform * parentTransform;
        foreach (var i in h.Geometry.Indices.Indices16)
        {
            verts.AddIfUnique(Vector3.Transform(h.Geometry.Vertices[i].Position, tr), out int index);
            indices.Add(index);
        }

        var inputHull = new HullData()
        {
            Hull = Hull.FromTriangles(verts.ToArray(), indices.ToArray()),
            Source = h.Name,
        };
        return QuickhullAndVerify(inputHull);
    }


    static EditResult<HullData> QuickhullAndVerify(HullData h)
    {
        EditMessage warning = h.Hull.Kind switch
        {
            HullKind.NonWatertight =>  EditMessage.Warning($"{h.Source} is not convex (not water-tight), fixing."),
            HullKind.Multibody => EditMessage.Warning($"{h.Source} is not convex (not all triangles connected), creating convex hull."),
            HullKind.Degenerate => EditMessage.Warning($"{h.Source} is not convex (degenerate triangles)"),
            HullKind.Concave => EditMessage.Warning($"{h.Source} may not be convex, fixing."),
            _ => null
        };
        if (!h.Hull.MakeConvex(true))
        {
            var volume = h.CalculateVolume();
            if (volume < 0.0001f)
                return EditResult<HullData>.Error($"Degenerate mesh, no volume.");
            else
                EditResult<HullData>.Error($"Degenerate mesh.");
        }

        if (h.Hull.Vertices.Length > 65535 || h.Hull.Indices.Length > 65535)
        {
            return EditResult<HullData>.Error($"Hull mesh for {h.Source} is too complex");
        }

        return warning == null ? h.AsResult() : new EditResult<HullData>(h, [warning]);
    }


    static EditResult<SurfaceHull> ToSurfaceHull(HullData hullData, uint crc, byte type, int[] indices)
    {
        var surf = new SurfaceHull();
        surf.HullId = crc;
        surf.Unknown = 0;
        surf.Type = type;
        surf.Faces = new List<SurfaceFace>();

        var edges = new List<Point>();
        for (int i = 0; i < hullData.FaceCount; i++)
        {
            var f = hullData.Hull.GetFace(i);
            edges.Add(new Point(f.A, f.B));
            edges.Add(new Point(f.B, f.C));
            edges.Add(new Point(f.C, f.A));
        }

        // Collect indices of reverse edge pairs (B-A, C-B, A-C)
        var reversed = edges.Select(
            x => edges.IndexOf(new Point(x.Y, x.X))
        ).ToArray();

        int edgeCount = 0;

        const float RayEpsilon = 1E-6f;
        for (int i = 0; i < hullData.FaceCount; i++)
        {
            var normal = hullData.Hull.FaceNormal(i);
            var hit = hullData.Raycast(new Ray(hullData.Hull.FaceCenter(i) - normal * RayEpsilon, -normal));
            Debug.Assert(hit != i);
            var face = new SurfaceFace();
            face.Index = surf.Faces.Count;
            face.Opposite = hit == -1 ? 1 : hit;
            if (hit == -1)
            {
                FLLog.Info("Sur", $"{hullData.Source}: Face {i} no opposite hit");
            }

            face.Flag = type == 5;
            face.Flags = type == 5
                ? new Sur.Point3<bool>(true, true, true)
                : new Sur.Point3<bool>(false, false, false);
            face.Points.A = (ushort)indices[edges[edgeCount].X];
            face.Shared.A = reversed[edgeCount];
            edgeCount++;

            face.Points.B = (ushort)indices[edges[edgeCount].X];
            face.Shared.B = reversed[edgeCount];
            edgeCount++;

            face.Points.C = (ushort)indices[edges[edgeCount].X];
            face.Shared.C = reversed[edgeCount];
            edgeCount++;
            surf.Faces.Add(face);
        }

        return new EditResult<SurfaceHull>(surf);
    }
}
