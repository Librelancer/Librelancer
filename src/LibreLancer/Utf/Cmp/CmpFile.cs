// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibreLancer.Resources;
using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Anm;
using LibreLancer.Utf.Mat;

namespace LibreLancer.Utf.Cmp
{
    /// <summary>
    /// Represents a UTF Compound File (.cmp)
    /// </summary>
    public class CmpFile : UtfFile, IRigidModelFile
    {
        public string? Path { get; set; }

        public VmsFile? VMeshLibrary { get; private set; }
        public AnmFile? Animation { get; set; }
        public MatFile? MaterialLibrary { get; private set; }
        public TxmFile? TextureLibrary { get; private set; }
        public MaterialAnimCollection? MaterialAnim { get; private set; }

        public List<Part> Parts { get; private set; }
        public ConstructCollection Constructs { get; private set; }
        public Dictionary<string, ModelFile> Models { get; private set; }
        public Dictionary<string, CmpCameraInfo> Cameras { get; private set; }

        public CmpFile(string path, Stream stream) : this(parseFile(path, stream))
        {
            Path = path;
        }

        public IEnumerable<Part> ModelParts() => Parts.Where(x => x.Camera == null);

        public Part? GetRootPart()
        {
            return Parts.FirstOrDefault(part => part.ObjectName.Equals("Root", StringComparison.OrdinalIgnoreCase));
        }

        public CmpFile(IntermediateNode rootnode)
        {
            Models = new Dictionary<string, ModelFile>();
            Cameras = new Dictionary<string, CmpCameraInfo>();
            Constructs = [];
            Parts = [];
            List<string> modelNames = [];

            foreach (Node node in rootnode)
            {
                var im = (node as IntermediateNode)!;

                switch (node.Name.ToLowerInvariant())
                {
                    case "exporter version":
                        break;
                    case "vmeshlibrary":
                        VMeshLibrary = VMeshLibrary == null
                            ? new VmsFile(im)
                            : throw new Exception("Multiple vmeshlibrary nodes in cmp root");
                        break;
                    case "animation":
                        Animation = Animation == null
                            ? new AnmFile(im)
                            : throw new Exception("Multiple animation nodes in cmp root");
                        break;
                    case "material library":
                        MaterialLibrary = MaterialLibrary == null
                            ? new MatFile(im)
                            : throw new Exception("Multiple material library nodes in cmp root");
                        break;
                    case "texture library":
                        TextureLibrary = TextureLibrary == null
                            ? new TxmFile(im)
                            : throw new Exception("Multiple texture library nodes in cmp root");
                        break;
                    case "cmpnd":
                        foreach (Node subNode in im)
                        {
                            if (subNode is LeafNode)
                            {
                                continue;
                            }

                            var imSubNode = (subNode as IntermediateNode)!;

                            if (imSubNode.Name.Equals("cons", StringComparison.OrdinalIgnoreCase))
                            {
                                Constructs.AddNode(imSubNode);
                            }
                            else if (
                                imSubNode.Name.StartsWith("part_", StringComparison.OrdinalIgnoreCase) ||
                                imSubNode.Name.Equals("root", StringComparison.OrdinalIgnoreCase)
                            )
                            {
                                string objectName = string.Empty, fileName = string.Empty;

                                foreach (var cmpSubNode in imSubNode)
                                {
                                    var leafSubNode = (LeafNode) cmpSubNode;

                                    switch (leafSubNode.Name.ToLowerInvariant())
                                    {
                                        case "object name":
                                            objectName = leafSubNode.StringData;
                                            break;
                                        case "file name":
                                            fileName = leafSubNode.StringData;
                                            break;
                                        case "index":
                                            break;
                                        default:
                                            FLLog.Error("Cmp",
                                                "Invalid node in " + subNode.Name + ": " + leafSubNode.Name);
                                            break;
                                    }
                                }

                                Parts.Add(new Part(objectName, fileName, Models, Cameras, Constructs));
                            }
                            else throw new Exception("Invalid node in " + im.Name + ": " + subNode.Name);
                        }

                        break;
                    case "materialanim":
                        MaterialAnim = new MaterialAnimCollection(im);
                        break;
                    default:
                        if ((IntermediateNode?) im is null)
                        {
                            var m = new ModelFile(new IntermediateNode(node.Name, []))
                            {
                                Path = node.Name
                            };

                            Models.Add(node.Name, m);
                            modelNames.Add(node.Name);
                            FLLog.Warning("Cmp",
                                Path ?? "Utf" + ": Invalid Node in cmp root, assuming empty: " + node.Name);
                            break;
                        }

                        if (im.Any(x => x.Name.Equals("camera", StringComparison.OrdinalIgnoreCase)))
                        {
                            var cam = new CmpCameraInfo(im);
                            Cameras.Add(im.Name, cam);
                        }
                        else
                        {
                            ModelFile m = new ModelFile(im)
                            {
                                Path = im.Name
                            };
                            Models.Add(im.Name, m);
                            modelNames.Add(im.Name);
                        }

                        break;
                }
            }

            // FL handles cmpnd nodes that point to non-existant models: fix up here
            List<Part> broken = [];
            broken.AddRange(Parts.Where(t => t.IsBroken()));

            foreach (var b in broken)
            {
                Parts.Remove(b);
            }
        }

        public RigidModel CreateRigidModel(bool drawable, ResourceManager resources)
        {
            var mdl = new RigidModel { Path = Path, Source = RigidModelSource.Compound, Parts = new() };
            List<RigidModelPart> allParts = [];

            foreach (var p in Parts)
            {
                if (p.Camera != null)
                {
                    continue;
                }

                var mdlPart = p.Model!.CreatePart(drawable, resources, p.ObjectName, p.FileName);

                if (p.Construct != null)
                {
                    mdlPart.Construct = p.Construct.Clone();
                }

                mdlPart.Children = [];
                allParts.Add(mdlPart);
            }

            foreach (var p in allParts)
            {
                mdl.Parts.Add(p);

                if (p.Construct != null)
                {
                    var parent = allParts.First(x =>
                        x.Name!.Equals(p.Construct.ParentName, StringComparison.OrdinalIgnoreCase));
                    parent.Children!.Add(p);
                }
                else
                    mdl.Root = p;
            }

            mdl.AllParts = allParts.ToArray();
            mdl.MaterialAnims = MaterialAnim;
            mdl.Animation = Animation;
            mdl.UpdateTransform();
            return mdl;
        }

        public void ClearResources()
        {
            MaterialLibrary = null;
            TextureLibrary = null;
            VMeshLibrary = null;
        }

        public override string ToString()
        {
            return Path ?? "Empty";
        }
    }
}
