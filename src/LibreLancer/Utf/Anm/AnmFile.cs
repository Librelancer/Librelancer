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

            foreach (IntermediateNode node in root)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "script":
                        foreach (IntermediateNode scNode in node)
                        {
                            Scripts.Add(scNode.Name, new Script(scNode, constructs));
                        }
                        break;
                    default: throw new Exception("Invalid node in " + root.Name + ": " + node.Name);
                }
            }
        }

        public void Update()
        {
            foreach (KeyValuePair<string, Script> s in Scripts) if (s.Key.StartsWith("sc_rotate", StringComparison.OrdinalIgnoreCase)) s.Value.Update();
        }
    }
}
