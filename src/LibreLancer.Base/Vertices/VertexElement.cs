// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Vertices
{
	public struct VertexElement
	{
		public int Slot;
		public int Elements;
		public VertexElementType Type;
		public bool Normalized;
		public int Offset;

		public VertexElement (int slot, int elems, VertexElementType type, bool normalized, int offset)
		{
			Slot = slot;
			Elements = elems;
			Type = type;
			Normalized = normalized;
			Offset = offset;
		}
	}
}

