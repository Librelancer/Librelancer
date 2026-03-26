using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.ContentEdit.Texture;
using LibreLancer.Resources;
using LibreLancer.Utf.Mat;
using SimpleMesh;
using Material = SimpleMesh.Material;

namespace LibreLancer.ContentEdit.Model;

public static class MaterialExporter
{
    public static Material GetMaterial(uint crc, ResourceManager resources, Dictionary<string, Material> materials)
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

    public static Dictionary<string, ImageData> ExportImages(ResourceManager resources, Dictionary<string, Material> materials)
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

    static int IndexFromFlags(int flags)
    {
        var sf = (SamplerFlags)flags;
        return ((sf & SamplerFlags.SecondUV) == SamplerFlags.SecondUV) ? 1 : 0;
    }
}
