// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
namespace LibreLancer.Utf
{
	public class UtfLoader : UtfFile
	{
		public static IDrawable GetDrawable(IntermediateNode root, ResourceManager resources, string path = "/")
		{
			bool cmpnd = false;
			bool model = false;
			foreach (var node in root)
			{
                switch (node.Name.ToLowerInvariant())
                {
                    case "sphere":
                        return new SphFile(root, resources, path);
                    case "vmeshpart":
                        return new ModelFile(root);
                    case "cmpnd":
                        cmpnd = true;
                        break;
                    case "multilevel":
                    case "hardpoints":
                        model = true;
                        break;
                    case "skeleton":
                        return new DfmFile(root);
                }
			}
			if (cmpnd)
				return new CmpFile(root);
			if (model)
				return new ModelFile(root);
            return null;
		}
		public static IDrawable LoadDrawable(Stream file, string filename, ResourceManager resources)
		{
			var root = parseFile(filename, file);
            var dr = GetDrawable(root, resources, filename);
			if (dr is ModelFile) ((ModelFile)dr).Path = filename;
			if (dr is CmpFile) ((CmpFile)dr).Path = filename;
            if (dr == null)
                FLLog.Error("Utf", filename + " is not valid IDrawable");
			return dr;
		}
		public static bool LoadResourceNode(IntermediateNode root, ResourceManager library, out MatFile materials, out TxmFile textures, out Vms.VmsFile vms)
		{
            materials = null;
            textures = null;
            vms = null;
            try
            {
                foreach (Node node in root)
                {
                    switch (node.Name.ToLowerInvariant())
                    {
                        case "material library":
                            IntermediateNode materialLibraryNode = node as IntermediateNode;
                            materials = new MatFile(materialLibraryNode);
                            break;
                        case "texture library":
                            IntermediateNode textureLibraryNode = node as IntermediateNode;
                            try
                            {
                                textures = new TxmFile(textureLibraryNode);
                            }
                            catch (Exception ex)
                            {
                                FLLog.Error("Utf", ex.Message);
                            }
                            break;
                        case "vmeshlibrary":
                            IntermediateNode vmsnode = node as IntermediateNode;
                            vms = new Vms.VmsFile(vmsnode);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                FLLog.Error("Resources", "Error loading resources: " + ex.Message + "\n" + ex.StackTrace);
                materials = null;
                textures = null;
                vms = null;
                return false;
            }
            return true;
		}
		public static void LoadResourceFile(Stream stream, string file, ResourceManager library, out MatFile materials, out TxmFile textures, out Vms.VmsFile vms)
		{
			materials = null;
			textures = null;
			vms = null;
			var root = parseFile(file, stream);
			LoadResourceNode(root, library, out materials, out textures, out vms);
		}
	}
}
