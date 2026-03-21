using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.ContentEdit.Texture;
using LibreLancer.Data;
using LibreLancer.Physics;
using LibreLancer.Resources;
using LibreLancer.Sur;
using LibreLancer.Utf;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
using SimpleMesh;
using Material = SimpleMesh.Material;

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
        var ag = new List<Geometry>();
        var processed = ProcessNode(rootModel, output, settings, resources, sur, ag, false);
        if (processed.IsError)
            return new EditResult<SimpleMesh.Model>(null, processed.Messages);
        output.Geometries = ag.ToArray();
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

    static ImageData ExportSingleImage(string tex, HashSet<string> attempted, ResourceManager resources)
    {
        if (string.IsNullOrWhiteSpace(tex) || attempted.Contains(tex))
            return null;
        var img = resources.FindImage(tex);
        if (img != null)
        {
            var exported = TextureExporter.ExportTexture(img, true);
            if (exported != null)
            {
                return new ImageData(tex, exported, "image/png");
            }
        }
        attempted.Add(tex);
        return null;
    }
    static Dictionary<string, ImageData> ExportImages(ResourceManager resources, Dictionary<string, Material> materials)
    {
        HashSet<string> attempted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, ImageData>();
        foreach (var m in materials.Values)
        {
            var dt = ExportSingleImage(m.DiffuseTexture?.Name, attempted, resources);
            if (dt != null)
                result[dt.Name] = dt;
            var et = ExportSingleImage(m.EmissiveTexture?.Name, attempted, resources);
            if (et != null)
                result[et.Name] = et;
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
        var ag = new List<Geometry>();
        var output = new SimpleMesh.Model() {Materials = new Dictionary<string, Material>()};
        var processed = ProcessNode(exportNode, output, settings, resources, sur, ag, true);
        if (processed.IsError)
            return new EditResult<SimpleMesh.Model>(null, processed.Messages);
        output.Geometries = ag.ToArray();
        output.Roots = new[] { processed.Data };
        if(settings.IncludeTextures)
            output.Images = ExportImages(resources, output.Materials);
        return output.AsResult();
    }

    static SimpleMesh.ModelNode FromHardpoint(
        HardpointDefinition def,
        uint parentId,
        ResourceManager resources,
        Dictionary<string, Material> materials,
        List<Geometry> geometries,
        SurFile? sur)
    {
        var n = new ModelNode();
        n.Name = def.Name;
        n.Transform = def.Transform.Matrix();
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

        if (sur != null &&
            sur.TryGetHardpoint(parentId, CrcTool.FLModelCrc(def.Name), out var hulls))
        {
            n.Children = new List<ModelNode>();
            for (int i = 0; i < hulls.Length; i++)
            {
                var h = hulls[i];
                var geo = GeometryFromSur($"{def.Name}.{i}$hull", h, resources, materials, geometries);
                Matrix4x4.Invert(n.Transform, out var inverse);
                for (int j = 0; j < geo.Vertices.Count; j++)
                {
                    geo.Vertices.Position[j] = Vector3.Transform(geo.Vertices.Position[j], inverse);
                }
                var hn = new ModelNode();
                hn.Geometry = geo;
                hn.Name = $"{def.Name}.{i}$hull";
                hn.Transform = Matrix4x4.Identity;
                hn.Properties["hull"] = true;
                n.Children.Add(hn);
            }
        }
        return n;
    }

    static EditResult<ModelNode> ProcessNode(ExportModelNode node, SimpleMesh.Model dest, ModelExporterSettings settings, ResourceManager res, SurFile sur, List<Geometry> allGeos, bool is3db)
    {
        var sm = new ModelNode();
        sm.Name = node.Name;
        if (node.Construct != null)
        {
            sm.Transform = new Transform3D(node.Construct.Origin, node.Construct.Rotation).Matrix();
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
        var l0 = GeometryFromRef(node.Name, 0, node.Model.Levels[0], dest.Materials, allGeos, res);
        if (l0.IsError)
            return EditResult<ModelNode>.Error($"Unable to export node {node.Name} (Level 0)", l0.Messages);
        sm.Geometry = l0.Data;
        if (settings.IncludeLods)
        {
            for (int i = 1; i < node.Model.Levels.Length; i++)
            {
                var lod = new ModelNode() {Name = node.Name + "$lod" + i};
                var lodRes = GeometryFromRef(node.Name, i, node.Model.Levels[i], dest.Materials, allGeos, res);
                if (lodRes.IsError)
                    return EditResult<ModelNode>.Error($"Unable to export node {node.Name} (Level {i})",
                        lodRes.Messages);
                lod.Geometry = lodRes.Data;
                sm.Children.Add(lod);
            }
        }
        if (settings.IncludeHardpoints)
        {
            var id = is3db ? 0 : CrcTool.FLModelCrc(node.Name);
            foreach (var hp in node.Model.Hardpoints)
            {
                sm.Children.Add(FromHardpoint(hp, id, res, dest.Materials, allGeos, settings.IncludeHulls ? sur : null));
            }
        }
        if (settings.IncludeHulls && sur != null)
        {
            var hulls = sur.GetMesh(new ConvexMeshId(is3db ? 0 : CrcTool.FLModelCrc(node.Name), 0));
            for (int i = 0; i < hulls.Length; i++)
            {
                var surnode = new ModelNode
                {
                    Geometry = GeometryFromSur(node.Name + "." + i + "$hull", hulls[i], res, dest.Materials, allGeos),
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
                Geometry = GeometryFromVMeshWire(node.Name, node.Model.VMeshWire, res, dest.Materials, allGeos),
                Name = node.Name + ".vmeshwire"
            };
            sm.Children.Add(meshNode);
        }
        foreach (var n in node.Children)
        {
            var child = ProcessNode(n, dest, settings, res, sur, allGeos, is3db);
            if (child.IsError)
                return child;
            sm.Children.Add(child.Data);
        }
        return sm.AsResult();
    }

    static int IndexFromFlags(int flags)
    {
        var sf = (SamplerFlags)flags;
        return ((sf & SamplerFlags.SecondUV) == SamplerFlags.SecondUV) ? 1 : 0;
    }

    static Material GetMaterial(uint crc, ResourceManager resources, Dictionary<string, Material> materials)
    {
        LibreLancer.Utf.Mat.Material mat;
        Color4 dc;
        string name;
        string dt = null;
        string et = null;
        Color4 ec;
        int etIndex = 0;
        if ((mat = resources.FindMaterial(crc)) != null)
        {
            name = mat.Name;
            dc = mat.Dc;
            dt = mat.DtName;
            et = mat.EtName;
            etIndex = IndexFromFlags(mat.EtFlags);
            if (et != null && mat.Ec == null)
                ec = Color4.White;
            else
                ec = mat.Ec ?? Color4.Black;
        }
        else
        {
            name = $"material_0x{crc:X8}";
            dc = Color4.White;
            ec = Color4.Black;
        }
        if (!materials.TryGetValue(name, out var m))
        {
            var x = dt != null ? new TextureInfo(dt, 0) : null;
            var y = et != null ? new TextureInfo(et, etIndex) : null;
            m = new Material()
            {
                Name = name, DiffuseColor = LinearColor.FromSrgb(dc), DiffuseTexture = x, EmissiveTexture = y,
                EmissiveColor = new Vector3(ec.R, ec.G, ec.B)
            };
            materials[name] = m;
        }
        return m;
    }

    static Geometry GeometryFromSur(string name, ConvexMesh ms, ResourceManager resources, Dictionary<string, Material> materials, List<Geometry> geometries)
    {
        var va = new VertexArray(VertexAttributes.None, ms.Vertices.Length);
        for (int i = 0; i < ms.Vertices.Length; i++)
            va.Position[i] = ms.Vertices[i];
        var geo = new Geometry(va, Indices.FromBuffer(ms.Indices.Select(x => (uint)x).ToArray()));
        geo.Name = name;
        geo.Groups = [new TriangleGroup(GetMaterial(0, resources, materials)) { IndexCount = ms.Indices.Length }];
        return geo;
    }


    static Geometry GeometryFromVMeshWire(string name, VMeshWire wire, ResourceManager resources,
        Dictionary<string, Material> materials, List<Geometry> allGeos)
    {
        var gb = new GeometryBuilder(VertexAttributes.None);
        var mesh = resources.FindMeshData(wire.MeshCRC);
        for (int i = 0; i < wire.NumIndices; i++)
        {
            var idx = wire.VertexOffset + wire.Indices[i];
            var vert = new Vertex() {Position = mesh.GetPosition(idx)};
            gb.Add(ref vert);
        }
        gb.AddGroup(GetMaterial(0, resources, materials));
        var geo = gb.Finish();
        geo.Name = $"{name}.wire.mesh";
        geo.Kind = GeometryKind.Lines;
        allGeos.Add(geo);
        return geo;
    }

    // Get just the referenced geometry from the VMeshData
    static EditResult<Geometry> GeometryFromRef(string name, int level, VMeshRef vms, Dictionary<string,Material> materials, List<Geometry> geometries, ResourceManager resources)
    {
        var mesh = resources.FindMeshData(vms.MeshCrc);
        if (vms.MeshCrc == 0)
            return new EditResult<Geometry>(null);
        if ((mesh == null))
            return EditResult<Geometry>.Error($"{name} - VMeshData lookup failed 0x{vms.MeshCrc}");

        var attrs = VertexAttributes.None;
        if (mesh.VertexFormat.Normal)
            attrs |= VertexAttributes.Normal;
        if (mesh.VertexFormat.Diffuse)
            attrs |= VertexAttributes.Diffuse;
        if (mesh.VertexFormat.TexCoords > 1 && mesh.VertexFormat.TexCoords != 3)
        {
            attrs |= VertexAttributes.Texture1 | VertexAttributes.Texture2;
        }
        else if (mesh.VertexFormat.TexCoords == 1)
            attrs |= VertexAttributes.Texture1;

        var gb = new GeometryBuilder(attrs);
        for (int meshi = vms.StartMesh; meshi < vms.StartMesh + vms.MeshCount; meshi++)
        {
            var m = mesh.Meshes[meshi];
            for (int i = m.TriangleStart; i < m.TriangleStart + m.NumRefVertices; i++)
            {
                int idx = mesh.Indices[i] + m.StartVertex + vms.StartVertex;
                Vertex vert = new Vertex() { Position = mesh.GetPosition(idx) };
                if (mesh.VertexFormat.Normal)
                    vert.Normal = mesh.GetNormal(idx);
                if (mesh.VertexFormat.Diffuse)
                    vert.Diffuse = LinearColor.FromSrgb((Color4)mesh.GetDiffuse(idx));
                if (mesh.VertexFormat.TexCoords > 1 && mesh.VertexFormat.TexCoords != 3)
                {
                    vert.Texture2 = mesh.GetTexCoord(idx, 1);
                    vert.Texture1 = mesh.GetTexCoord(idx, 0);
                }
                else if (mesh.VertexFormat.TexCoords == 1)
                {
                    vert.Texture1 = mesh.GetTexCoord(idx, 0);
                }
                gb.Add(ref vert);
            }
            gb.AddGroup(GetMaterial(m.MaterialCrc, resources, materials));
        }
        var geo = gb.Finish();
        geo.Name = $"{name}.{(int)mesh.VertexFormat.FVF}.level{level}";
        geometries.Add(geo);
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
