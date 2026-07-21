// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using LibreLancer.ContentEdit;
using LibreLancer;
using LibreLancer.Data;
using LibreLancer.Graphics;
using LibreLancer.Render.Materials;
using LibreLancer.Resources;
using LibreLancer.Utf;
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
        public string Reference;
        public uint MaterialId;

        public TextureReference()
        {
        }

        public TextureReference(string name, Texture tex, string reference = null, uint materialId = 0)
        {
            Name = name;
            Reference = reference;
            MaterialId = materialId;
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
		static readonly Dictionary<string, string> materialLibraryHints = new(StringComparer.OrdinalIgnoreCase);

		public static void DetectDrawable(string name, IDrawable drawable, ResourceManager res, List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs, string sourcePath = null)
		{
			if (drawable is CmpFile)
			{
				var cmp = (CmpFile)drawable;
				foreach (var part in cmp.ModelParts()) DetectResourcesModel(part.Model, name + ", " + part.Model.Path, res, missing, matrefs, texrefs, sourcePath);
			}
			if (drawable is ModelFile)
			{
				DetectResourcesModel((ModelFile)drawable, name, res, missing, matrefs, texrefs, sourcePath);
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
		static void DetectResourcesModel(ModelFile mdl, string mdlname, ResourceManager res, List<MissingReference> missing, List<uint> matrefs, List<TextureReference> texrefs, string sourcePath)
        {
            if (mdl.Levels.Length <= 0) return;
			var lvl = mdl.Levels[0];
            var msh = res.FindMesh(lvl.MeshCrc);
			if (msh == null) return;
			for (int i = lvl.StartMesh; i < (lvl.StartMesh + lvl.MeshCount); i++)
            {
                var materialCrc = msh.Meshes[i].MaterialCrc;
                if (materialCrc == 0)
                    continue;
                var mat = res.FindMaterial(materialCrc);
				if (mat == null)
				{
					var str = "Material: 0x" + materialCrc.ToString("X");
					if (!HasMissing(missing, str)) missing.Add(new MissingReference(
						str,
						string.Format("{0}, VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i),
						FindMaterialLibraryHint(sourcePath, materialCrc)));
				}
				else
				{
					if (!matrefs.Contains(materialCrc)) matrefs.Add(materialCrc);
					DoMaterialRefs(mat, res, missing, texrefs, string.Format(" - {0} VMesh(0x{1:X}) #{2}", mdlname, lvl.MeshCrc, i));
				}
			}
		}

		static string FindMaterialLibraryHint(string sourcePath, uint materialCrc)
		{
			if (string.IsNullOrWhiteSpace(sourcePath))
				return null;

			var cacheKey = $"{sourcePath}|{materialCrc:X}";
			if (materialLibraryHints.TryGetValue(cacheKey, out var cached))
				return cached;

			var hint = BuildMaterialLibraryHint(sourcePath, materialCrc);
			materialLibraryHints[cacheKey] = hint;
			return hint;
		}

		static string BuildMaterialLibraryHint(string sourcePath, uint materialCrc)
		{
			try
			{
				if (!File.Exists(sourcePath))
					return null;

				var directory = Path.GetDirectoryName(sourcePath);
				if (string.IsNullOrEmpty(directory))
					return null;

				foreach (var matPath in Directory.EnumerateFiles(directory, "*.mat"))
				{
					if (MatFileContainsMaterial(matPath, materialCrc))
					{
						var rel = ToDataRelativePath(matPath);
						return $"Recommended fix: add material_library = {rel}";
					}
				}

				var expected = Path.ChangeExtension(sourcePath, ".mat");
				var expectedRel = ToDataRelativePath(expected);
				var existsText = File.Exists(expected)
					? "The same-name MAT exists but does not contain this CRC."
					: "No adjacent MAT containing this CRC was found.";
				return $"{existsText} Check material_library. Candidate: material_library = {expectedRel}";
			}
			catch (Exception ex)
			{
				return $"Could not inspect adjacent MAT files: {ex.Message}";
			}
		}

		static bool MatFileContainsMaterial(string matPath, uint materialCrc)
		{
			try
			{
				var utf = new EditableUtf(matPath);
				UtfLoader.LoadResourceNode(utf.Export(), null, out var mat, out _, out _);
				return mat?.Materials.ContainsKey(materialCrc) == true;
			}
			catch
			{
				return false;
			}
		}

		static string ToDataRelativePath(string path)
		{
			var normalized = path.Replace('/', '\\');
			var marker = "\\DATA\\";
			var idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			if (idx >= 0)
				return normalized.Substring(idx + marker.Length);
			return Path.GetFileName(path);
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
                texrefs.Add(new TextureReference(tex, tx, refstr, CrcTool.FLModelCrc(mName)));
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
