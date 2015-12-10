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
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;

using OpenTK;


namespace LibreLancer.Utf.Cmp
{
    public class RevoluteHardpoint : Hardpoint
    {
        public Vector3 Axis { get; private set; }
        public float Max { get; private set; }
        public float Min { get; private set; }

        public RevoluteHardpoint(IntermediateNode root)
            : base(root)
        {
            foreach (LeafNode node in root)
            {
                if (!parentNode(node))
                    switch (node.Name.ToLowerInvariant())
                    {
                        case "axis":
                            Axis = node.Vector3Data.Value;
                            break;
                        case "max":
                            Max = node.SingleData.Value;
                            break;
                        case "min":
                            Min = node.SingleData.Value;
                            break;
                        default:
                            throw new Exception("Invalid LeafNode in " + root.Name + ": " + node.Name);
                    }
            }
        }
    }
}