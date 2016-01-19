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
    public class Script
    {
        public float RootHeight { get; private set; }
        public List<Channel> Channels { get; private set; }

        public Script(IntermediateNode root, ConstructCollection constructs)
        {
            Channels = new List<Channel>();

            foreach (Node node in root)
            {
                if (node.Name.Equals("root height", StringComparison.OrdinalIgnoreCase)) RootHeight = (node as LeafNode).SingleData.Value;
                else if (
                    node.Name.StartsWith("object map ", StringComparison.OrdinalIgnoreCase) ||
                    node.Name.StartsWith("joint map ", StringComparison.OrdinalIgnoreCase)
                ) Channels.Add(new Channel(node as IntermediateNode, constructs));
                else throw new Exception("Invalid Node in script root: " + node.Name);
            }
        }

		public void Update(TimeSpan delta)
        {
            foreach (Channel channel in Channels) channel.Update(delta);
        }
    }
}
