// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FxNode
	{
		public string NodeName = "LIBRELANCER:UNNAMED_NODE";
		public uint CRC;
		public float NodeLifeSpan = float.MaxValue;
		public AlchemyTransform Transform;

		public FxNode(AlchemyNode ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter (AleProperty.Node_Name, out temp)) {
				NodeName = (string)temp.Value;
				CRC = CrcTool.FLAleCrc(NodeName);
			}
			if (ale.TryGetParameter (AleProperty.Node_Transform, out temp)) {
				Transform = (AlchemyTransform)temp.Value;
			} else {
				Transform = new AlchemyTransform ();
			}
			if (ale.TryGetParameter (AleProperty.Node_LifeSpan, out temp)) {
				NodeLifeSpan = (float)temp.Value;
			}
		}

        protected static Matrix4x4 GetAttachment(NodeReference reference, Matrix4x4 attachment)
		{
			if (reference.IsAttachmentNode) {
                return attachment;
			}
			else if (reference.Parent == null) {
				return Matrix4x4.Identity;
			}
			else {
                return GetAttachment(reference.Parent, attachment);
			}
		}

        public virtual AlchemyNode SerializeNode()
        {
            var n = new AlchemyNode { ClassName = GetType().Name };
            n.Parameters.Add(new(AleProperty.Node_Name, NodeName));
            n.Parameters.Add(new(AleProperty.Node_Transform, Transform));
            if(NodeLifeSpan <= 3.4e36f)
                n.Parameters.Add(new(AleProperty.Node_LifeSpan, NodeLifeSpan));
            return n;
        }

        public override string ToString()
        {
            return NodeName;
        }

        public FxNode(string nodename)
		{
			NodeName = nodename;
            CRC = CrcTool.FLAleCrc(nodename);
        }
	}
}

