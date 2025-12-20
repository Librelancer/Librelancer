using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Utf;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Vms;
using LibreLancer.World;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedModel
{
    public string Name;
    public string Copyright;
    public ImportedModelNode Root;
    public Dictionary<string, ImageData> Images;
    public Animation[] ImportAnimations;

    public static EditResult<ImportedModel> FromSimpleMesh(string name, SimpleMesh.Model input)
    {
        Dictionary<string, ModelNode[]> autodetect = new Dictionary<string,ModelNode[]>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var obj in input.Roots)
        {
            var res = GetLods(obj, autodetect);
            if (res.IsError)
                return new EditResult<ImportedModel>(null, res.Messages);
        }
        List<ImportedModelNode> nodes = new List<ImportedModelNode>();
        foreach(var obj in input.Roots) {
            var res = AutodetectTree(obj, nodes, null, autodetect);
            if (res.IsError)
                return new EditResult<ImportedModel>(null, res.Messages);
        }
        if (nodes.Count > 1) {
            return EditResult<ImportedModel>.Error("More than one root model");
        }
        if (nodes.Count == 0) {
            return EditResult<ImportedModel>.Error("Could not find root model");
        }
        if (nodes[0].Def == null)
        {
            return EditResult<ImportedModel>.Error("Model root must be a mesh");
        }
        if(nodes[0].Def.Geometry?.Kind == GeometryKind.Lines)
            return EditResult<ImportedModel>.Error("Root mesh cannot be wireframe");
        var geo = CheckGeometries(nodes[0]);
        if (geo.IsError)
            return new EditResult<ImportedModel>(null, geo.Messages);
        var m = new ImportedModel()
            { Name = name, Root = nodes[0], Images = input.Images, Copyright = input.Copyright ?? "" };
        //Set up root
        m.Root.Construct = null;
        HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<EditMessage> warnings = new List<EditMessage>();
        usedNames.Add("Root");
        foreach (var child in m.Root.Children)
        {
            child.Construct.ParentName = "Root";
            CheckDuplicateNaming(child, usedNames, warnings);
        }

        if (input.Animations != null) {
            m.ImportAnimations = input.Animations.Where(x => !"<Default>".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
            var defAnim =
                input.Animations.FirstOrDefault(x => "<Default>".Equals(x.Name, StringComparison.OrdinalIgnoreCase));
            if (defAnim != null)
            {
                foreach (var child in m.Root.Children)
                    ApplyDefaultPose(defAnim, child);
            }
        }

        return new EditResult<ImportedModel>(m, warnings);
    }

    static void CheckDuplicateNaming(ImportedModelNode n, HashSet<string> usedNames, List<EditMessage> warnings)
    {
        int j = 0;
        var ogName = n.Name;
        while (usedNames.Contains(n.Name))
        {
            n.Name = $"{ogName}.{j:D4}";
            j++;
        }
        if (ogName != n.Name) {
            warnings.Add(EditMessage.Warning($"Renamed duplicate node name '{ogName}' to '{n.Name}'"));
            usedNames.Add(n.Name);
        }
        foreach (var child in n.Children) {
            child.Construct.ParentName = n.Name;
            CheckDuplicateNaming(child, usedNames, warnings);
        }
    }

    static EditResult<bool> CheckGeometries(ImportedModelNode node)
    {
        if (node.Def != null)
        {
            if (node.Def.Geometry.Indices.Length > ushort.MaxValue)
            {
                return EditResult<bool>.Error($"Node {node.Name ?? "(noname)"} has >65535 indices");
            }
            if (node.Def.Geometry.Vertices.Length > ushort.MaxValue)
            {
                return EditResult<bool>.Error($"Node {node.Name ?? "(noname)"} has >65535 vertices");
            }
            if (node.Def.Geometry.Indices.Indices16 == null &&
                node.Def.Geometry.Indices.Indices32 != null)
            {
                return EditResult<bool>.Error($"Node {node.Name ?? "(no name)"} requires 32-bit indices, which are unsupported. Vertex count too high");
            }
        }
        foreach (var c in node.Children)
        {
            var res = CheckGeometries(c);
            if (res.IsError) return res;
        }
        return true.AsResult();
    }

    static void ApplyDefaultPose(Animation anm, ImportedModelNode node)
    {
        var tr = anm.Translations.FirstOrDefault(x => x.Target.Equals(node.Name, StringComparison.OrdinalIgnoreCase));
        var rot = anm.Rotations.FirstOrDefault(x => x.Target.Equals(node.Name, StringComparison.OrdinalIgnoreCase));
        if (tr != null || rot != null)
        {
            var con = node.Construct.Clone();
            var p = tr != null ? tr.Keyframes[0].Translation : con.Origin;
            var r = rot != null
                ? rot.Keyframes[0].Rotation
                : con.Rotation;
            con.Rotation = r;
            con.Origin = p;
            node.Construct = con;
        }
        foreach(var child in node.Children)
            ApplyDefaultPose(anm, child);
    }

    static bool IsIgnored(ModelNode node)
    {
        return node.Properties.ContainsKey("export_ignore");
    }

    static bool IsHull(ModelNode node)
    {
        return !IsIgnored(node) &&
               (node.Properties.ContainsKey("hull") ||
                node.Name.EndsWith("$hull"));
    }

    static bool GetHardpoint(ModelNode node, out ImportedHardpoint hp)
    {
        hp = null;
        PropertyValue pv;
        if (!node.Properties.TryGetValue("hardpoint", out pv) || !pv.AsBoolean())
            return false;
        var orientation = Matrix4x4.CreateFromQuaternion(node.Transform.ExtractRotation());
        var position = Vector3.Transform(Vector3.Zero, node.Transform);
        HardpointDefinition hpdef;
        if (node.Properties.TryGetValue("hptype", out pv) && pv.AsString(out var hptype) &&
            hptype.Equals("rev", StringComparison.OrdinalIgnoreCase))
        {
            Vector3 axis;
            float min;
            float max;
            if (!node.Properties.TryGetValue("axis", out pv) || !pv.AsVector3(out axis))
                axis = Vector3.UnitY;
            if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                min = -45f;
            if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                max = 45f;
            if (min > max) {
                (min, max) = (max, min);
            }
            hpdef = new RevoluteHardpointDefinition(node.Name) {
                Orientation = orientation,
                Position = position,
                Min = MathHelper.DegreesToRadians(min),
                Max = MathHelper.DegreesToRadians(max),
                Axis = axis,
            };
        }
        else
        {
            hpdef = new FixedHardpointDefinition(node.Name) {Orientation = orientation, Position = position};
        }

        hp = new ImportedHardpoint() { Hardpoint = hpdef, Hulls = node.Children.Where(IsHull).ToList() };
        return true;
    }

    static (AbstractConstruct, bool) GetConstruct(ModelNode node, string childName, string parentName)
    {
        var rot = node.Transform.ExtractRotation();
        var origin = Vector3.Transform(Vector3.Zero, node.Transform);
        bool propsSet = false;
        if (!node.Properties.TryGetValue("construct", out var construct) ||
            !construct.AsString(out var contype))
        {
            return (
                new FixConstruct() {Rotation = rot, Origin = origin, ParentName = parentName, ChildName = childName},
                true);
        }
        PropertyValue pv;
        Vector3 axis;
        Vector3 offset = Vector3.Zero;
        float min;
        float max;
        switch (contype.ToLowerInvariant())
        {
            case "rev":
            {

                if(node.Properties.TryGetValue("offset", out pv))  pv.AsVector3(out offset);
                if (!node.Properties.TryGetValue("axis_rotation", out pv) || !pv.AsVector3(out axis))
                    axis = Vector3.UnitY;
                else
                    propsSet = true;
                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                    min = -90f;
                else
                    propsSet = true;
                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                    max = 90f;
                else
                    propsSet = true;
                if (min > max) {
                    (min, max) = (max, min);
                }
                return (new RevConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Min = MathHelper.DegreesToRadians(min),
                    Max = MathHelper.DegreesToRadians(max),
                    AxisRotation = axis,
                    Offset = offset,
                    ParentName = parentName,
                    ChildName = node.Name,
                }, propsSet);
            }
            case "pris":
            {
                if (node.Properties.TryGetValue("offset", out pv)) {
                    propsSet = true;
                    pv.AsVector3(out offset);
                }
                if (!node.Properties.TryGetValue("axis_translation", out pv) || !pv.AsVector3(out axis))
                    axis = Vector3.UnitY;
                else
                    propsSet = true;
                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsSingle(out min))
                    min = 0;
                else
                    propsSet = true;
                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsSingle(out max))
                    max = 1;
                else
                    propsSet = true;
                if (min > max) {
                    (min, max) = (max, min);
                }
                return (new PrisConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Min = min,
                    Max = max,
                    AxisTranslation = axis,
                    Offset = offset,
                    ParentName = parentName,
                    ChildName = childName
                }, propsSet);
            }
            case "sphere":
                if (node.Properties.TryGetValue("offset", out pv)) {
                    pv.AsVector3(out offset);
                    propsSet = true;
                }

                if (!node.Properties.TryGetValue("min", out pv) || !pv.AsVector3(out var minaxis)) {
                    minaxis = new Vector3(-MathF.PI);
                    propsSet = true;
                }

                if (!node.Properties.TryGetValue("max", out pv) || !pv.AsVector3(out var maxaxis)) {
                    maxaxis = new Vector3(MathF.PI);
                    propsSet = true;
                }

                return (new SphereConstruct()
                {
                    Rotation = rot, Origin = origin,
                    Offset = offset,
                    Min1 = minaxis.X, Min2 = minaxis.Y, Min3 = minaxis.Z,
                    Max1 = maxaxis.X, Max2 = maxaxis.Y, Max3 = maxaxis.Z,
                    ParentName = parentName,
                    ChildName = childName
                }, propsSet);
            case "fix":
            default:
                return (new FixConstruct() {Rotation = rot, Origin = origin, ParentName = parentName, ChildName = childName}, propsSet);
        }
    }

    static bool IsWire(ModelNode mn) => mn.Geometry != null && mn.Geometry.Kind == GeometryKind.Lines;


    static EditResult<bool> AutodetectTree(ModelNode obj, List<ImportedModelNode> parent, string parentName, Dictionary<string,ModelNode[]> autodetect)
    {
        //Skip detected lods & hulls
        var num = LodNumber(obj, out _);
        if (num != 0 && num != NULL_GEOMETRY) return true.AsResult();
        if (IsHull(obj)) return true.AsResult();
        //Build tree
        var mdl = new ImportedModelNode();
        mdl.Name = obj.Name;
        if (obj.Name.EndsWith("$lod0", StringComparison.InvariantCultureIgnoreCase))
            mdl.Name = obj.Name.Remove(obj.Name.Length - 5, 5);
        (mdl.Construct, mdl.ConstructPropertiesSet) = GetConstruct(obj, mdl.Name, parentName);
        mdl.Construct?.Reset();
        if (num != NULL_GEOMETRY) {
            var geometry = autodetect[mdl.Name];
            foreach (var g in geometry)
                if (g != null)
                    mdl.LODs.Add(g);
        }
        foreach(var child in obj.Children)
        {
            if (IsIgnored(child))
                continue;
            if(IsHull(child))
                mdl.Hulls.Add(child);
            else if (IsWire(child))
            {
                if (mdl.Wire != null)
                    return EditResult<bool>.Error($"Node {obj.Name} has more than one wireframe child");
                mdl.Wire = child;
            }
            else if (GetHardpoint(child, out var hp))
                mdl.Hardpoints.Add(hp);
            else
                AutodetectTree(child, mdl.Children, mdl.Name, autodetect);
        }
        parent.Add(mdl);
        return true.AsResult();
    }
    static EditResult<bool> GetLods(ModelNode obj, Dictionary<string,ModelNode[]> autodetect)
    {
        string objn;
        var num = LodNumber(obj, out objn);
        if(num >= 0) {
            ModelNode[] lods;
            if(!autodetect.TryGetValue(objn, out lods)) {
                lods = new ModelNode[10];
                autodetect.Add(objn, lods);
            }
            lods[num] = obj;
        }
        foreach (var child in obj.Children)
        {
            var res = GetLods(child, autodetect);
            if (res.IsError) return res;
        }
        return true.AsResult();
    }

    static bool CheckSuffix(string postfixfmt, string src, int count)
    {
        for (int i = 0; i < count; i++)
            if (src[src.Length - postfixfmt.Length + i] != postfixfmt[i]) return false;
        return true;
    }

    private const int NULL_GEOMETRY = -2;
    private const int LINE_GEOMETRY = -1;
    //Autodetected LOD: object with geometry + suffix $lod[0-9]
    static int LodNumber(ModelNode obj, out string name)
    {
        name = obj.Name;
        if (obj.Geometry == null) return NULL_GEOMETRY;
        if (obj.Geometry.Kind == GeometryKind.Lines) return LINE_GEOMETRY;
        if (obj.Name.Length < 6) return 0;
        if (!char.IsDigit(obj.Name, obj.Name.Length - 1)) return 0;
        if (!CheckSuffix("$lodX", obj.Name, 4)) return 0;
        name = obj.Name.Substring(0, obj.Name.Length - "$lodX".Length);
        return int.Parse(obj.Name[obj.Name.Length - 1] + "");
    }

    bool VerifyModelMaterials(ModelNode mn)
    {
        if (mn != null && mn.Geometry.Groups.Any(x => string.IsNullOrWhiteSpace(x.Material.Name)))
            return false;
        return true;
    }

    bool VerifyMaterials(ImportedModelNode r)
    {
        foreach (var l in r.LODs)
        {
            if (!VerifyModelMaterials(l)) return false;
        }
        if (!VerifyModelMaterials(r.Def)) return false;
        foreach(var child in r.Children)
            if (!VerifyMaterials(child))
                return false;
        return true;
    }

    class TaskHandler(bool multithreaded)
    {
        List<Task> tasks = new List<Task>();
        public void Run(Action task)
        {
            if (multithreaded)
                tasks.Add(Task.Run(task));
            else
                task();
        }
        public void Wait()
        {
            Task.WaitAll(tasks.ToArray());
        }
    }

    public EditResult<EditableUtf> CreateModel(ModelImporterSettings settings)
    {
        var utf = new EditableUtf();
        var tasks = new TaskHandler(settings.Multithreaded);
        //Vanity
        if (!string.IsNullOrWhiteSpace(Copyright))
        {
            utf.Root.Children.Add(LUtfNode.StringNode(utf.Root, "Copyright", Copyright));
        }
        utf.Root.Children.Add(LUtfNode.StringNode(utf.Root, "Exporter Version",  "LancerEdit " + Platform.GetInformationalVersion<ImportedModel>()));
        List<EditMessage> warnings = new List<EditMessage>();

        if (string.IsNullOrWhiteSpace(Name))
            return EditResult<EditableUtf>.Error("Model name cannot be empty");
        if (Root == null)
            return EditResult<EditableUtf>.Error("Model must have a root node");
        if(Root.LODs.Count == 0)
            return EditResult<EditableUtf>.Error("Model root must have a mesh");
        if (!VerifyMaterials(Root))
            return EditResult<EditableUtf>.Error("Material name cannot be empty");
        if (Root.Children.Count == 0 && !settings.ForceCompound)
            Export3DB(Name, utf.Root, Root, settings);
        else
        {
            var suffix = $".{IdSalt.New()}.3db";
            var vmslib = new LUtfNode() {Name = "VMeshLibrary", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(vmslib);
            var cmpnd = new LUtfNode() {Name = "Cmpnd", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(cmpnd);
            ExportModels(Name, utf.Root, suffix, vmslib, Root, settings);
            //Animations
            if (ImportAnimations != null && ImportAnimations.Length > 0)
            {
                List<ImportedModelNode> allNodes = new List<ImportedModelNode>();
                BuildNodeList(Root, allNodes);
                var anms = new AnmFile();
                foreach (var ani in ImportAnimations)
                {
                    var res = AnimationConversion.ImportAnimation(allNodes, ani);
                    warnings.AddRange(res.Messages.Select(x => EditMessage.Warning(x.Message)));
                    if(!res.IsError)
                        anms.Scripts.Add(res.Data.Name, res.Data);
                }
                if (anms.Scripts.Count > 0)
                {
                    var anims = new LUtfNode() {Name = "Animation", Parent = utf.Root, Children = new List<LUtfNode>()};
                    utf.Root.Children.Add(anims);
                    AnimationWriter.WriteAnimations(anims, anms);
                }
            }
            int cmpndIndex = 1;
            //Build compound nodes
            var consBuilder = new ConsBuilder();
            cmpnd.Children.Add(CmpndNode(cmpnd, "Root", Root.Name + suffix, "Root", 0));
            foreach (var child in Root.Children)
            {
                ProcessConstruct(child, cmpnd, consBuilder, suffix, ref cmpndIndex);
            }

            var cons = new LUtfNode() {Name = "Cons", Parent = cmpnd, Children = new List<LUtfNode>()};
            if (consBuilder.Fix != null) {
                cons.Children.Add(new LUtfNode() { Name = "Fix", Parent = cons, Data = consBuilder.Fix.GetData()});
            }
            if (consBuilder.Rev != null) {
                cons.Children.Add(new LUtfNode() { Name = "Rev", Parent = cons, Data = consBuilder.Rev.GetData()});
            }
            if (consBuilder.Pris != null) {
                cons.Children.Add(new LUtfNode() { Name = "Pris", Parent = cons, Data = consBuilder.Pris.GetData()});
            }
            if (consBuilder.Sphere != null) {
                cons.Children.Add(new LUtfNode() { Name = "Sphere", Parent = cons, Data = consBuilder.Sphere.GetData()});
            }
            if(cons.Children.Count > 0)
                cmpnd.Children.Add(cons);
        }

        if (settings.GenerateMaterials)
        {
            List<SimpleMesh.Material> materials = new List<SimpleMesh.Material>();
            IterateMaterials(materials, Root);
            var mats = new LUtfNode() { Name = "material library", Parent = utf.Root };
            mats.Children = new List<LUtfNode>();

            var txms = new LUtfNode() { Name = "texture library", Parent = utf.Root };
            txms.Children = new List<LUtfNode>();
            HashSet<string> createdTextures = new HashSet<string>();
            foreach (var mat in materials)
            {
                var dt = mat.DiffuseTexture?.Name ?? (settings.GeneratePlaceholderTextures ? mat.Name : null);
                GenerateTexture(dt, warnings, createdTextures, txms, settings, DDSFormat.DXT5, tasks);
                GenerateTexture(mat.EmissiveTexture?.Name, warnings, createdTextures, txms, settings, DDSFormat.DXT1, tasks);
                if (settings.AdvancedMaterials)
                {
                    GenerateTexture(mat.NormalTexture?.Name, warnings, createdTextures, txms, settings, DDSFormat.RGTC2, tasks);
                    GenerateMetallicRoughnessTexture(mat.MetallicRoughnessTexture?.Name, warnings, createdTextures, txms, settings, tasks);
                }
            }

            int i = 0;
            foreach (var mat in materials)
                mats.Children.Add(DefaultMaterialNode(mats,mat,i++, settings));

            if (mats.Children.Count > 0)
            {
                mats.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
                utf.Root.Children.Add(mats);
            }
            if (txms.Children.Count > 0)
            {
                txms.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
                utf.Root.Children.Add(txms);
            }
        }
        tasks.Wait();
        return new EditResult<EditableUtf>(utf, warnings);
    }

    void GenerateMetallicRoughnessTexture(string texture, List<EditMessage> warnings, HashSet<string> createdTextures, LUtfNode txms, ModelImporterSettings settings, TaskHandler tasks)
    {
        if (string.IsNullOrWhiteSpace(texture)) return;
        if (!createdTextures.Add(texture)) return;
        if (settings.ImportTextures && Images != null && Images.TryGetValue(texture, out var img))
        {
            var mtl = ImportTextureNode(txms, texture + "_METAL", img.Data, DDSFormat.MetallicRGTC1, tasks);
            var rough = ImportTextureNode(txms, texture + "_ROUGH", img.Data, DDSFormat.RoughnessRGTC1, tasks);
            if(mtl.IsSuccess)
                txms.Children.Add(mtl.Data);
            else
                warnings.Add(EditMessage.Warning($"{texture}_METAL not imported"));
            warnings.AddRange(mtl.Messages.Select(x => EditMessage.Warning(x.Message)));
            if (rough.IsSuccess)
                txms.Children.Add(rough.Data);
            else
                warnings.Add(EditMessage.Warning($"{texture}_ROUGH not imported"));
            warnings.AddRange(rough.Messages.Select(x => EditMessage.Warning(x.Message)));
        }
    }

    void GenerateTexture(string texture, List<EditMessage> warnings, HashSet<string> createdTextures, LUtfNode txms, ModelImporterSettings settings, DDSFormat format, TaskHandler tasks)
    {
        if (string.IsNullOrWhiteSpace(texture)) return;
        if (!createdTextures.Add(texture)) return;
        if (settings.ImportTextures && Images != null && Images.TryGetValue(texture, out var img))
        {
            var result = ImportTextureNode(txms, texture, img.Data, format, tasks);
            if(result.IsSuccess)
                txms.Children.Add(result.Data);
            else
                warnings.Add(EditMessage.Warning($"{texture} not imported"));
            warnings.AddRange(result.Messages.Select(x => EditMessage.Warning(x.Message)));
        }
        else if (settings.GeneratePlaceholderTextures)
        {
            txms.Children.Add(DefaultTextureNode(txms, texture));
        }
    }

    static LUtfNode DefaultMaterialNode(LUtfNode parent, SimpleMesh.Material mat, int i, ModelImporterSettings settings)
    {
        var matnode = new LUtfNode() { Name = mat.Name, Parent = parent };
        matnode.Children = new List<LUtfNode>();
        var type = (!string.IsNullOrWhiteSpace(mat.EmissiveTexture?.Name) ||
                    mat.EmissiveColor != Vector3.Zero)
            ? "DcDtEcEt"
            : "DcDt";
        matnode.Children.Add(new LUtfNode() { Name = "Type", Parent = matnode, StringData = type });
        var srgb = mat.DiffuseColor.ToSrgb();
        var arr = new float[] { srgb.X, srgb.Y, srgb.Z };
        matnode.Children.Add(new LUtfNode() { Name = "Dc", Parent = matnode, Data = UnsafeHelpers.CastArray(arr) });
        if (settings.GeneratePlaceholderTextures || !string.IsNullOrWhiteSpace(mat.DiffuseTexture?.Name))
        {
            string textureName = (mat.DiffuseTexture?.Name ?? mat.Name) + ".dds";
            matnode.Children.Add(LUtfNode.StringNode(matnode, "Dt_name", textureName));
            matnode.Children.Add(LUtfNode.IntNode(matnode, "Dt_flags", 64));
        }
        if (!string.IsNullOrWhiteSpace(mat.EmissiveTexture?.Name))
        {
            string textureName = mat.EmissiveTexture.Name  + ".dds";
            matnode.Children.Add(LUtfNode.StringNode(matnode, "Et_name", textureName));
            matnode.Children.Add(LUtfNode.IntNode(matnode, "Et_flags", 64));
        }
        else if (mat.EmissiveColor != Vector3.Zero) //TODO: Maybe EcEt is not right in Librelancer?
        {
            arr = new float[] { mat.EmissiveColor.X, mat.EmissiveColor.Y, mat.EmissiveColor.Z };
            matnode.Children.Add(new LUtfNode() { Name = "Ec", Parent = matnode, Data = UnsafeHelpers.CastArray(arr) });
        }
        if (!string.IsNullOrWhiteSpace(mat.NormalTexture?.Name) && settings.AdvancedMaterials)
        {
            string textureName = mat.NormalTexture.Name + ".dds";
            matnode.Children.Add(LUtfNode.StringNode(matnode, "Nm_name", textureName));
            matnode.Children.Add(LUtfNode.IntNode(matnode, "Nm_flags", 64));
        }
        if (mat.MetallicRoughness && settings.AdvancedMaterials)
        {
            matnode.Children.Add(LUtfNode.FloatNode(matnode, "M_factor", mat.MetallicFactor));
            matnode.Children.Add(LUtfNode.FloatNode(matnode, "R_factor", mat.RoughnessFactor));
            if (!string.IsNullOrWhiteSpace(mat.MetallicRoughnessTexture?.Name))
            {
                string textureNameRough = mat.MetallicRoughnessTexture.Name + "_ROUGH.dds";
                string textureNameMetal = mat.MetallicRoughnessTexture.Name + "_METAL.dds";
                matnode.Children.Add(LUtfNode.StringNode(matnode, "Rt_name", textureNameRough));
                matnode.Children.Add(LUtfNode.IntNode(matnode, "Rt_flags", 64));
                matnode.Children.Add(LUtfNode.StringNode(matnode, "Mt_name",  textureNameMetal));
                matnode.Children.Add(LUtfNode.IntNode(matnode, "Mt_flags", 64));
            }
        }

        return matnode;
    }

    static EditResult<LUtfNode> ImportTextureNode(LUtfNode parent, string name, ReadOnlySpan<byte> data, DDSFormat format, TaskHandler tasks)
    {
        var texnode = new LUtfNode() { Name = name + ".dds", Parent = parent };
        texnode.Children = new List<LUtfNode>();
        var d = data.ToArray();

        var analyzed = TextureImport.OpenBuffer(d, null);
        if (!analyzed.IsError) {
            if (format == DDSFormat.DXT5 && analyzed.Data.Type == TexLoadType.Opaque) {
                format = DDSFormat.DXT1;
            }
            tasks.Run(() => texnode.Children.Add(TextureImport.ImportAsMIPSNode(d, texnode, format)));
        }
        else
        {
            return EditResult<LUtfNode>.Error($"{name}: {analyzed.AllMessages()}");
        }
        return new EditResult<LUtfNode>(texnode, analyzed.Messages.Select(x => EditMessage.Warning($"{name}: {x.Message}")));
    }

    static LUtfNode DefaultTextureNode(LUtfNode parent, string name)
    {
        var texnode = new LUtfNode() { Name = name + ".dds", Parent = parent };
        texnode.Children = new List<LUtfNode>();
        var d = new byte[DefaultTexture.Data.Length];
        Buffer.BlockCopy(DefaultTexture.Data, 0, d, 0, DefaultTexture.Data.Length);
        texnode.Children.Add(new LUtfNode() { Name = "MIPS", Parent = texnode, Data = d });
        return texnode;
    }

    static bool HasMat(List<Material> materials, Material m)
    {
        foreach (var m2 in materials)
        {
            if (m2.Name == m.Name) return true;
        }
        return false;
    }
    static void IterateMaterials(List<Material> materials, ImportedModelNode mdl)
    {
        foreach (var lod in mdl.LODs)
        foreach (var dc in lod.Geometry.Groups)
            if (dc.Material != null && !HasMat(materials, dc.Material))
                materials.Add(dc.Material);
        foreach (var child in mdl.Children)
            IterateMaterials(materials, child);
    }

    class ConsBuilder
    {
        public FixConstructor Fix;
        public RevConstructor Rev;
        public PrisConstructor Pris;
        public SphereConstructor Sphere;
    }

    void BuildNodeList(ImportedModelNode node, List<ImportedModelNode> allNodes)
    {
        allNodes.Add(node);
        foreach (var c in node.Children)
            BuildNodeList(c, allNodes);
    }

    void ProcessConstruct(ImportedModelNode mdl, LUtfNode cmpnd, ConsBuilder cons, string suffix,
        ref int index)
    {
        cmpnd.Children.Add(CmpndNode(cmpnd, "PART_" + mdl.Name, mdl.Name + suffix, mdl.Name, index++));
        switch (mdl.Construct)
        {
            case FixConstruct fix:
                cons.Fix ??= new FixConstructor();
                cons.Fix.Add(fix);
                break;
            case RevConstruct rev:
                cons.Rev ??= new RevConstructor();
                cons.Rev.Add(rev);
                break;
            case PrisConstruct pris:
                cons.Pris ??= new PrisConstructor();
                cons.Pris.Add(pris);
                break;
            case SphereConstruct sphere:
                cons.Sphere ??= new SphereConstructor();
                cons.Sphere.Add(sphere);
                break;
        }
        foreach (var child in mdl.Children)
            ProcessConstruct(child, cmpnd, cons, suffix, ref index);
    }

    LUtfNode CmpndNode(LUtfNode cmpnd, string name, string filename, string objname, int index)
    {
        var node = new LUtfNode() { Parent = cmpnd, Name = name };
        node.Children =
        [
            LUtfNode.StringNode(node, "File Name", filename),
            LUtfNode.StringNode(node, "Object Name", objname),
            LUtfNode.IntNode(node, "Index", index)
        ];
        return node;
    }

    void ExportModels(string mdlName, LUtfNode root, string suffix, LUtfNode vms, ImportedModelNode model, ModelImporterSettings settings)
    {
        var modelNode = new LUtfNode() {Parent = root, Name = model.Name + suffix};
        modelNode.Children = new List<LUtfNode>();
        root.Children.Add(modelNode);
        Export3DB(mdlName, modelNode, model, settings, vms);
        foreach (var child in model.Children)
            ExportModels(mdlName, root, suffix, vms, child, settings);
    }

    static ushort[] GetIndicesForWire(Geometry lod, Geometry vmeshwire)
    {
        ushort[] newIndices = new ushort[vmeshwire.Indices.Length];
        for (int i = 0; i < vmeshwire.Indices.Length; i++)
        {
            var pos = vmeshwire.Vertices[vmeshwire.Indices.Indices16[i]].Position;
            int j;
            for (j = 0; j < lod.Vertices.Length; j++)
            {
                if (Vector3.Distance(lod.Vertices[j].Position, pos) < 0.0001f)
                    break;
            }
            if (j == lod.Vertices.Length)
                return null;
            newIndices[i] = (ushort)j;
        }
        return newIndices;
    }

    static LUtfNode GetVMeshWireNode(LUtfNode parentNode, uint crc, ushort[] indices)
    {
        var vertexOffset = indices.Min();
        var max = indices.Max();
        using var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write(VMeshWire.HEADER_SIZE);
        writer.Write(crc);
        writer.Write(vertexOffset);
        writer.Write((ushort)(max - vertexOffset + 1)); //vertex count
        writer.Write((ushort)(indices.Length)); //index count
        writer.Write(max); //max vertex
        foreach(var i in indices)
            writer.Write((ushort)(i - vertexOffset));
        var wireNode = new LUtfNode{Name = "VMeshWire", Parent = parentNode, Children = new List<LUtfNode>()};
        wireNode.Children.Add(new LUtfNode() { Name = "VWireData", Parent = wireNode, Data = ms.ToArray()});
        return wireNode;
    }

    static void Export3DB(string mdlName, LUtfNode node3db, ImportedModelNode mdl, ModelImporterSettings settings, LUtfNode vmeshlibrary = null)
    {
        var vms = vmeshlibrary ?? new LUtfNode()
            { Name = "VMeshLibrary", Parent = node3db, Children = new List<LUtfNode>() };
        for (int i = 0; i < mdl.LODs.Count; i++)
        {
            if (settings.AdvancedMaterials)
            {
                if (mdl.LODs[i].Geometry.Groups.Any(x => x.Material.NormalTexture != null) &&
                    ((mdl.LODs[i].Geometry.Attributes & VertexAttributes.Tangent) != VertexAttributes.Tangent))
                {
                    TangentGeneration.GenerateMikkTSpace(mdl.LODs[i].Geometry);
                    mdl.LODs[i].Geometry.Attributes |= VertexAttributes.Tangent;
                }
            }
            var n = new LUtfNode()
            {
                Name = $"{mdlName}-{mdl.Name}.lod{i}.{(int)GeometryWriter.FVF(mdl.LODs[i].Geometry, settings.AdvancedMaterials)}.vms",
                Parent = vms
            };
            n.Children = new List<LUtfNode>();
            n.Children.Add(new LUtfNode()
                    { Name = "VMeshData", Parent = n, Data = GeometryWriter.VMeshData(mdl.LODs[i].Geometry, settings.AdvancedMaterials) });
            vms.Children.Add(n);
        }
        if (vmeshlibrary == null && mdl.LODs.Count > 0)
            node3db.Children.Add(vms);

        if (mdl.LODs.Count > 1)
        {
            var multilevel = new LUtfNode() {Name = "MultiLevel", Parent = node3db};
            multilevel.Children = new List<LUtfNode>();
            var switch2 = new LUtfNode() {Name = "Switch2", Parent = multilevel};
            multilevel.Children.Add(switch2);
            for (int i = 0; i < mdl.LODs.Count; i++)
            {
                var n = new LUtfNode() {Name = "Level" + i, Parent = multilevel};
                n.Children = new List<LUtfNode>();
                n.Children.Add(new LUtfNode() {Name = "VMeshPart", Parent = n, Children = new List<LUtfNode>()});
                n.Children[0].Children.Add(new LUtfNode()
                {
                    Name = "VMeshRef",
                    Parent = n.Children[0],
                    Data = GeometryWriter.VMeshRef(mdl.LODs[i].Geometry,
                        string.Format("{0}-{1}.lod{2}.{3}.vms", mdlName, mdl.Name, i,
                            (int) GeometryWriter.FVF(mdl.LODs[i].Geometry, settings.AdvancedMaterials)))
                });
                multilevel.Children.Add(n);
            }

            //Generate Switch2: TODO - Be more intelligent about this
            var mlfloats = new float[multilevel.Children.Count];
            mlfloats[0] = 0;
            float cutOff = 2250;
            for (int i = 1; i < mlfloats.Length - 1; i++)
            {
                mlfloats[i] = cutOff;
                cutOff *= 2;
            }

            mlfloats[mlfloats.Length - 1] = 1000000;
            switch2.Data = UnsafeHelpers.CastArray(mlfloats);
            node3db.Children.Add(multilevel);
        }
        else if (mdl.LODs.Count == 0)
        {
            var part = new LUtfNode() {Name = "VMeshPart", Parent = node3db};
            part.Children = new List<LUtfNode>();
            part.Children.Add(new LUtfNode()
            {
                Name = "VMeshRef",
                Parent = part,
                Data = GeometryWriter.NullVMeshRef()
            });
            node3db.Children.Add(part);
        }
        else
        {
            var part = new LUtfNode() {Name = "VMeshPart", Parent = node3db};
            part.Children = new List<LUtfNode>();
            part.Children.Add(new LUtfNode()
            {
                Name = "VMeshRef",
                Parent = part,
                Data = GeometryWriter.VMeshRef(mdl.LODs[0].Geometry,
                    string.Format("{0}-{1}.lod0.{2}.vms", mdlName, mdl.Name,
                        (int) GeometryWriter.FVF(mdl.LODs[0].Geometry, settings.AdvancedMaterials)))
            });
            node3db.Children.Add(part);
        }

        if (mdl.Hardpoints.Count > 0)
        {
            var hp = new ModelHpNode() {Node = node3db};
            hp.HardpointsToNodes(mdl.Hardpoints.Select(x => new Hardpoint(x.Hardpoint, null)).ToList());
        }

        if (mdl.Wire != null)
        {
            ushort[] wireIndices = null;
            Geometry wireLod = mdl.Wire.Geometry;
            Geometry srcGeometry = null;
            int i;
            for (i = 0; i < mdl.LODs.Count; i++)
            {
                if ((wireIndices = GetIndicesForWire(mdl.LODs[i].Geometry, wireLod)) != null) {
                    srcGeometry = mdl.LODs[i].Geometry;
                    break;
                }
            }
            if (wireIndices != null)
            {
                FLLog.Info("Import", $"{mdl.Name} VMeshWire created from existing VMeshData");
                node3db.Children.Add(GetVMeshWireNode(node3db,
                    CrcTool.FLModelCrc($"{mdlName}-{mdl.Name}.lod{i}.{(int) GeometryWriter.FVF(srcGeometry, settings.AdvancedMaterials)}.vms"),
                    wireIndices
                    ));
            }
            else
            {
                FLLog.Info("Import", $"{mdl.Name} VMeshWire creating new VMeshData");
                string nodeName = $"{mdlName}-{mdl.Name}.vmeshwire.pos.vms";
                var n = new LUtfNode()
                {
                    Name = nodeName,
                    Parent = vms
                };
                n.Children = new List<LUtfNode>();
                n.Children.Add(new LUtfNode()
                    {Name = "VMeshData", Parent = n, Data = GeometryWriter.VMeshData(wireLod, false, D3DFVF.XYZ)});
                vms.Children.Add(n);
                if(vmeshlibrary == null && mdl.LODs.Count == 0)
                    node3db.Children.Add(vms);
                node3db.Children.Add(GetVMeshWireNode(node3db, CrcTool.FLModelCrc(nodeName), wireLod.Indices.Indices16));
            }
        }
    }
}
