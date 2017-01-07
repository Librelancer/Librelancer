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
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
namespace LibreLancer.Utf.Anm
{
	public class JointMap
	{
		public string ParentName;
		public string ChildName;
		public Channel Channel;
		public JointMap(IntermediateNode root)
		{
			foreach (Node node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
					case "parent name":
						if (ParentName == null) ParentName = (node as LeafNode).StringData;
						else throw new Exception("Multiple parent name nodes in channel root");
						break;
					case "child name":
						if (ChildName == null) ChildName = (node as LeafNode).StringData;
						else throw new Exception("Multiple child name nodes in channel root");
						break;
					case "channel":
						if (Channel == null) Channel = new Channel((node as IntermediateNode), false);
						else throw new Exception("Multiple data nodes in channel root");
						break;
				}
			}
		}
	}
}
