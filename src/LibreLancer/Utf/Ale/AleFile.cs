// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Utf.Ale
{
    public class AleFile : UtfFile
    {
        public ALEffectLib FxLib = null!;
        public AlchemyNodeLibrary NodeLib = null!;
        public string Path;

        public AleFile(string file, Stream stream) : this(parseFile(file, stream))
        {
            Path = file;
        }

        public AleFile(IntermediateNode root)
        {
            // TODO: This is ugly
            foreach (var node in root.Children)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "aleffectlib":
                        FxLib = new ALEffectLib(((node as IntermediateNode)!.Children[0] as LeafNode)!);
                        break;
                    case "alchemynodelibrary":
                        NodeLib = new AlchemyNodeLibrary(((node as IntermediateNode)!.Children[0] as LeafNode)!);
                        break;
                    default:
                        throw new NotImplementedException(node.Name);
                }
            }

            Path = "[utf]";
        }
    }
}
