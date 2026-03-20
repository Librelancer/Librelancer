// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Utf.Vms;

namespace LibreLancer.Utf.Mat
{
    public class TxmFile : UtfFile
    {
        public Dictionary<string, TextureData> Textures { get; private set; }
        public Dictionary<string, TexFrameAnimation> Animations { get; private set; }

        public TxmFile()
        {
            Textures = new Dictionary<string, TextureData>();
            Animations = new Dictionary<string, TexFrameAnimation>();
        }

        public TxmFile(IntermediateNode textureLibraryNode)
            : this()
        {
            setTextures(textureLibraryNode);
        }

        private void setTextures(IntermediateNode textureLibraryNode)
        {
            foreach (var textureNode in textureLibraryNode)
            {
                if (textureNode is LeafNode)
                {
                    if (!textureNode.Name.Equals("texture count", StringComparison.OrdinalIgnoreCase))
                        FLLog.Warning("Txm", "Skipping invalid node " + textureNode.Name);
                    continue;
                }

                var imNode = textureNode as IntermediateNode;

                LeafNode? child = null;
                bool isTexture = true;
                bool isTgaMips = false;

                if (imNode!.Count == 1)
                {
                    child = imNode[0] as LeafNode;
                }
                else
                {
                    foreach (var node in imNode)
                    {
                        var n = node.Name.ToLowerInvariant().Trim();

                        if (n == "mip0")
                        {
                            child = node as LeafNode;
                            isTgaMips = true;
                        }

                        if (n == "mips")
                        {
                            child = node as LeafNode;
                            isTgaMips = false;
                            break;
                        }

                        if (n == "mipu")
                        {
                            child = node as LeafNode;
                            isTgaMips = false;
                            break;
                        }

                        if (n == "fps")
                        {
                            isTexture = false;
                            break;
                        }
                    }
                }

                if (isTexture)
                {
                    if (child == null) throw new Exception("Invalid texture library");

                    TextureData data = new TextureData(child, textureNode.Name, isTgaMips);

                    if (isTgaMips)
                    {
                        foreach (var node in imNode)
                            data.SetLevel(node);
                    }

                    if (data == null) throw new Exception("Invalid texture library");

                    var key = textureNode.Name;

                    if (!Textures.TryAdd(key, data))
                    {
                        FLLog.Error("Txm", "Duplicate texture " + key + " in texture library");
                    }
                }
                else
                {
                    Animations.Add(textureNode.Name, new TexFrameAnimation(imNode));
                }
            }
        }
    }
}
