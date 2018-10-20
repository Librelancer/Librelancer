// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Collections.Generic;
namespace LibreLancer.Infocards
{
	public class Infocard
	{
		public List<InfocardNode> Nodes;

		public string ExtractText()
		{
			var b = new StringBuilder();
			foreach (var n in Nodes)
			{
				if (n is InfocardParagraphNode) b.AppendLine();
				if (n is InfocardTextNode) b.Append((n as InfocardTextNode).Contents);
			}
			return b.ToString();
		}

		public override string ToString()
		{
			return ExtractText();
		}
	}
}

