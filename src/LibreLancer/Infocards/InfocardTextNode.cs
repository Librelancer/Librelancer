// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Infocards
{
	public class InfocardTextNode : InfocardNode
	{
		public bool Bold;
		public bool Italic;
		public bool Underline;
		public int FontIndex;
		public Color4 Color = Color4.White;
		public TextAlignment Alignment;
		public string Contents;
	}
}

