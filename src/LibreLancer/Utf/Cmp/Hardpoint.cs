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
namespace LibreLancer.Utf.Cmp
{
    public abstract class Hardpoint
    {
        public string Name { get; private set; }
        public Matrix4 Orientation { get; private set; }
        public Vector3 Position { get; private set; }

        public Hardpoint(IntermediateNode root)
        {
            if (root == null) throw new ArgumentNullException("root");

            Name = root.Name;
        }

        protected bool parentNode(LeafNode node)
        {
            switch (node.Name.ToLowerInvariant())
            {
                case "orientation":
                    Orientation = node.MatrixData3x3.Value;
                    break;
                case "position":
                    Position = node.Vector3Data.Value;
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}