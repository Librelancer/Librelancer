using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Physics;
using LibreLancer.Sur;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using Microsoft.EntityFrameworkCore.Query.Internal;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class ModelExporter
{
    public static SimpleMesh.Model Export(CmpFile cmp, SurFile sur, ModelExporterSettings settings, ResourceManager resources)
    {
        //Build tree
        ExportModelNode rootModel = null;
        var parentModels = new List<ExportModelNode>();
        foreach (var p in cmp.Parts)
            if (p.Construct == null)
            {
                p.Model?.VMeshWire.Initialize(cmp);
                rootModel = new ExportModelNode
                {
                    Model = p.Model,
                    Name = p.ObjectName,
                };
                break;
            }

        parentModels.Add(rootModel);
        var q = new Queue<Part>(cmp.Parts);
        var infiniteDetect = 0;
        while (q.Count > 0)
        {
            var part = q.Dequeue();
            if (part.Construct == null) continue;
            var enqueue = true;
            foreach (var mdl in parentModels)
                if (part.Construct.ParentName == mdl.Name)
                {
                    part.Model.VMeshWire?.Initialize(cmp);
                    var child = new ExportModelNode
                    {
                        Construct = part.Construct,
                        Model = part.Model,
                        Name = part.ObjectName,
                    };
                    mdl.Children.Add(child);
                    parentModels.Add(child);
                    enqueue = false;
                    break;
                }

            if (enqueue)
                q.Enqueue(part);
            infiniteDetect++;
            if (infiniteDetect > 100000) throw new Exception("Infinite cmp loop detected");
        }
        //Export model
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};
        output.Roots = new[] {ProcessNode(rootModel, output, settings, resources, sur, false)};
        return output;
    }
    
    public static SimpleMesh.Model Export(ModelFile mdl, SurFile sur, ModelExporterSettings settings, ResourceManager resources)
    {
        mdl.VMeshWire?.Initialize(mdl);
        var exportNode = new ExportModelNode()
        {
            Name = "Root",
            Model = mdl,
            Construct = null,
        };
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};
        output.Roots = new[] {ProcessNode(exportNode, output, settings, resources, sur, true)};
        return output;
    }

    static SimpleMesh.ModelNode FromHardpoint(HardpointDefinition def)
    {
        var n = new ModelNode();
        n.Name = def.Name;
        n.Transform = def.Transform;
        n.Properties["hardpoint"] = true;
        if (def is FixedHardpointDefinition)
        {
            n.Properties["hptype"] = "fix";
        }
        else if (def is RevoluteHardpointDefinition rev)
        {
            n.Properties["hptype"] = "rev";
            n.Properties["min"] = MathHelper.RadiansToDegrees(rev.Min);
            n.Properties["max"] = MathHelper.RadiansToDegrees(rev.Max);
            n.Properties["axis"] = rev.Axis;
        }
        return n;
    }

    static ModelNode ProcessNode(ExportModelNode node, SimpleMesh.Model dest, ModelExporterSettings settings, ResourceManager res, SurFile sur, bool is3db)
    {
        var sm = new ModelNode();
        sm.Name = node.Name;
        if (node.Construct != null)
        {
            sm.Transform = node.Construct.Rotation * Matrix4x4.CreateTranslation(node.Construct.Origin);
        }
        if (node.Construct is FixConstruct)
        {
            sm.Properties["construct"] = "fix";
        }
        if (node.Construct is RevConstruct rev)
        {
            sm.Properties["construct"] = "rev";
            sm.Properties["min"] = MathHelper.RadiansToDegrees(rev.Min);
            sm.Properties["max"] = MathHelper.RadiansToDegrees(rev.Max);
            sm.Properties["offset"] = rev.Offset;
            sm.Properties["axis_rotation"] = rev.AxisRotation;
        }
        if (node.Construct is PrisConstruct pris)
        {
            sm.Properties["construct"] = "pris";
            sm.Properties["min"] = pris.Min;
            sm.Properties["max"] = pris.Max;
            sm.Properties["offset"] = pris.Offset;
            sm.Properties["axis_translation"] = pris.AxisTranslation;
        }
        if (node.Construct is SphereConstruct sphere)
        {
            sm.Properties["construct"] = "sphere";
            sm.Properties["min"] = new Vector3(sphere.Min1, sphere.Min2, sphere.Min3);
            sm.Properties["max"] = new Vector3(sphere.Max1, sphere.Max2, sphere.Max3);
            sm.Properties["offset"] = sphere.Offset;
        }
        if (node.Construct is LooseConstruct)
        {
            sm.Properties["construct"] = "loose";
        }
        sm.Geometry = GeometryFromRef(node.Name, 0, node.Model.Levels[0], dest.Materials, res);
        if (settings.IncludeLods)
        {
            for (int i = 1; i < node.Model.Levels.Length; i++)
            {
                var lod = new ModelNode() {Name = node.Name + "$lod" + i};
                lod.Geometry = GeometryFromRef(node.Name, i, node.Model.Levels[i], dest.Materials, res);
                sm.Children.Add(lod);
            }
        }
        if (settings.IncludeHardpoints)
        {
            foreach (var hp in node.Model.Hardpoints)
            {
                sm.Children.Add(FromHardpoint(hp));
            }
        }
        if (settings.IncludeHulls && sur != null)
        {
            var hulls = sur.GetMesh(is3db ? 0 : CrcTool.FLModelCrc(node.Name));
            for (int i = 0; i < hulls.Length; i++)
            {
                var surnode = new ModelNode
                {
                    Geometry = GeometryFromSur(node.Name + "." + i + "$hull", hulls[i], res, dest.Materials),
                    Name = node.Name + "." + i + "$hull",
                    Properties =
                    {
                        ["hull"] = true
                    }
                };
                sm.Children.Add(surnode);
            }
        }

        if (node.Model.VMeshWire != null)
        {
            var meshNode = new ModelNode
            {
                Geometry = GeometryFromVMeshWire(node.Name, node.Model.VMeshWire, res, dest.Materials),
                Name = node.Name + ".vmeshwire"
            };
            sm.Children.Add(meshNode);
        }
        foreach (var n in node.Children)
        {
            sm.Children.Add(ProcessNode(n, dest, settings, res, sur, is3db));
        }
        return sm;
    }

    static int HashVert(ref Vertex vert)
    {
        unchecked
        {
            int hash = (int) 2166136261;
            hash = hash * 16777619 ^ vert.Position.GetHashCode();
            hash = hash * 16777619 ^ vert.Normal.GetHashCode();
            hash = hash * 16777619 ^ vert.Texture1.GetHashCode();
            hash = hash * 16777619 ^ vert.Texture2.GetHashCode();
            hash = hash * 16777619 ^ vert.Diffuse.GetHashCode();
            return hash;
        }
    }

    static int FindDuplicate(List<int> hashes, List<Vertex> buf, int startIndex,
        ref Vertex search, int hash)
    {
        for (int i = startIndex; i < buf.Count; i++)
        {
            if (hashes[i] != hash) continue;
            if (buf[i].Position != search.Position) continue;
            if (buf[i].Normal != search.Normal) continue;
            if (buf[i].Texture1 != search.Texture1) continue;
            if (buf[i].Diffuse != search.Diffuse) continue;
            if (buf[i].Texture2 != search.Texture2) continue;
            return i;
        }

        return -1;
    }

    static Material GetMaterial(uint crc, ResourceManager resources, Dictionary<string, Material> materials)
    {
        LibreLancer.Utf.Mat.Material mat;
        Color4 dc;
        string name;
        if ((mat = resources.FindMaterial(crc)) != null)
        {
            name = mat.Name;
            dc = mat.Dc;
        }
        else
        {
            name = $"material_0x{crc:X8}";
            dc = Color4.White;
        }
        if (!materials.TryGetValue(name, out var m))
        {
            m = new Material() {Name = name, DiffuseColor = dc};
            materials[name] = m;
        }
        return m;
    }

    static Geometry GeometryFromSur(string name, ConvexMesh ms, ResourceManager resources, Dictionary<string, Material> materials)
    {
        var geo = new Geometry();
        geo.Name = name;
        geo.Vertices = ms.Vertices.Select(x => new Vertex() {Position = x}).ToArray();
        geo.Indices = Indices.FromBuffer(ms.Indices.Select(x => (uint)x).ToArray());
        geo.Groups = new TriangleGroup[] {
            new TriangleGroup()
            {
                BaseVertex =  0, 
                StartIndex = 0,
                IndexCount = ms.Indices.Length,
                Material = GetMaterial(0, resources, materials)
            }
        };
        geo.Attributes = VertexAttributes.Position;
        return geo;
    }

    static Geometry GeometryFromVMeshWire(string name, VMeshWire wire, ResourceManager resources,
        Dictionary<string, Material> materials)
    {
        var geo = new Geometry();
        geo.Name = name + "." + ".wire.mesh";
        geo.Attributes = VertexAttributes.Position;
        List<Vertex> verts = new List<Vertex>();
        List<int> hashes = new List<int>();
        List<uint> indices = new List<uint>();
        foreach (var pos in wire.Lines)
        {
            var vert = new Vertex() {Position = pos};
            var hash = HashVert(ref vert);
            int newIndex = FindDuplicate(hashes, verts, 0, ref vert, hash);
            if (newIndex == -1)
            {
                newIndex = verts.Count;
                verts.Add(vert);
                hashes.Add(hash);
            }
            indices.Add((uint)newIndex);
        }
        geo.Vertices = verts.ToArray();
        geo.Indices = Indices.FromBuffer(indices.ToArray());
        geo.Groups = new TriangleGroup[] {
            new TriangleGroup()
            {
                BaseVertex =  0, 
                StartIndex = 0,
                IndexCount = indices.Count,
                Material = GetMaterial(0, resources, materials)
            }
        };
        geo.Kind = GeometryKind.Lines;
        return geo;
    }

    // Get just the referenced geometry from the VMeshData
    static Geometry GeometryFromRef(string name, int level, VMeshRef vms, Dictionary<string,Material> materials, ResourceManager resources)
    {
        var geo = new Geometry();
        geo.Name = name + "." + (int) vms.Mesh.OriginalFVF + ".level" + level;
        List<Vertex> verts = new List<Vertex>();
        List<int> hashes = new List<int>();
        List<uint> indices = new List<uint>();
        List<TriangleGroup> groups = new List<TriangleGroup>();
        for (int meshi = vms.StartMesh; meshi < vms.StartMesh + vms.MeshCount; meshi++)
        {
            var m = vms.Mesh.Meshes[meshi];
            var dc = new TriangleGroup();
            dc.StartIndex = indices.Count;
            dc.IndexCount = m.NumRefVertices;
            dc.Material = GetMaterial(m.MaterialCrc, resources, materials);
            for (int i = m.TriangleStart; i < m.TriangleStart + m.NumRefVertices; i++)
            {
                int idx = vms.Mesh.Indices[i] + m.StartVertex + vms.StartVertex;
                Vertex vert;
                if (vms.Mesh.verticesVertexPosition != null)
                    vert = new Vertex()
                        {Position = vms.Mesh.verticesVertexPosition[idx].Position};
                else if (vms.Mesh.verticesVertexPositionNormal != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionNormal[idx].Position,
                        Normal = vms.Mesh.verticesVertexPositionNormal[idx].Normal
                    };
                }
                else if (vms.Mesh.verticesVertexPositionTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionTexture[idx].Position,
                        Texture1 = vms.Mesh.verticesVertexPositionTexture[idx].TextureCoordinate
                    };
                }
                else if (vms.Mesh.verticesVertexPositionNormalTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionNormalTexture[idx].Position,
                        Normal = vms.Mesh.verticesVertexPositionNormalTexture[idx].Normal,
                        Texture1 = vms.Mesh.verticesVertexPositionNormalTexture[idx].TextureCoordinate
                    };
                }
                else if (vms.Mesh.verticesVertexPositionNormalTextureTwo != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Texture1 |
                                   VertexAttributes.Texture2;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].Position,
                        Normal = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].Normal,
                        Texture1 = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx].TextureCoordinate,
                        Texture2 = vms.Mesh.verticesVertexPositionNormalTextureTwo[idx]
                            .TextureCoordinateTwo
                    };
                }
                else if (vms.Mesh.verticesVertexPositionNormalDiffuseTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Diffuse |
                                   VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Position,
                        Normal = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Normal,
                        Diffuse = Color4.FromRgba(vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx].Diffuse),
                        Texture1 = vms.Mesh.verticesVertexPositionNormalDiffuseTexture[idx]
                            .TextureCoordinate
                    };
                }
                else if (vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Diffuse |
                                   VertexAttributes.Texture1 | VertexAttributes.Texture2;
                    vert = new Vertex()
                    {
                        Position = vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Position,
                        Normal = vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Normal,
                        Diffuse = Color4.FromRgba(vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Diffuse),
                        Texture1 = vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].TextureCoordinate,
                        Texture2 = vms.Mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].TextureCoordinateTwo,
                    };
                }
                else
                    throw new Exception("something in state is real bad"); //Never called
                //Internal stored OpenGL, flip to DX
                vert.Texture1.Y = 1 - vert.Texture1.Y;
                vert.Texture2.Y = 1 - vert.Texture2.Y;
                var hash = HashVert(ref vert);
                int newIndex = FindDuplicate(hashes, verts, 0, ref vert, hash);
                if (newIndex == -1)
                {
                    newIndex = verts.Count;
                    verts.Add(vert);
                    hashes.Add(hash);
                }
                indices.Add((uint)newIndex);
            }
            groups.Add(dc);
        }
        geo.Vertices = verts.ToArray();
        //Reconstruct base vertex
        foreach (var group in groups)
        {
            uint min = uint.MaxValue;
            for (int i = group.StartIndex; i < group.StartIndex + group.IndexCount; i++) {
                if (indices[i] < min)
                    min = indices[i];
            }
            group.BaseVertex = (int)min;
            for (int i = group.StartIndex; i < group.StartIndex + group.IndexCount; i++) {
                indices[i] -= min;
            }
        }
        geo.Indices = Indices.FromBuffer(indices.ToArray());
        geo.Groups = groups.ToArray();
        return geo;
    }

    private class ExportModelNode
    {
        public string Name;
        public List<ExportModelNode> Children = new();
        public ModelFile Model;
        public AbstractConstruct Construct;
    }
}