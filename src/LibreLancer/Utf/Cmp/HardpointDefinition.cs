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
    public abstract class HardpointDefinition
    {
        public string Name { get; private set; }
        public Matrix4 Orientation;
        public Vector3 Position;

        public HardpointDefinition(IntermediateNode root)
        {
            if (root == null) throw new ArgumentNullException("root");

            Name = root.Name;
			Orientation = Matrix4.Identity;
			Position = Vector3.Zero;
        }
        public HardpointDefinition(string name)
        {
            Name = name;
            Orientation = Matrix4.Identity;
            Position = Vector3.Zero;
        }
        protected bool parentNode(LeafNode node)
        {
			
            switch (node.Name.ToLowerInvariant())
            {
                case "orientation":
                    if (node.MatrixData3x3 != null)
                        Orientation = node.MatrixData3x3.Value;
                    else
                        FLLog.Error("3db", "Hardpoint " + Name + " has garbage orientation, defaulting to identity.");
                    break;
                case "position":
                    if (node.Vector3Data != null)
                        Position = node.Vector3Data.Value;
                    else
                        FLLog.Error("3db", "Hardpoint " + Name + " has garbage position, defaulting to zero.");
                    break;
                default:
                    return false;
            }

            return true;
        }

		public virtual Matrix4 Transform
		{
			get
			{
				return Orientation * Matrix4.CreateTranslation(Position);
			}	
		}
    }
}