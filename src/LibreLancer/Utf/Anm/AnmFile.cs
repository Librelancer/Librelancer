// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;


namespace LibreLancer.Utf.Anm
{
    public class AnmFile : UtfFile
    {
        public Dictionary<string, Script> Scripts { get; private set; }

        public AnmFile(string path)
        {
            foreach (IntermediateNode node in parseFile(path))
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "animation":
                        load(node, null);
                        break;
                    default: throw new Exception("Invalid Node in anm root: " + node.Name);
                }
            }
        }

        public AnmFile(IntermediateNode root, ConstructCollection constructs)
        {
            load(root, constructs);
        }

        private void load(IntermediateNode root, ConstructCollection constructs)
        {
            Scripts = new Dictionary<string, Script>();

			foreach (Node node in root)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "script":
						foreach (IntermediateNode scNode in (IntermediateNode)node)
                        {
                            Scripts.Add(scNode.Name, new Script(scNode, constructs));
                        }
                        break;
					case "anim_credits":
						//TODO: What is this?
						break;
                    default: throw new Exception("Invalid node in " + root.Name + ": " + node.Name);
                }
            }
        }
    }
}
