// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using System.Collections.Generic;
using LibreLancer.Graphics.Text;
using LibreLancer.Interface;
using WattleScript.Interpreter;

namespace LibreLancer.Infocards
{
    [UiLoadable]
    [WattleScriptUserData]
	public class Infocard
	{
		public List<RichTextNode> Nodes;

		public string ExtractText()
		{
			var b = new StringBuilder();
			foreach (var n in Nodes)
			{
				if (n is RichTextParagraphNode) b.AppendLine();
				if (n is RichTextTextNode) b.Append((n as RichTextTextNode).Contents);
			}
			return b.ToString();
		}

		public override string ToString()
		{
			return ExtractText();
		}
	}
}

