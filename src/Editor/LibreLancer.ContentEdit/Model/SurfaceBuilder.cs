using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using LibreLancer.Sur;
using LibreLancer.Utf;
using SimpleMesh;

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
        foreach(var c in node.Children)
            if (NodeHasHulls(c))
                return true;
        return false;
    }

    public static EditResult<SurFile> CreateSur(ImportedModel model, bool forceCompound = false)
    {
        List<EditMessage> warnings = new List<EditMessage>();
        var parts = new List<SurfacePart>();
        CreateSurfacePart(model.Root, parts, (h,c,m) => { }, !forceCompound && model.Root.Children.Count == 0, warnings);
        if (parts.Count == 0) {
            return EditResult<SurFile>.Error("No valid hulls", warnings);
        }
        parts[0].Dynamic = true;
        var result = new SurFile() { Surfaces = parts };
        if(!VerifyWriteSur(result))
            warnings.Add(EditMessage.Warning("Verify writing sur failed"));
        return new EditResult<SurFile>(result, warnings);
    }



    static void CreateSurfacePart(ImportedModelNode node, List<SurfacePart> parts, Action<HullData, uint, Matrix4x4> addToParent, bool is3db, List<EditMessage> warnings)
    {
        List<HullData> convexHulls = new List<HullData>();
        var modelCrc = is3db ? 0 : CrcTool.FLModelCrc(node.Name);
        foreach (var h in node.Hulls) {
            if (h.Geometry == null) {
                warnings.Add(EditMessage.Warning($"Hull node {h.Name} contains no geometry"));
                continue;
            }
            var hull = CreateHull(h);
            if (hull.IsError) {
                warnings.Add(EditMessage.Warning($"Hull creation failed: {hull.AllMessages()}"));
            }
            else
            {
                warnings.AddRange(hull.Messages);
                if (hull.Data.FaceCount > 300) {
                    warnings.Add(EditMessage.Warning($"Hull {h.Name} has > 300 triangles, this can cause issues with Freelancer"));
                }
                convexHulls.Add(hull.Data);
            }
        }
        if (convexHulls.Count == 0) {
            warnings.Add(EditMessage.Warning($"Node {node.Name} has no valid collision hulls"));
        }

        List<SurfacePoint> points = new List<SurfacePoint>();
        List<SurfaceNode> nodes = new List<SurfaceNode>();

        void AddHull(HullData h, uint crc, byte type, bool add = true)
        {
            if (type == 4 && node.Construct is FixConstruct) {
                addToParent(h, crc, node.Transform.Matrix());
            }
            Vector3 minimum = new Vector3(float.MaxValue);
            Vector3 maximum = new Vector3(float.MinValue);
            var indices = new int[h.Vertices.Length];
            for (int i = 0; i < h.Vertices.Length; i++)
            {
                var p = h.Vertices[i];
                points.AddIfUnique(new SurfacePoint(p, modelCrc), out indices[i]);
                minimum = Vector3.Min(p, minimum);
                maximum = Vector3.Max(p, maximum);
            }
            var n = new SurfaceNode();
            var hull = ToSurfaceHull(h, crc, type, indices);
            n.Hull = hull.Data;
            warnings.AddRange(hull.Messages);
            n.SetBoundary(minimum, maximum);
            nodes.Add(n);
        }

        foreach (var h in convexHulls)
        {
           AddHull(h, modelCrc, 4);
        }

        var part = new SurfacePart();
        parts.Add(part);

        // Pull child hulls into closest dynamic part
        foreach (var child in node.Children)
        {

            CreateSurfacePart(child, parts, (h,c, m) =>
            {
                if(node.Construct is FixConstruct)
                    addToParent(h, c, m * node.Transform.Matrix());
                else
                {
                    var h2 = new HullData() {Source = h.Source, Indices = h.Indices};
                    h2.Vertices = h.Vertices.Select(x => Vector3.Transform(x, m)).ToArray();
                    AddHull(h2, c, 4);
                }
            }, false, warnings);
        }

        part.Crc = modelCrc;
        part.Points = points;
        part.Dynamic = node.Construct is FixConstruct;

        //  Condense node list by grouping nodes into pairs until only one node remain which becomes root
        while (nodes.Count > 1) {
            var unsorted = new List<SurfaceNode>();
            var lengths = new List<float>();
            var pairs = new List<(SurfaceNode left, SurfaceNode right)>();
            foreach (var leftNode in nodes.Where(unsorted.AddIfUnique)) {
                foreach (var rightNode in nodes.Where(x => x != leftNode)) {
                    pairs.Add((leftNode, rightNode));
                    lengths.Add(Vector3.Distance(leftNode.Center, rightNode.Center));
                }
            }

            // Group list into pairs until one or none are left
            nodes = new List<SurfaceNode>();
            while (unsorted.Count > 1)
            {
                var index = lengths.IndexOfMin(); // Get pair index of shortest length

                // Get and remove pair from unsorted list
                var (leftNode, rightNode) = pairs[index];
                unsorted.Remove(leftNode);
                unsorted.Remove(rightNode);

                // Create new node by grouping selected pair
                nodes.Add(SurfaceNode.GroupNodes(leftNode, rightNode));

                //  Remove left and right nodes from pairs and lengths
                for (int i = lengths.Count - 1; i >= 0; i--) {
                    if (pairs[i].left == leftNode ||
                        pairs[i].right == rightNode ||
                        pairs[i].left == rightNode ||
                        pairs[i].right == leftNode)
                    {
                        lengths.RemoveAt(i);
                        pairs.RemoveAt(i);
                    }
                }

                // Should one remain add it to next round
                if(unsorted.Count == 1) nodes.Add(unsorted[0]);
            }
        }

        // Set resulting node to root
        if (nodes.Count == 1)
        {
            part.Root = nodes[0];

            // Generate wrap from points if root has no hull
            if (part.Root.Hull == null)
            {
                var h = CreateHullFromPositions(points.Select(x => x.Point));
                warnings.AddRange(h.Messages.Select(x => x.Message).Select(EditMessage.Warning));
                if (h.IsError) {
                    parts.Remove(part);
                    warnings.Add(EditMessage.Warning("Could not generate wrap hull for " + node.Name));
                }
                var indices = new int[points.Count];
                for (int i = 0; i < indices.Length; i++)
                    indices[i] = i;
                var hull = ToSurfaceHull(h.Data, modelCrc, 5, indices);
                warnings.AddRange(hull.Messages.Select(x => x.Message).Select(EditMessage.Warning));
                if (hull.IsError) {
                    parts.Remove(part);
                    warnings.Add(EditMessage.Warning("Could not generate wrap hull for " + node.Name));
                }
                part.Root.Hull = hull.Data;
            }

            part.Minimum = new Vector3(float.MaxValue);
            part.Maximum = new Vector3(float.MinValue);
            part.Center = Vector3.Zero;

            float minRadius = 0;
            float maxRadius = 0;

            foreach (var p in points)
            {
                part.Minimum = Vector3.Min(part.Minimum, p.Point);
                part.Maximum = Vector3.Max(part.Maximum, p.Point);
                var radius = Vector3.Distance(part.Center, p.Point);
                maxRadius = Math.Max(radius, maxRadius);
            }

            if (minRadius == 0)
                minRadius = maxRadius;
            part.Radius = maxRadius;
            // Seems to generate objects that consistently work in vanilla FL
            part.Inertia = new Vector3(0.2f * maxRadius * maxRadius);
            part.Scale = minRadius / maxRadius;
        }
        else
        {
            parts.Remove(part);
            if(convexHulls.Count != 0)
                warnings.Add(EditMessage.Warning("Could not generate BSP tree for " + node.Name));
        }
    }

    static EditResult<HullData> CreateHullFromPositions(IEnumerable<Vector3> positions)
    {
        var pos = new List<Vector3>();
        foreach (var p in positions)
            pos.AddIfUnique(p);
        return EditResult<HullData>.TryCatch(() => new ConvexHullCalculator().GenerateHull(pos, false));
    }



    static EditResult<HullData> CreateHull(ModelNode h)
    {
        if (h.Geometry.Indices.Indices32 != null)
            return EditResult<HullData>.Error($"Mesh {h.Name} is too complex");

        var verts = new List<Vector3>();
        var indices = new List<ushort>();
        foreach (var i in h.Geometry.Indices.Indices16)
        {
            verts.AddIfUnique(h.Geometry.Vertices[i].Position, out int index);
            indices.Add((ushort) index);
        }
        var inputHull = new HullData()
        {
            Vertices = verts.ToArray(),
            Indices = indices.ToArray(),
            Source = h.Name,
        };
        return QuickhullAndVerify(inputHull, h.Name);
    }


    static EditResult<HullData> QuickhullAndVerify(HullData h, string name)
    {
        var fromPos = CreateHullFromPositions(h.Vertices);
        if (fromPos.IsError) return EditResult<HullData>.Error($"Creating convex hull for {name} failed");
        return Math.Abs(fromPos.Data.GetVolume() - h.GetVolume()) > 2.0f
            ? new EditResult<HullData>(fromPos.Data, new[] {EditMessage.Warning($"Source hull {name} may not be convex")})
            : fromPos;
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
            var f = hullData.GetFace(i);
            edges.Add(new Point(f.A, f.B));
            edges.Add(new Point(f.B, f.C));
            edges.Add(new Point(f.C, f.A));
        }
        // Collect indices of reverse edge pairs (B-A, C-B, A-C)
        var reversed = edges.Select(
            x => edges.IndexOf(new Point(x.Y, x.X))
        ).ToArray();

        int edgeCount = 0;

        const float RayEpsilon = 1E-7f;
        int missingHit = 0;
        for (int i = 0; i < hullData.FaceCount; i++)
        {
            var normal = hullData.GetFaceNormal(i);
            var hit = hullData.Raycast(new Ray(hullData.GetFaceCenter(i) - normal * RayEpsilon, -normal));
            Debug.Assert(hit != i);
            var face = new SurfaceFace();
            face.Index = surf.Faces.Count;
            face.Opposite = hit == -1 ? 1 : hit;
            if (hit == -1)
            {
                missingHit++;
            }
            face.Flag = type == 5;
            face.Flags = type == 5
                ? new Point3<bool>(true, true, true)
                : new Point3<bool>(false, false, false);
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

        if (missingHit > 0)
            return new EditResult<SurfaceHull>(surf, new[]
            {
                EditMessage.Warning($"{hullData.Source}: {missingHit}/{hullData.FaceCount} faces could not calculate opposite")
            });
        else
            return new EditResult<SurfaceHull>(surf);
    }
}
