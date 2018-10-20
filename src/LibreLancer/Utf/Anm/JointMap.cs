// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
