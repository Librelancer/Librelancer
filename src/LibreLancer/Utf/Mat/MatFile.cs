// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Mat
{
	public class MatFile : UtfFile
    {

        public Dictionary<uint, Material> Materials { get; private set; } = new ();

		public TxmFile TextureLibrary { get; private set; }

		public MatFile ()
		{
		}

		public MatFile (IntermediateNode materialLibraryNode)
		{
			setMaterials (materialLibraryNode);
		}

		private void setMaterials (IntermediateNode materialLibraryNode)
		{
			//TODO: int count = 0;
			foreach (Node materialNode in materialLibraryNode) {
				if (materialNode is IntermediateNode) {
					uint materialId = CrcTool.FLModelCrc (materialNode.Name);
                    if (!Materials.ContainsKey(materialId))
                    {
                        try
                        {
                            var mat = Material.FromNode(materialNode as IntermediateNode);
                            Materials.Add (materialId, Material.FromNode (materialNode as IntermediateNode));
                        }
                        catch (Exception e)
                        {
                            FLLog.Error("Mat", $"Error loading material {materialNode.Name}: {e}");
                        }
                    }
				}
            }
        }
	}
}
