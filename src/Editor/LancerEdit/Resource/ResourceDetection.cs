// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Net;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Mat;
namespace LancerEdit
{
    public class TextureReference
    {
        public string Name;
        public bool Found;
        public int Width;
        public int Height;

        public TextureReference()
        {
        }

        public TextureReference(string name, Texture tex)
        {
            Name = name;
            Found = tex != null;
            if (tex is Texture2D t2d)
            {
                Width = t2d.Width;
                Height = t2d.Height;
            }
            if (tex is TextureCube tcb)
            {
                Width = Height = tcb.Size;
            }
        }
    }
	static class ResourceDetection
	{
		public static void DetectDrawable(string name, IDrawable drawable, ResourceManager res, List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs)
		{
			if (drawable is CmpFile)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.ModelParts()) DetectResourcesModel(part.Model, name + ", " + part.Model.Path, res, missing, matrefs, texrefs);
			}
			if (drawable is ModelFile)
			{
				DetectResourcesModel((ModelFile)drawable, name, res, missing, matrefs, texrefs);
			}
			if (drawable is SphFile)
			{
				var sph = (SphFile)drawable;
				for (int i = 0; i < sph.SideMaterials.Length; i++)
				{
                    if (sph.SideMaterials[i] != null && !sph.SideMaterials[i].Loaded) sph.SideMaterials[i] = null;
					if (sph.SideMaterials[i] == null)
					{
						var str = "Material: " + sph.SideMaterialNames[i];
						if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, string.Format("{0} M{1}", name, i)));
					}
					else
					{
						var crc = CrcTool.FLModelCrc(sph.SideMaterialNames[i]);
						if (!matrefs.Contains(crc)) matrefs.Add(crc);
						DoMaterialRefs(sph.SideMaterials[i], res, missing, texrefs, string.Format(" - {0} M{1}", name, i));
					}
				}
			}
		}
		static void DetectResourcesModel(ModelFile mdl, string mdlname, ResourceManager res, List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs)
        {
            if (mdl.Levels.Length <= 0) return;
			var lvl = mdl.Levels[0];
            var msh = res.FindMesh(lvl.MeshCrc);
            if (msh == null) return;
			for (int i = lvl.StartMesh; i < (lvl.StartMesh + lvl.MeshCount); i++)
            {
                var mat = res.FindMaterial(msh.Meshes[i].MaterialCrc);
				if (mat == null)
				{
					var str = "Material: 0x" + msh.Meshes[i].MaterialCrc.ToString("X");
					if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, string.Format("{0}, VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i)));
				}
				else
				{
					if (!matrefs.Contains(msh.Meshes[i].MaterialCrc)) matrefs.Add(msh.Meshes[i].MaterialCrc);
					DoMaterialRefs(mat, res, missing, texrefs, string.Format(" - {0} VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i));
				}
			}
		}

		static void DoMaterialRefs(Material m, ResourceManager res, List<MissingReference> missing, List<TextureReference> texrefs, string refstr)
		{
			RefTex(m.DtName, res, missing, texrefs, m.Name, refstr);
			if (m.Render is NomadMaterial)
			{
				var nt = m.NtName ?? "NomadRGB1_NomadAlpha1";
				RefTex(nt, res, missing, texrefs, m.Name, refstr);
			}
			RefTex(m.EtName, res, missing, texrefs, m.Name, refstr);
			RefTex(m.DmName, res, missing, texrefs, m.Name, refstr);
            RefTex(m.Dm0Name, res, missing, texrefs, m.Name, refstr);
			RefTex(m.Dm1Name, res, missing, texrefs, m.Name, refstr);
		}

		static void RefTex(string tex, ResourceManager res, List<MissingReference> missing, List<TextureReference> texrefs, string mName, string refstr)
		{
			if (tex != null)
            {
                if (HasTexture(texrefs, tex)) return;
                var tx = res.FindTexture(tex);
                texrefs.Add(new TextureReference(tex, tx));
                if (tx == null)
				{
					var str = "Texture: " + tex;
					if (!HasMissing(missing, str)) missing.Add(new MissingReference(str, mName + refstr));
				}
			}
		}

		public static bool HasTexture(List<TextureReference> refs, string item)
		{
			foreach (var tex in refs)
				if (tex.Name.Equals(item, StringComparison.InvariantCultureIgnoreCase)) return true;
			return false;
		}
		public static bool HasMissing(List<MissingReference> missing, string item)
		{
			foreach (var m in missing)
				if (m.Missing == item) return true;
			return false;
		}
	}
}
