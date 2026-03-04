// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Linq;
using LibreLancer.Resources;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
namespace LibreLancer.Utf
{
	public class UtfLoader : UtfFile
	{
		public static IDrawable? GetDrawable(IntermediateNode root, ResourceManager resources, string path = "/")
		{
			var cmpnd = false;
			var model = false;
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
            {
                return new CmpFile(root);
            }

            return model ? new ModelFile(root) : null;

        }
		public static IDrawable? LoadDrawable(Stream file, string filename, ResourceManager resources)
		{
			var root = parseFile(filename, file);
            var dr = GetDrawable(root, resources, filename);
			switch (dr)
            {
                case ModelFile modelFile:
                    modelFile.Path = filename;
                    break;
                case CmpFile cmpFile:
                    cmpFile.Path = filename;
                    break;
                case null:
                    FLLog.Error("Utf", filename + " is not valid IDrawable");
                    break;
            }

            return dr;
		}
		public static bool LoadResourceNode(IntermediateNode root, ResourceManager library, out MatFile? materials, out TxmFile? textures, out Vms.VmsFile? vms)
		{
            materials = null;
            textures = null;
            vms = null;
            try
            {
                foreach (var node in root.OfType<IntermediateNode>())
                {
                    switch (node.Name.ToLowerInvariant())
                    {
                        case "material library":
                            materials = new MatFile(node);
                            break;
                        case "texture library":
                            try
                            {
                                textures = new TxmFile(node);
                            }
                            catch (Exception ex)
                            {
                                FLLog.Error("Utf", ex.Message);
                            }
                            break;
                        case "vmeshlibrary":
                            vms = new Vms.VmsFile(node);
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
		public static void LoadResourceFile(Stream stream, string file, ResourceManager library, out MatFile? materials, out TxmFile? textures, out Vms.VmsFile? vms)
		{
			materials = null;
			textures = null;
			vms = null;
			var root = parseFile(file, stream);
			LoadResourceNode(root, library, out materials, out textures, out vms);
		}
	}
}
