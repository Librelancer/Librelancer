using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public class ImportedModel
{
    public string Name;
    public ImportedModelNode Root;
    
    public static EditorResult<ImportedModel> FromSimpleMesh(string name, SimpleMesh.Model input)
    {
        Dictionary<string, ModelNode[]> autodetect = new Dictionary<string,ModelNode[]>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var obj in input.Roots)
            GetLods(obj, autodetect);
        List<ImportedModelNode> nodes = new List<ImportedModelNode>();
        foreach(var obj in input.Roots) {
            AutodetectTree(obj, nodes, autodetect);
        }
        if (nodes.Count > 1) {
            return EditorResult<ImportedModel>.FromMessages(EditorMessage.Error("More than one root model"));
        }
        if (nodes.Count == 0) {
            return EditorResult<ImportedModel>.FromMessages(EditorMessage.Error("Could not find root model"));
        }
        
        return new EditorResult<ImportedModel>(new ImportedModel() {Name = name, Root = nodes[0]});
    }
    
    static void AutodetectTree(ModelNode obj, List<ImportedModelNode> parent, Dictionary<string,ModelNode[]> autodetect)
    {
        var num = LodNumber(obj, out _);
        if (num != 0) return;
        var mdl = new ImportedModelNode();
        mdl.Name = obj.Name;
        if (obj.Name.EndsWith("_lod0", StringComparison.InvariantCultureIgnoreCase))
            mdl.Name = obj.Name.Remove(obj.Name.Length - 5, 5);
        var geometry = autodetect[mdl.Name];
        foreach (var g in geometry)
            if (g != null) mdl.LODs.Add(g);
        foreach(var child in obj.Children) {
            AutodetectTree(child, mdl.Children, autodetect);
        }
        parent.Add(mdl);
    }
    static void GetLods(ModelNode obj, Dictionary<string,ModelNode[]> autodetect)
    {
        string objn;
        var num = LodNumber(obj, out objn);
        if(num != -1) {
            ModelNode[] lods;
            if(!autodetect.TryGetValue(objn, out lods)) {
                lods = new ModelNode[10];
                autodetect.Add(objn, lods);
            }
            lods[num] = obj;
        }
        foreach (var child in obj.Children)
            GetLods(child, autodetect);
    }
    
    static bool CheckSuffix(string postfixfmt, string src, int count)
    {
        for (int i = 0; i < count; i++)
            if (src[src.Length - postfixfmt.Length + i] != postfixfmt[i]) return false;
        return true;
    }
    
    //Autodetected LOD: object with geometry + suffix _lod[0-9]
    static int LodNumber(ModelNode obj, out string name)
    {
        name = obj.Name;
        if (obj.Geometry == null) return -1;
        if (obj.Name.Length < 6) return 0;
        if (!char.IsDigit(obj.Name, obj.Name.Length - 1)) return 0;
        if (!CheckSuffix("_lodX", obj.Name, 4)) return 0;
        name = obj.Name.Substring(0, obj.Name.Length - "_lodX".Length);
        return int.Parse(obj.Name[obj.Name.Length - 1] + "");
    }

    bool VerifyModelMaterials(ModelNode mn)
    {
        if (mn.Geometry.Groups.Any(x => string.IsNullOrWhiteSpace(x.Material.Name)))
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
    
    public EditorResult<EditableUtf> CreateModel(ModelImporterSettings settings)
    {
        var utf = new EditableUtf();
        //Vanity
        var expv = new LUtfNode() {Name = "Exporter Version", Parent = utf.Root};
        expv.StringData = "LancerEdit " + Platform.GetInformationalVersion<ImportedModel>();
        utf.Root.Children.Add(expv);

        if (string.IsNullOrWhiteSpace(Name))
            return EditorResult<EditableUtf>.FromMessages(EditorMessage.Error("Model name cannot be empty"));
        
        if (Root == null)
            return EditorResult<EditableUtf>.FromMessages(EditorMessage.Error("Model must have a root node"));
        if(Root.LODs.Count == 0)
            return EditorResult<EditableUtf>.FromMessages(EditorMessage.Error("Model root must have a mesh"));
        if (!VerifyMaterials(Root))
            return EditorResult<EditableUtf>.FromMessages(EditorMessage.Error("Material name cannot be empty"));
        if (Root.Children.Count == 0)
            Export3DB(Name, utf.Root, Root);
        else
        {
            var suffix = (new Random().Next()) + ".3db";
            var vmslib = new LUtfNode() {Name = "VMeshLibrary", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(vmslib);
            var cmpnd = new LUtfNode() {Name = "Cmpnd", Parent = utf.Root, Children = new List<LUtfNode>()};
            utf.Root.Children.Add(cmpnd);
            ExportModels(Name, utf.Root, suffix, vmslib, Root);
            int cmpndIndex = 1;
            FixConstructor fix = new FixConstructor();
            cmpnd.Children.Add(CmpndNode(cmpnd, "Root", Root.Name + suffix, "Root", 0));
            foreach (var child in Root.Children)
            {
                ProcessConstruct("Root", child, cmpnd, fix, suffix, ref cmpndIndex);
            }

            var cons = new LUtfNode() {Name = "Cons", Parent = cmpnd, Children = new List<LUtfNode>()};
            var trs = new LUtfNode() {Name = "Fix", Parent = cons, Data = fix.GetData()};
            cons.Children.Add(trs);
            cmpnd.Children.Add(cons);
        }

        if (settings.GenerateMaterials)
        {
            List<SimpleMesh.Material> materials = new List<SimpleMesh.Material>();
            IterateMaterials(materials, Root);
            var mats = new LUtfNode() { Name = "material library", Parent = utf.Root };
            mats.Children = new List<LUtfNode>();
            int i = 0;
            foreach (var mat in materials)
                mats.Children.Add(DefaultMaterialNode(mats,mat,i++));
            var txms = new LUtfNode() { Name = "texture library", Parent = utf.Root };
            txms.Children = new List<LUtfNode>();
            foreach (var mat in materials)
                txms.Children.Add(DefaultTextureNode(txms,mat.Name));
            utf.Root.Children.Add(mats);
            utf.Root.Children.Add(txms);
        }
        
        return new EditorResult<EditableUtf>(utf);
    }
    
    static LUtfNode DefaultMaterialNode(LUtfNode parent, SimpleMesh.Material mat, int i)
    {
        var matnode = new LUtfNode() { Name = mat.Name, Parent = parent };
        matnode.Children = new List<LUtfNode>();
        matnode.Children.Add(new LUtfNode() { Name = "Type", Parent = matnode, StringData = "DcDt" });
        var arr = new float[] {mat.DiffuseColor.X, mat.DiffuseColor.Y, mat.DiffuseColor.Z};
        matnode.Children.Add(new LUtfNode() { Name = "Dc", Parent = matnode, Data = UnsafeHelpers.CastArray(arr) });
        matnode.Children.Add(new LUtfNode() { Name = "Dt_name", Parent = matnode, StringData = mat.Name + ".dds" });
        matnode.Children.Add(new LUtfNode() { Name = "Dt_flags", Parent = matnode, Data = BitConverter.GetBytes(64) });
        return matnode;
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

    void ProcessConstruct(string parentName, ImportedModelNode mdl, LUtfNode cmpnd, FixConstructor fix, string suffix,
        ref int index)
    {
        cmpnd.Children.Add(CmpndNode(cmpnd, "PART_" + mdl.Name, mdl.Name + suffix, mdl.Name, index++));
        if (mdl.Transform == true)
        {
            fix.Add(parentName, mdl.Name, mdl.Def.Transform);
        }
        else
        {
            fix.Add(parentName, mdl.Name, Matrix4x4.Identity);
        }

        foreach (var child in mdl.Children)
            ProcessConstruct(mdl.Name, child, cmpnd, fix, suffix, ref index);
    }

    LUtfNode CmpndNode(LUtfNode cmpnd, string name, string filename, string objname, int index)
    {
        var node = new LUtfNode() {Parent = cmpnd, Name = name, Children = new List<LUtfNode>()};
        node.Children.Add(new LUtfNode()
        {
            Name = "File Name",
            Parent = node,
            StringData = filename
        });
        node.Children.Add(new LUtfNode()
        {
            Name = "Object Name",
            Parent = node,
            StringData = objname,
        });
        node.Children.Add(new LUtfNode()
        {
            Name = "Index",
            Parent = node,
            Data = BitConverter.GetBytes(index)
        });
        return node;
    }

    void ExportModels(string mdlName, LUtfNode root, string suffix, LUtfNode vms, ImportedModelNode model)
    {
        var modelNode = new LUtfNode() {Parent = root, Name = model.Name + suffix};
        modelNode.Children = new List<LUtfNode>();
        root.Children.Add(modelNode);
        Export3DB(mdlName, modelNode, model, vms);
        foreach (var child in model.Children)
            ExportModels(mdlName, root, suffix, vms, child);
    }

    static void Export3DB(string mdlName, LUtfNode node3db, ImportedModelNode mdl, LUtfNode vmeshlibrary = null)
    {
        var vms = vmeshlibrary ?? new LUtfNode()
            {Name = "VMeshLibrary", Parent = node3db, Children = new List<LUtfNode>()};
        for (int i = 0; i < mdl.LODs.Count; i++)
        {
            var n = new LUtfNode()
            {
                Name = string.Format("{0}-{1}.lod{2}.{3}.vms", mdlName, mdl.Name, i,
                    (int) GeometryWriter.FVF(mdl.LODs[i].Geometry)),
                Parent = vms
            };
            n.Children = new List<LUtfNode>();
            n.Children.Add(new LUtfNode()
                {Name = "VMeshData", Parent = n, Data = GeometryWriter.VMeshData(mdl.LODs[i].Geometry)});
            vms.Children.Add(n);
        }

        if (vmeshlibrary == null)
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
                            (int) GeometryWriter.FVF(mdl.LODs[i].Geometry)))
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
                        (int) GeometryWriter.FVF(mdl.LODs[0].Geometry)))
            });
            node3db.Children.Add(part);
        }
    }
}