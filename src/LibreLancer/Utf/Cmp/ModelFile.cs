// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;

using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Cmp
{
    /// <summary>
    /// Represents a UTF Model File (.3db)
    /// </summary>
    public class ModelFile : UtfFile, IDrawable, ILibFile
    {
        //private RenderTools.Camera camera;
        private ILibFile additionalLibrary;
        private bool ready;

        public string Path { get; set; }

        public VmsFile VMeshLibrary { get; private set; }
        public MatFile MaterialLibrary { get; private set; }
        public TxmFile TextureLibrary { get; private set; }
		public MaterialAnimCollection MaterialAnim { get; private set; }

        public List<HardpointDefinition> Hardpoints { get; private set; }
        public VMeshRef[] Levels { get; private set; }
        public float[] Switch2 { get; private set; }
        public VMeshWire VMeshWire { get; private set; }

        public ModelFile(string path, ILibFile additionalLibrary)
        {
            Path = path;
            load(parseFile(path), additionalLibrary);
        }

        public ModelFile(IntermediateNode root, ILibFile additionalLibrary)
        {
            Path = root.Name;
            load(root, additionalLibrary);
        }

        private void load(IntermediateNode root, ILibFile additionalLibrary)
        {
            this.additionalLibrary = additionalLibrary;
            ready = false;

            Hardpoints = new List<HardpointDefinition>();
            var lvls = new Dictionary<int, VMeshRef>();

            foreach (Node node in root)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "exporter version":
                        break;
                    case "vmeshlibrary":
                        IntermediateNode vMeshLibraryNode = node as IntermediateNode;
                        if (VMeshLibrary == null) VMeshLibrary = new VmsFile(vMeshLibraryNode, this);
                        else throw new Exception("Multiple vmeshlibrary nodes in 3db root");
                        break;
                    case "material library":
                        IntermediateNode materialLibraryNode = node as IntermediateNode;
                        if (MaterialLibrary == null) MaterialLibrary = new MatFile(materialLibraryNode, this);
                        else throw new Exception("Multiple material library nodes in 3db root");
                        break;
                    case "texture library":
                        IntermediateNode textureLibraryNode = node as IntermediateNode;
                        if (TextureLibrary == null) TextureLibrary = new TxmFile(textureLibraryNode);
                        else throw new Exception("Multiple texture library nodes in 3db root");
                        break;
                    case "hardpoints":
                        IntermediateNode hardpointsNode = node as IntermediateNode;
                        foreach (Node hpn in hardpointsNode)
                        {
							if (hpn is LeafNode)
								continue; //No nodes here
							var hardpointTypeNode = (IntermediateNode)hpn;
                            switch (hardpointTypeNode.Name.ToLowerInvariant())
                            {
                                case "fixed":
                                    foreach (IntermediateNode fixedNode in hardpointTypeNode)
                                        Hardpoints.Add(new FixedHardpointDefinition(fixedNode));
                                    break;
                                case "revolute":
                                    foreach (IntermediateNode revoluteNode in hardpointTypeNode)
                                        Hardpoints.Add(new RevoluteHardpointDefinition(revoluteNode));
                                    break;
                                default:
                                    Hardpoints.Add(new FixedHardpointDefinition(hardpointTypeNode));
                                    break;
                            }
                        }
                        break;
                    case "vmeshpart":
                        {
                            IntermediateNode vMeshPartNode = node as IntermediateNode;
                            if (vMeshPartNode.Count == 1)
                            {
                                LeafNode vMeshRefNode = vMeshPartNode[0] as LeafNode;
                                lvls.Add(0, new VMeshRef(vMeshRefNode.ByteArrayData, this));
                            }
                            else throw new Exception("Invalid VMeshPart: More than one child or zero elements");
                        }
                        break;
                    case "multilevel":
                        IntermediateNode multiLevelNode = node as IntermediateNode;
                        foreach (Node multiLevelSubNode in multiLevelNode)
                        {
                            if (multiLevelSubNode.Name.StartsWith("level", StringComparison.OrdinalIgnoreCase))
                            {
								if (multiLevelSubNode is LeafNode)
									continue;
                                IntermediateNode levelNode = multiLevelSubNode as IntermediateNode;
                                if (levelNode.Count == 1)
                                {
                                    int level = 0;
                                    if (!int.TryParse(levelNode.Name.Substring(5), out level)) throw new Exception("Invalid Level: Missing index");

                                    IntermediateNode vMeshPartNode = levelNode[0] as IntermediateNode;
                                    if (vMeshPartNode.Count == 1)
                                    {
                                        LeafNode vMeshRefNode = vMeshPartNode[0] as LeafNode;
                                        lvls.Add(level, new VMeshRef(vMeshRefNode.ByteArrayData, this));
                                    }
                                    else throw new Exception("Invalid VMeshPart: More than one child or zero elements");
                                }
                                //else throw new Exception("Invalid Level: More than one child or zero elements");
                            }
                            else if (multiLevelSubNode.Name.Equals("switch2", StringComparison.OrdinalIgnoreCase))
                            {
                                LeafNode switch2Node = multiLevelSubNode as LeafNode;
                                Switch2 = switch2Node.SingleArrayData;
                            }
                            else throw new Exception("Invalid node in " + multiLevelNode.Name + ": " + multiLevelSubNode.Name);
                        }
                        break;
                    case "vmeshwire":
                        VMeshWire = new VMeshWire(node as IntermediateNode, this);
                        break;
                    case "mass properties":
                        // TODO 3db Mass Properties
                        break;
                    case "extent tree":
                        // TODO 3db Extent Tree
                        break;
					case "materialanim":
						MaterialAnim = new MaterialAnimCollection((IntermediateNode)node);
						break;
                    default:
                        FLLog.Error("3db", (Path ?? "") + ": Invalid node: " + node.Name);
                        break;
                }
            }
            
            //Sort levels in order
            var lvl2 = new List<VMeshRef>();
            for (int i = 0; i < 100; i++)
            {
                if (lvls.ContainsKey(i))
                {
                    lvl2.Add(lvls[i]);
                }
                else
                {
                    break;
                }
            }
            Levels = lvl2.ToArray();
        }

		public void Initialize(ResourceManager cache)
        {
            for(int i = 0; i < Levels.Length; i++) Levels[i].Initialize(cache);
            ready = Levels.Length > 0;
            if(VMeshWire != null) VMeshWire.Initialize(this);
        }

        public void Resized()
        {
            if (ready)
            {
                Levels[0].Resized();
                //foreach (VMeshRef level in Levels.Values) level.DeviceReset();
            }
        }

		public void Update(ICamera camera, TimeSpan delta, TimeSpan totalTime)
        {
            if (ready)
            {
				if (MaterialAnim != null)
					MaterialAnim.Update((float)totalTime.TotalSeconds);
				//Levels[0].Update(camera, delta);
                for(int i = 0; i < Levels.Length; i++) Levels[i].Update(camera, delta);
            }
        }
		public float GetRadius()
		{
			return Levels[0].Radius;
		}
		public void DrawBuffer(CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
		{
			if (ready)
			{
				var ma = MaterialAnim;
				if (ma == null && additionalLibrary is CmpFile)
					ma = ((CmpFile)additionalLibrary).MaterialAnim;
				Levels[0].DrawBuffer(buffer, world, ref light, ma, overrideMat);
			}
		}

        public void DrawBufferLevel(VMeshRef level, CommandBuffer buffer, Matrix4 world, ref Lighting light, Material overrideMat = null)
        {
            if (ready)
            {
                var ma = MaterialAnim;
                if (ma == null && additionalLibrary is CmpFile)
                    ma = ((CmpFile)additionalLibrary).MaterialAnim;
                level.DrawBuffer(buffer, world, ref light, ma, overrideMat);
            }
        }

		public void DepthPrepassLevel(VMeshRef level, RenderState rstate, Matrix4 world)
		{
			if (ready)
			{
				var ma = MaterialAnim;
				if (ma == null && additionalLibrary is CmpFile)
					ma = ((CmpFile)additionalLibrary).MaterialAnim;
				level.DepthPrepass(rstate, world, ma);
			}
		}

		public void Draw(RenderState rstate, Matrix4 world, Lighting light)
        {
			if (ready) {
				var ma = MaterialAnim;
				if (ma == null && additionalLibrary is CmpFile)
					ma = ((CmpFile)additionalLibrary).MaterialAnim;
				Levels [0].Draw (rstate, world, light, ma);

				/*Matrix tworld = Transform * world;
                float cameraDistance = Vector3.Distance(tworld.Translation, camera.Position);

                for (int i = 0; i < Switch2.Length; i++)
                {
                    if (cameraDistance <= Switch2[i])
                    {
                        if (Levels.ContainsKey(i)) Levels[i].Draw(tworld);
                        break;
                    }
                }*/
			}
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