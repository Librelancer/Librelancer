/* The contents of this file are subject to the Mozilla Public License
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
 * Portions created by the Initial Developer are Copyright (C) 2011, 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;

//using FLCommon;
using OpenTK;

//using FLParser.Utf;

using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Vms;
//using FLApi.Universe;

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

        public string Name { get; private set; }

        public VmsFile VMeshLibrary { get; private set; }
        public MatFile MaterialLibrary { get; private set; }
        public TxmFile TextureLibrary { get; private set; }

        public List<Hardpoint> Hardpoints { get; private set; }
        public Dictionary<int, VMeshRef> Levels { get; private set; }
        public float[] Switch2 { get; private set; }

        public ModelFile(string path, ILibFile additionalLibrary)
        {
            Name = path;
            load(parseFile(path), additionalLibrary);
        }

        public ModelFile(IntermediateNode root, ILibFile additionalLibrary)
        {
            Name = root.Name;
            load(root, additionalLibrary);
        }

        private void load(IntermediateNode root, ILibFile additionalLibrary)
        {
            this.additionalLibrary = additionalLibrary;
            ready = false;

            Hardpoints = new List<Hardpoint>();
            Levels = new Dictionary<int, VMeshRef>();

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
                        foreach (IntermediateNode hardpointTypeNode in hardpointsNode)
                        {
                            switch (hardpointTypeNode.Name.ToLowerInvariant())
                            {
                                case "fixed":
                                    foreach (IntermediateNode fixedNode in hardpointTypeNode)
                                        Hardpoints.Add(new FixedHardpoint(fixedNode));
                                    break;
                                case "revolute":
                                    foreach (IntermediateNode revoluteNode in hardpointTypeNode)
                                        Hardpoints.Add(new RevoluteHardpoint(revoluteNode));
                                    break;
                                default: throw new Exception("Invalid node in " + hardpointsNode.Name + ": " + hardpointTypeNode.Name);
                            }
                        }
                        break;
                    case "vmeshpart":
                        {
                            IntermediateNode vMeshPartNode = node as IntermediateNode;
                            if (vMeshPartNode.Count == 1)
                            {
                                LeafNode vMeshRefNode = vMeshPartNode[0] as LeafNode;
                                Levels.Add(0, new VMeshRef(vMeshRefNode.ByteArrayData, this));
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
                                IntermediateNode levelNode = multiLevelSubNode as IntermediateNode;
                                if (levelNode.Count == 1)
                                {
                                    int level = 0;
                                    if (!int.TryParse(levelNode.Name.Substring(5), out level)) throw new Exception("Invalid Level: Missing index");

                                    IntermediateNode vMeshPartNode = levelNode[0] as IntermediateNode;
                                    if (vMeshPartNode.Count == 1)
                                    {
                                        LeafNode vMeshRefNode = vMeshPartNode[0] as LeafNode;
                                        Levels.Add(level, new VMeshRef(vMeshRefNode.ByteArrayData, this));
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
                        // TODO 3db VMeshWire
                        break;
                    case "mass properties":
                        // TODO 3db Mass Properties
                        break;
                    case "extent tree":
                        // TODO 3db Extent Tree
                        break;
                    default: throw new Exception("Invalid node in 3db root: " + node.Name);
                }
            }
        }

		public void Initialize(ResourceCache cache)
        {
            //this.camera = camera;

			if (Levels.ContainsKey (0))
				Levels [0].Initialize (cache);
            //foreach (VMeshRef level in Levels.Values) level.Initialize(device, content, camera, ambient, lights);

            ready = Levels.ContainsKey(0);
        }

        public void Resized()
        {
            if (ready)
            {
                Levels[0].Resized();
                //foreach (VMeshRef level in Levels.Values) level.DeviceReset();
            }
        }

        public void Update(Camera camera)
        {
            if (ready)
            {
                Levels[0].Update(camera);
                //foreach (VMeshRef level in Levels.Values) level.Update();
            }
        }

		public void Draw(Matrix4 world, Lighting light)
        {
            if (ready)
            {
                Levels[0].Draw(world, light);

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

        public TextureData FindTexture(string name)
        {
            if (TextureLibrary != null)
            {
                TextureData texture = TextureLibrary.FindTexture(name);
                if (texture != null) return texture;
            }
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
            return Name;
        }
    }
}