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
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Dfm
{
	/// <summary>
	/// Represents a UTF Deformable File (.dfm)
	/// </summary>
	public class DfmFile : UtfFile, ILibFile, IDrawable
	{
		private ILibFile additionalLibrary;

		public string Path { get; private set; }

		public MatFile MaterialLibrary { get; private set; }
		public TxmFile TextureLibrary { get; private set; }

		public Dictionary<int, DfmMesh> Levels { get; private set; }
		public float[] Fractions { get; private set; }

		public string Skeleton { get; private set; }
		public float? Scale { get; private set; }

		public Dictionary<int, DfmPart> Parts { get; private set; }
		public Dictionary<string, Bone> Bones { get; private set; }
		public ConstructCollection Constructs { get; private set; }

		public IEnumerable<DfmHardpoint> GetHardpoints()
		{
			foreach (var b in Bones)
			{
				foreach (var hp in b.Value.Hardpoints)
					yield return new DfmHardpoint() { Bone = b.Value, Hp = hp };
			}
		}

		public DfmFile(IntermediateNode root, ILibFile additionalLibrary)
		{
			this.additionalLibrary = additionalLibrary;
			Levels = new Dictionary<int, DfmMesh>();

			Bones = new Dictionary<string, Bone>();
			Parts = new Dictionary<int, DfmPart>();
			Constructs = new ConstructCollection();

			foreach (Node node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
				case "exporter version":
					break;
				case "material library":
					IntermediateNode materialLibraryNode = node as IntermediateNode;
					if (MaterialLibrary == null) MaterialLibrary = new MatFile(materialLibraryNode, this);
					else throw new Exception("Multiple material library nodes in dfm root");
					break;
				case "texture library":
					IntermediateNode textureLibraryNode = node as IntermediateNode;
					if (TextureLibrary == null) TextureLibrary = new TxmFile(textureLibraryNode);
					else throw new Exception("Multiple texture library nodes in dfm root");
					break;
				case "multilevel":
					IntermediateNode multiLevelNode = node as IntermediateNode;
					foreach (Node multiLevelSubNode in multiLevelNode)
					{
						if (multiLevelSubNode.Name.StartsWith("mesh", StringComparison.OrdinalIgnoreCase))
						{
							IntermediateNode meshNode = multiLevelSubNode as IntermediateNode;

							int level = 0;
							if (!int.TryParse(meshNode.Name.Substring(4), out level)) throw new Exception("");
							Levels.Add(level, new DfmMesh(meshNode, this, Parts));
						}
						else if (multiLevelSubNode.Name.Equals("fractions", StringComparison.OrdinalIgnoreCase))
						{
							LeafNode fractionsNode = multiLevelSubNode as LeafNode;
							if (Fractions == null) Fractions = fractionsNode.SingleArrayData;
							else throw new Exception("Multiple fractions nodes in multilevel node");
						}
						else throw new Exception("Invalid node in " + multiLevelNode.Name + ": " + multiLevelSubNode.Name);
					}
					break;
				case "skeleton":
					IntermediateNode skeletonNode = node as IntermediateNode;
					foreach (LeafNode skeletonSubNode in skeletonNode)
					{
						switch (skeletonSubNode.Name.ToLowerInvariant())
						{
						case "name":
							if (Skeleton == null) Skeleton = skeletonSubNode.StringData;
							else throw new Exception("Multiple name nodes in skeleton node");
							break;
						default: throw new Exception("Invalid node in " + skeletonSubNode.Name + ": " + skeletonSubNode.Name);
						}
					}
					break;
				case "cmpnd":
					IntermediateNode cmpndNode = node as IntermediateNode;
					foreach (Node cmpndSubNode in cmpndNode)
					{
						if (cmpndSubNode.Name.Equals("scale", StringComparison.OrdinalIgnoreCase))
						{
							if (Scale == null) Scale = (cmpndSubNode as LeafNode).SingleData;
							else throw new Exception("Multiple scale nodes in cmpnd node");
						}
						else if (cmpndSubNode.Name.Equals("cons", StringComparison.OrdinalIgnoreCase))
						{
							IntermediateNode consNode = cmpndSubNode as IntermediateNode;
							Constructs.AddNode(consNode);
						}
						else if (
							cmpndSubNode.Name.StartsWith("part_", StringComparison.OrdinalIgnoreCase) ||
							cmpndSubNode.Name.Equals("root", StringComparison.OrdinalIgnoreCase)
						)
						{
							IntermediateNode partsNode = cmpndSubNode as IntermediateNode;
							string objectName = string.Empty, fileName = string.Empty;
							int index = -1;

							foreach (LeafNode partNode in partsNode)
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
									index = partNode.Int32Data.Value;
									break;
								default: throw new Exception("Invalid node in " + cmpndSubNode.Name + ": " + partNode.Name);
								}
							}

							Parts.Add(index, new DfmPart(objectName, fileName, Bones, Constructs));
						}
						else throw new Exception("Invalid node in " + node.Name + ": " + cmpndSubNode.Name);
					}
					break;
				default:
					if (node.Name.EndsWith(".3db", StringComparison.OrdinalIgnoreCase))
					{
						Bone b = new Bone(node as IntermediateNode);
						Bones.Add(node.Name, b);
					}
					else throw new Exception("Invalid Node in dfm root: " + node.Name);
					break;
				}
			}
			foreach (var bone in Bones)
			{
				foreach (var construct in Constructs)
				{
					if (bone.Key.StartsWith(construct.ChildName, StringComparison.OrdinalIgnoreCase))
					{
						bone.Value.Construct = construct;
						break;
					}
				}
			}
		}

		public void Initialize(ResourceManager cache)
		{
			if (Levels.ContainsKey (0))
				Levels [0].Initialize (cache);
		}

		public void Resized()
		{
			Levels[0].Resized();
		}


		public void Draw(RenderState rstate, Matrix4 world, Lighting lights)
		{
			Levels [0].Draw (rstate, world, lights);
		}

		public Texture FindTexture(string name)
		{
			//if (TextureLibrary != null)
			//{
				//Texture texture = TextureLibrary.FindTexture(name);
				//if (texture != null) return texture;
			//}
			if (additionalLibrary != null) return additionalLibrary.FindTexture(name);
			return null;

		}

		public Material FindMaterial(uint materialId)
		{
			if (MaterialLibrary != null)
			{
				Material material = MaterialLibrary.FindMaterial(materialId);
				if (material != null) return material;
			}
			if (additionalLibrary != null) return additionalLibrary.FindMaterial(materialId);
			return null;
		}

		public VMeshData FindMesh(uint vMeshLibId)
		{
			return null;
		}

		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
		{
			Levels[0].Update(camera, delta);
		}

		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, Lighting light)
		{
			Levels[0].DrawBuffer(buffer, world, light);		
		}

		public float GetRadius()
		{
			return 20000; //Wrong
		}
	}
}
