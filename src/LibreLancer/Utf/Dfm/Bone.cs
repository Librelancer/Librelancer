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
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Utf.Dfm
{
	public class Bone
	{
		public Matrix4 BoneToRoot { get; private set; }
		public byte LodBits { get; private set; }
		public List<HardpointDefinition> Hardpoints { get; private set; }

		protected Matrix4 transform = Matrix4.Identity;
		public Matrix4 Transform { get { return transform; } }

		public Bone(IntermediateNode node)
		{
			Hardpoints = new List<HardpointDefinition>();

			foreach (Node subNode in node)
			{
				switch (subNode.Name.ToLowerInvariant())
				{
				case "bone to root":
					BoneToRoot = (subNode as LeafNode).MatrixData4x3.Value;
					break;
				case "lod bits":
					LodBits = (subNode as LeafNode).ByteArrayData[0];
					break;
				case "hardpoints":
					IntermediateNode hardpointsNode = subNode as IntermediateNode;
					foreach (IntermediateNode hardpointTypeNode in hardpointsNode)
					{
						switch (hardpointTypeNode.Name.ToLowerInvariant())
						{
						case "fixed":
							foreach (IntermediateNode fixedNode in hardpointTypeNode)
								Hardpoints.Add(new FixedHardpointDefinition(fixedNode));
							break;
						case "revolute":
							foreach (IntermediateNode revoluteNode in hardpointTypeNode)
								Hardpoints.Add(new RevoluteHardpointDefinition(revoluteNode));
							break;
						default: throw new Exception("Invalid node in " + hardpointsNode.Name + ": " + hardpointTypeNode.Name);
						}
					}
					break;
				default: throw new Exception("Invalid node in " + node.Name + ": " + subNode.Name);
				}
			}
		}

		public void Update(Matrix4 world)
		{
			transform = world * BoneToRoot;
		}
	}
}
