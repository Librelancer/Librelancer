/* The contents of this file a
 * re subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Mat;
//using .Universe;

namespace LibreLancer.Utf.Cmp
{
    /// <summary>
    /// Represents a UTF Compound File (.cmp)
    /// </summary>
    public class CmpFile : UtfFile, IDrawable, ILibFile
    {
        private ILibFile additionalLibrary;

        public string Path { get; set; }

        public VmsFile VMeshLibrary { get; private set; }
        public AnmFile Animation { get; private set; }
        public MatFile MaterialLibrary { get; private set; }
        public TxmFile TextureLibrary { get; private set; }
		public MaterialAnimCollection MaterialAnim { get; private set; }

		public Dictionary<int, Part> Parts { get; private set; }
        public ConstructCollection Constructs { get; private set; }
        public Dictionary<string, ModelFile> Models { get; private set; }

		public CmpFile(string path, ILibFile additionalLibrary) : this(parseFile(path), additionalLibrary)
		{
			Path = path;
		}

        public CmpFile(IntermediateNode rootnode, ILibFile additionalLibrary)
        {
            this.additionalLibrary = additionalLibrary;

            Models = new Dictionary<string, ModelFile>();
            Constructs = new ConstructCollection();
            Parts = new Dictionary<int, Part>();

			foreach (Node node in rootnode)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "exporter version":
                        break;
                    case "vmeshlibrary":
                        IntermediateNode vMeshLibraryNode = node as IntermediateNode;
                        if (VMeshLibrary == null) VMeshLibrary = new VmsFile(vMeshLibraryNode, this);
                        else throw new Exception("Multiple vmeshlibrary nodes in cmp root");
                        break;
                    case "animation":
                        IntermediateNode animationNode = node as IntermediateNode;
                        if (Animation == null) Animation = new AnmFile(animationNode, Constructs);
                        else throw new Exception("Multiple animation nodes in cmp root");
                        break;
                    case "material library":
                        IntermediateNode materialLibraryNode = node as IntermediateNode;
                        if (MaterialLibrary == null) MaterialLibrary = new MatFile(materialLibraryNode, this);
                        else throw new Exception("Multiple material library nodes in cmp root");
                        break;
                    case "texture library":
                        IntermediateNode textureLibraryNode = node as IntermediateNode;
                        if (TextureLibrary == null) TextureLibrary = new TxmFile(textureLibraryNode);
                        else throw new Exception("Multiple texture library nodes in cmp root");
                        break;
                    case "cmpnd":
                        IntermediateNode cmpndNode = node as IntermediateNode;
						int maxIndices = int.MaxValue;
                        foreach (Node SubNode in cmpndNode)
                        {
							if (SubNode is LeafNode) continue;
							var cmpndSubNode = (IntermediateNode)SubNode;
                            if (cmpndSubNode.Name.Equals("cons", StringComparison.OrdinalIgnoreCase))
                            {
                                Constructs.AddNode(cmpndSubNode);
                            }
                            else if (
                                cmpndSubNode.Name.StartsWith("part_", StringComparison.OrdinalIgnoreCase) ||
                                cmpndSubNode.Name.Equals("root", StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                string objectName = string.Empty, fileName = string.Empty;
                                int index = -1;

                                foreach (LeafNode partNode in cmpndSubNode)
                                {
                                    switch (partNode.Name.ToLowerInvariant())
                                    {
                                        case "object name":
                                            objectName = partNode.StringData;
                                            break;
                                        case "file name":
                                            fileName = partNode.StringData;
                                            break;
										case "index":
											if (partNode.Int32Data != null)
												index = partNode.Int32Data.Value;
											else
												index = partNode.Int32ArrayData [0];
                                            break;
                                        default: throw new Exception("Invalid node in " + cmpndSubNode.Name + ": " + partNode.Name);
                                    }
                                }
								if (Parts.ContainsKey(index))
								{
									FLLog.Error("Cmp", "Duplicate index");
									Parts.Add(maxIndices--, new Part(objectName, fileName, Models, Constructs));
								}
								else
                                	Parts.Add(index, new Part(objectName, fileName, Models, Constructs));
                            }
                            else throw new Exception("Invalid node in " + cmpndNode.Name + ": " + cmpndSubNode.Name);
                        }
                        break;
                    case "materialanim":
						MaterialAnim = new MaterialAnimCollection((IntermediateNode)node);
                        break;
                    default:
                        if (node.Name.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelFile m = new ModelFile(node as IntermediateNode, this);
							m.Path = node.Name;
                            Models.Add(node.Name, m);
                        }
                        else FLLog.Error("Cmp", Path ?? "Utf" + ": Invalid Node in cmp root: " + node.Name);
                        break;
                }
            }
        }

		public void Initialize(ResourceManager cache)
        {
            foreach (var part in Parts.Values) part.Initialize(cache);
        }

        public void Resized()
        {
            for (int i = 0; i < Parts.Count; i++) Parts[i].Resized();
        }

		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
		{
			if (MaterialAnim != null)
				MaterialAnim.Update((float)totalTime.TotalSeconds);
            foreach (var part in Parts.Values) part.Update(camera, delta, totalTime);
        }

		public float GetRadius()
		{
			float max = float.MinValue;
			foreach (var part in Parts.Values)
			{
				var r = part.Model.GetRadius();
				float d = 0;
				if(part.Construct != null)
					d = part.Construct.Transform.Transform(part.Model.Levels[0].Center).Length;
				max = Math.Max(max, r + d);
			}
			return max;
		}
		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
		{
			for (int i = 0; i < Parts.Count; i++) Parts[i].DrawBuffer(buffer, world, ref light, overrideMat);
		}
		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
            for (int i = 0; i < Parts.Count; i++) Parts[i].Draw(rstate, world, light);
        }

        public Texture FindTexture(string name)
        {
           	return additionalLibrary.FindTexture(name);
        }

        public Material FindMaterial(uint materialId)
        {
           	return additionalLibrary.FindMaterial(materialId);
        }

        public VMeshData FindMesh(uint vMeshLibId)
        {
            if (VMeshLibrary != null)
            {
                VMeshData mesh = VMeshLibrary.FindMesh(vMeshLibId);
                if (mesh != null) return mesh;
            }
            if (additionalLibrary != null) return additionalLibrary.FindMesh(vMeshLibId);
            return null;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}