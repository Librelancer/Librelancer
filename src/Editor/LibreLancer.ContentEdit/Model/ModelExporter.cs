using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.ContentEdit.Texture;
using LibreLancer.Physics;
using LibreLancer.Sur;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;
using Microsoft.EntityFrameworkCore.Query.Internal;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class ModelExporter
{
    public static EditResult<SimpleMesh.Model> Export(CmpFile cmp, SurFile sur, ModelExporterSettings settings, ResourceManager resources)
    {
        //Build tree
        ExportModelNode rootModel = null;
        var parentModels = new List<ExportModelNode>();
        foreach (var p in cmp.Parts)
            if (p.Construct == null)
            {
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
            if (infiniteDetect > 100000) return EditResult<SimpleMesh.Model>.Error("Infinite cmp loop detected");
        }
        //Export model
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};
        var processed = ProcessNode(rootModel, output, settings, resources, sur, false);
        if (processed.IsError)
            return new EditResult<SimpleMesh.Model>(null, processed.Messages);
        output.Roots = new[] { processed.Data };
        if (cmp.Animation != null && settings.IncludeAnimations)
        {
            var animations = new List<Animation>();
            if (cmp.Animation.Scripts.Count > 0) {
                animations.Add(AnimationConversion.DefaultAnimation(cmp));
            }
            foreach (var anm in cmp.Animation.Scripts.Values)
            {
                animations.Add(AnimationConversion.ExportAnimation(cmp, anm));
            }
            output.Animations = animations.ToArray();
        }
        if(settings.IncludeTextures)
            output.Images = ExportImages(resources, output.Materials);
        return output.AsResult();
    }

    static Dictionary<string, ImageData> ExportImages(ResourceManager resources, Dictionary<string, Material> materials)
    {
        HashSet<string> attempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, ImageData>();
        foreach (var m in materials.Values)
        {
            if(string.IsNullOrWhiteSpace(m.DiffuseTexture) || attempted.Contains(m.DiffuseTexture))
                continue;
            var img = resources.FindImage(m.DiffuseTexture);
            if (img != null)
            {
                var exported = TextureExporter.ExportTexture(img, true);
                if (exported != null)
                {
                    result[m.DiffuseTexture] = new ImageData(m.DiffuseTexture, exported, "image/png");
                }
            }
            attempted.Add(m.DiffuseTexture);
        }
        return result;
    }

    public static EditResult<SimpleMesh.Model> Export(ModelFile mdl, SurFile sur, ModelExporterSettings settings, ResourceManager resources)
    {
        var exportNode = new ExportModelNode()
        {
            Name = "Root",
            Model = mdl,
            Construct = null,
        };
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};
        var processed = ProcessNode(exportNode, output, settings, resources, sur, true);
        if (processed.IsError)
            return new EditResult<SimpleMesh.Model>(null, processed.Messages);
        output.Roots = new[] { processed.Data };
        if(settings.IncludeTextures)
            output.Images = ExportImages(resources, output.Materials);
        return output.AsResult();
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

    static EditResult<ModelNode> ProcessNode(ExportModelNode node, SimpleMesh.Model dest, ModelExporterSettings settings, ResourceManager res, SurFile sur, bool is3db)
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
        var l0 = GeometryFromRef(node.Name, 0, node.Model.Levels[0], dest.Materials, res);
        if (l0.IsError)
            return EditResult<ModelNode>.Error($"Unable to export node {node.Name} (Level 0)", l0.Messages);
        sm.Geometry = l0.Data;
        if (settings.IncludeLods)
        {
            for (int i = 1; i < node.Model.Levels.Length; i++)
            {
                var lod = new ModelNode() {Name = node.Name + "$lod" + i};
                var lodRes = GeometryFromRef(node.Name, i, node.Model.Levels[i], dest.Materials, res);
                if (lodRes.IsError)
                    return EditResult<ModelNode>.Error($"Unable to export node {node.Name} (Level {i})",
                        lodRes.Messages);
                lod.Geometry = lodRes.Data;
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

        if (node.Model.VMeshWire != null && settings.IncludeWireframes)
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
            var child = ProcessNode(n, dest, settings, res, sur, is3db);
            if (child.IsError)
                return child;
            sm.Children.Add(child.Data);
        }
        return sm.AsResult();
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
        string dt;
        if ((mat = resources.FindMaterial(crc)) != null)
        {
            name = mat.Name;
            dc = mat.Dc;
            dt = mat.DtName;
        }
        else
        {
            name = $"material_0x{crc:X8}";
            dc = Color4.White;
            dt = null;
        }
        if (!materials.TryGetValue(name, out var m))
        {
            m = new Material() {Name = name, DiffuseColor = dc, DiffuseTexture = dt};
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

    static Vector3 GetPosition(VMeshData vms, int index)
    {
        switch (vms.FlexibleVertexFormat)
        {
            case D3DFVF.XYZ:
                return vms.verticesVertexPosition[index].Position;
            case D3DFVF.XYZ | D3DFVF.NORMAL:
                return vms.verticesVertexPositionNormal[index].Position;
            case D3DFVF.XYZ | D3DFVF.TEX1:
                return vms.verticesVertexPositionTexture[index].Position;
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX1:
                return vms.verticesVertexPositionNormalDiffuseTexture[index].Position;
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX1:
                return vms.verticesVertexPositionNormalTexture[index].Position;
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.TEX2:
                return vms.verticesVertexPositionNormalTextureTwo[index].Position;
            case D3DFVF.XYZ | D3DFVF.NORMAL | D3DFVF.DIFFUSE | D3DFVF.TEX2:
                return vms.verticesVertexPositionNormalDiffuseTextureTwo[index].Position;
            default:
                throw new Exception($"D3DFVF {vms.FlexibleVertexFormat} not supported");
        }
    }

    static Geometry GeometryFromVMeshWire(string name, VMeshWire wire, ResourceManager resources,
        Dictionary<string, Material> materials)
    {
        var geo = new Geometry();
        geo.Name = name + "." + ".wire.mesh";
        geo.Attributes = VertexAttributes.Position;
        var mesh = resources.FindMeshData(wire.MeshCRC);
        List<Vertex> verts = new List<Vertex>();
        List<int> hashes = new List<int>();
        List<uint> indices = new List<uint>();
        for (int i = 0; i < wire.NumIndices; i++)
        {
            var idx = wire.VertexOffset + wire.Indices[i];
            var vert = new Vertex() {Position = GetPosition(mesh, idx)};
            var hash = HashVert(ref vert);
            int newIndex = FindDuplicate(hashes, verts, 0, ref vert, hash);
            if (newIndex == -1)
            {
                newIndex = verts.Count;
                verts.Add(vert);
                hashes.Add(hash);
            }

            indices.Add((uint) newIndex);
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
    static EditResult<Geometry> GeometryFromRef(string name, int level, VMeshRef vms, Dictionary<string,Material> materials, ResourceManager resources)
    {
        var geo = new Geometry();
        var mesh = resources.FindMeshData(vms.MeshCrc);
        if (vms.MeshCrc == 0)
            return new EditResult<Geometry>(null);
        if ((mesh == null))
            return EditResult<Geometry>.Error($"{name} - VMeshData lookup failed 0x{vms.MeshCrc}");
        geo.Name = name + "." + (int) mesh.OriginalFVF + ".level" + level;
        List<Vertex> verts = new List<Vertex>();
        List<int> hashes = new List<int>();
        List<uint> indices = new List<uint>();
        List<TriangleGroup> groups = new List<TriangleGroup>();
        for (int meshi = vms.StartMesh; meshi < vms.StartMesh + vms.MeshCount; meshi++)
        {
            var m = mesh.Meshes[meshi];
            var dc = new TriangleGroup();
            dc.StartIndex = indices.Count;
            dc.IndexCount = m.NumRefVertices;
            dc.Material = GetMaterial(m.MaterialCrc, resources, materials);
            for (int i = m.TriangleStart; i < m.TriangleStart + m.NumRefVertices; i++)
            {
                int idx = mesh.Indices[i] + m.StartVertex + vms.StartVertex;
                Vertex vert;
                if (mesh.verticesVertexPosition != null)
                    vert = new Vertex()
                        {Position = mesh.verticesVertexPosition[idx].Position};
                else if (mesh.verticesVertexPositionNormal != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionNormal[idx].Position,
                        Normal = mesh.verticesVertexPositionNormal[idx].Normal
                    };
                }
                else if (mesh.verticesVertexPositionTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionTexture[idx].Position,
                        Texture1 = mesh.verticesVertexPositionTexture[idx].TextureCoordinate
                    };
                }
                else if (mesh.verticesVertexPositionNormalTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionNormalTexture[idx].Position,
                        Normal = mesh.verticesVertexPositionNormalTexture[idx].Normal,
                        Texture1 = mesh.verticesVertexPositionNormalTexture[idx].TextureCoordinate
                    };
                }
                else if (mesh.verticesVertexPositionNormalTextureTwo != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Texture1 |
                                   VertexAttributes.Texture2;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionNormalTextureTwo[idx].Position,
                        Normal = mesh.verticesVertexPositionNormalTextureTwo[idx].Normal,
                        Texture1 = mesh.verticesVertexPositionNormalTextureTwo[idx].TextureCoordinate,
                        Texture2 = mesh.verticesVertexPositionNormalTextureTwo[idx]
                            .TextureCoordinateTwo
                    };
                }
                else if (mesh.verticesVertexPositionNormalDiffuseTexture != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Diffuse |
                                   VertexAttributes.Texture1;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionNormalDiffuseTexture[idx].Position,
                        Normal = mesh.verticesVertexPositionNormalDiffuseTexture[idx].Normal,
                        Diffuse = (Color4)mesh.verticesVertexPositionNormalDiffuseTexture[idx].Diffuse,
                        Texture1 = mesh.verticesVertexPositionNormalDiffuseTexture[idx]
                            .TextureCoordinate
                    };
                }
                else if (mesh.verticesVertexPositionNormalDiffuseTextureTwo != null)
                {
                    geo.Attributes = VertexAttributes.Position | VertexAttributes.Normal | VertexAttributes.Diffuse |
                                   VertexAttributes.Texture1 | VertexAttributes.Texture2;
                    vert = new Vertex()
                    {
                        Position = mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Position,
                        Normal = mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Normal,
                        Diffuse = (Color4)mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].Diffuse,
                        Texture1 = mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].TextureCoordinate,
                        Texture2 = mesh.verticesVertexPositionNormalDiffuseTextureTwo[idx].TextureCoordinateTwo,
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
        return geo.AsResult();
    }

    private class ExportModelNode
    {
        public string Name;
        public List<ExportModelNode> Children = new();
        public ModelFile Model;
        public AbstractConstruct Construct;
    }
}
