// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibreLancer.Utf.Anm
{
    public class AnmFile : UtfFile
    {
        public Dictionary<string, Script> Scripts { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
        public AnmBuffer Buffer = new();

        // Optimisation to avoid some copying
        public static void ParseToTable(Dictionary<string, Script> table, AnmBuffer buffer, StringDeduplication strings, Stream stream, string path)
        {
            var anm = new AnmFile {Buffer = buffer,
                Scripts = table
            };

            foreach (IntermediateNode node in parseFile(path, stream).Children.OfType<IntermediateNode>())
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        anm.Load(node, strings);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }


        public AnmFile(string path, Stream stream)
        {
            var table = new StringDeduplication();
            foreach (IntermediateNode node in parseFile(path, stream).Children.OfType<IntermediateNode>())
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        Load(node, table);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }

        public AnmFile()
        {
            Scripts = new Dictionary<string, Script>(StringComparer.OrdinalIgnoreCase);
        }

        public AnmFile(IntermediateNode root)
        {
            Load(root, new StringDeduplication());
            Buffer.Commit();
        }

        private void Load(IntermediateNode root, StringDeduplication strings)
        {
            foreach (var node in root.Children)
            {
                if (!node.Name.Equals("script", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var scNode in ((IntermediateNode)node).Children)
                {
                    Scripts[scNode.Name] = new Script((IntermediateNode)scNode, Buffer, strings);
                }
            }
        }
    }
}
