// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;


namespace LibreLancer.Utf.Anm
{
    public class AnmFile : UtfFile
    {
        public AnmBuffer Buffer = new AnmBuffer();
        //Optimisation to avoid some copying
        public static void ParseToTable(Dictionary<string, Script> table, AnmBuffer buffer, StringDeduplication strings, Stream stream, string path)
        {
            var anm = new AnmFile() {Buffer = buffer};
            anm.Scripts = table;
            foreach (IntermediateNode node in parseFile(path, stream))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        anm.Load(node, strings, null);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }
        public Dictionary<string, Script> Scripts { get; private set; }

        public AnmFile(string path, Stream stream)
        {
            var table = new StringDeduplication();
            foreach (IntermediateNode node in parseFile(path, stream))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        Load(node, table, null);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }

        public AnmFile()
        {
            Scripts = new Dictionary<string, Script>(StringComparer.OrdinalIgnoreCase);
        }

        public AnmFile(IntermediateNode root, ConstructCollection constructs)
        {
            Load(root, new StringDeduplication(), constructs);
            Buffer.Shrink();
        }
        void Load(IntermediateNode root, StringDeduplication strings, ConstructCollection constructs)
        {
            if(Scripts == null) Scripts = new Dictionary<string, Script>(root.Count, StringComparer.OrdinalIgnoreCase);
            foreach (Node node in root)
            {
                if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (IntermediateNode scNode in (IntermediateNode)node)
                    {
                        Scripts[scNode.Name] = new Script(scNode, Buffer, strings);
                    }
                }
            }
        }
    }
}
