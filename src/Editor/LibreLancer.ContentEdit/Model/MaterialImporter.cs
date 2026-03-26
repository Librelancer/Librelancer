using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SimpleMesh;

namespace LibreLancer.ContentEdit.Model;

public static class MaterialImporter
{
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

    public static void GenerateResourceLibraries(
        IEnumerable<Material> materials,
        Dictionary<string, ImageData> images,
        LUtfNode root,
        List<EditMessage> warnings,
        bool generatePlaceholderTextures,
        bool importTextures,
        bool advancedMaterials,
        bool multithreaded)
    {
        var tasks = new TaskHandler(multithreaded);

        var mats = new LUtfNode() { Name = "material library", Parent = root };
        mats.Children = new List<LUtfNode>();

        var txms = new LUtfNode() { Name = "texture library", Parent = root };
        txms.Children = new List<LUtfNode>();
        HashSet<string> createdTextures = new HashSet<string>();
        foreach (var mat in materials)
        {
            var dt = mat.DiffuseTexture?.Name ?? (generatePlaceholderTextures ? mat.Name : null);
            GenerateTexture(dt, warnings, createdTextures, txms, images, importTextures, generatePlaceholderTextures, DDSFormat.DXT5, tasks);
            GenerateTexture(mat.EmissiveTexture?.Name, warnings, createdTextures,  txms, images, importTextures, generatePlaceholderTextures, DDSFormat.DXT1, tasks);
            if (advancedMaterials)
            {
                GenerateTexture(mat.NormalTexture?.Name, warnings, createdTextures, txms, images, importTextures, generatePlaceholderTextures, DDSFormat.RGTC2, tasks);
                GenerateMetallicRoughnessTexture(mat.MetallicRoughnessTexture?.Name, warnings, createdTextures, txms, images, importTextures, tasks);
            }
        }

        int i = 0;
        foreach (var mat in materials)
            mats.Children.Add(DefaultMaterialNode(mats,mat, generatePlaceholderTextures, advancedMaterials));

        if (mats.Children.Count > 0)
        {
            mats.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            root.Children.Add(mats);
        }
        if (txms.Children.Count > 0)
        {
            txms.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            root.Children.Add(txms);
        }

        tasks.Wait();
    }

    static void GenerateMetallicRoughnessTexture(
        string texture,
        List<EditMessage> warnings,
        HashSet<string> createdTextures,
        LUtfNode txms,
        Dictionary<string,ImageData> images,
        bool importTextures,
        TaskHandler tasks)
    {
        if (string.IsNullOrWhiteSpace(texture)) return;
        if (!createdTextures.Add(texture)) return;
        if (importTextures && images.TryGetValue(texture, out var img))
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

    static void GenerateTexture(
        string texture,
        List<EditMessage> warnings,
        HashSet<string> createdTextures,
        LUtfNode txms,
        Dictionary<string,ImageData> images,
        bool importTextures,
        bool generatePlaceholders,
        DDSFormat format,
        TaskHandler tasks)
    {
        if (string.IsNullOrWhiteSpace(texture)) return;
        if (!createdTextures.Add(texture)) return;
        if (importTextures && images.TryGetValue(texture, out var img))
        {
            var result = ImportTextureNode(txms, texture, img.Data, format, tasks);
            if(result.IsSuccess)
                txms.Children.Add(result.Data);
            else
                warnings.Add(EditMessage.Warning($"{texture} not imported"));
            warnings.AddRange(result.Messages.Select(x => EditMessage.Warning(x.Message)));
        }
        else if (generatePlaceholders)
        {
            txms.Children.Add(DefaultTextureNode(txms, texture));
        }
    }

    static LUtfNode DefaultMaterialNode(LUtfNode parent, Material mat, bool generatePlaceholders, bool advancedMaterials)
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
        if (generatePlaceholders || !string.IsNullOrWhiteSpace(mat.DiffuseTexture?.Name))
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
        if (!string.IsNullOrWhiteSpace(mat.NormalTexture?.Name) && advancedMaterials)
        {
            string textureName = mat.NormalTexture.Name + ".dds";
            matnode.Children.Add(LUtfNode.StringNode(matnode, "Nm_name", textureName));
            matnode.Children.Add(LUtfNode.IntNode(matnode, "Nm_flags", 64));
        }
        if (mat.MetallicRoughness && advancedMaterials)
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

    static EditResult<LUtfNode> ImportTextureNode(
        LUtfNode parent, string name,
        ReadOnlySpan<byte> data,
        DDSFormat format,
        TaskHandler tasks)
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

    static LUtfNode DefaultTextureNode(
        LUtfNode parent,
        string name)
    {
        var texnode = new LUtfNode() { Name = name + ".dds", Parent = parent };
        texnode.Children = new List<LUtfNode>();
        var d = new byte[DefaultTexture.Data.Length];
        Buffer.BlockCopy(DefaultTexture.Data, 0, d, 0, DefaultTexture.Data.Length);
        texnode.Children.Add(new LUtfNode() { Name = "MIPS", Parent = texnode, Data = d });
        return texnode;
    }
}
