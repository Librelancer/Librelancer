// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer.Fx
{
	public class NodeReference
	{
		public FxNode Node;
		public NodeReference Parent;
		//Children - Editor Only
		public List<NodeReference> Children = new List<NodeReference>();
		public List<NodeReference> Paired = new List<NodeReference>();
		public bool IsAttachmentNode; //UTF Flag 1
	}
}
