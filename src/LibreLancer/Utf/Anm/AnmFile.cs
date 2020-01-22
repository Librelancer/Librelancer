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
        //Optimisation to avoid some copying
        public static void ParseToTable(Dictionary<string, Script> table, string path)
        {
            var anm = new AnmFile();
            anm.Scripts = table;
            foreach (IntermediateNode node in parseFile(path))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        anm.Load(node, null);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }
        public Dictionary<string, Script> Scripts { get; private set; }

        public AnmFile(string path)
        {
            foreach (IntermediateNode node in parseFile(path))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        Load(node, null);
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
            Load(root, constructs);
        }
        public AnmFile(Stream stream)
        {
            var utf = parseFile("stream", stream);
            Load(utf, null);
        }
        void Load(IntermediateNode root, ConstructCollection constructs)
        {
            if(Scripts == null) Scripts = new Dictionary<string, Script>(root.Count, StringComparer.OrdinalIgnoreCase);
            foreach (Node node in root)
            {
                if (node.Name.Equals("script", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (IntermediateNode scNode in (IntermediateNode)node)
                    {
                        Scripts[scNode.Name] = new Script(scNode, constructs);
                    }
                }
            }
        }
    }
}
