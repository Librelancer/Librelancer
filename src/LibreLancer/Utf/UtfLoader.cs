// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
namespace LibreLancer.Utf
{
	public class UtfLoader : UtfFile
	{
		public static IDrawable GetDrawable(IntermediateNode root, ILibFile resources)
		{
			bool cmpnd = false;
			bool multilevel = false;
			foreach (var node in root)
			{
				var l = node.Name.ToLowerInvariant();
				if (l == "sphere") return new SphFile(root, resources);
				if (l == "vmeshpart") return new ModelFile(root, resources);
				if (l == "cmpnd") cmpnd = true;
				if (l == "multilevel") multilevel = true;
				if (l == "skeleton") return new DfmFile(root, resources);
			}
			if (cmpnd)
				return new CmpFile(root, resources);
			if (multilevel)
				return new ModelFile(root, resources);
            return null;
		}
		public static IDrawable LoadDrawable(string file, ILibFile resources)
		{
			var root = parseFile(file);
			var dr = GetDrawable(root, resources);
			if (dr is ModelFile) ((ModelFile)dr).Path = file;
			if (dr is CmpFile) ((CmpFile)dr).Path = file;
            if (dr == null)
                FLLog.Error("Utf", file + " is not valid IDrawable");
			return dr;
		}
		public static void LoadResourceNode(IntermediateNode root, ILibFile library, out MatFile materials, out TxmFile textures, out Vms.VmsFile vms)
		{
			materials = null;
			textures = null;
			vms = null;
			foreach (Node node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
					case "material library":
						IntermediateNode materialLibraryNode = node as IntermediateNode;
						materials = new MatFile(materialLibraryNode, library);
						break;
					case "texture library":
						IntermediateNode textureLibraryNode = node as IntermediateNode;
                        try {
                            textures = new TxmFile(textureLibraryNode);
                        }
                        catch (Exception ex) {
                            FLLog.Error("Utf", ex.Message);
                        }
                        break;
					case "vmeshlibrary":
						IntermediateNode vmsnode = node as IntermediateNode;
						vms = new Vms.VmsFile(vmsnode, library);
						break;
				}
			}
		}
		public static void LoadResourceFile(string file, ILibFile library, out MatFile materials, out TxmFile textures, out Vms.VmsFile vms)
		{
			materials = null;
			textures = null;
			vms = null;
			var root = parseFile(file);
			LoadResourceNode(root, library, out materials, out textures, out vms);
		}
	}
}
