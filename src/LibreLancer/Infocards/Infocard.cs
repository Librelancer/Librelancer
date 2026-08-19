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
		public List<InfocardNode> Nodes = [];

        [WattleScriptHidden]
        public List<RichTextNode> CreateDisplayNodes(InfocardDisplayStyle style, FontManager fonts)
        {
            var rt = new List<RichTextNode>();
            foreach (var n in Nodes)
            {
                if (n is InfocardParagraphNode)
                {
                    rt.Add(new RichTextParagraphNode());
                    continue;
                }
                var src = (InfocardTextNode)n;
                var d = src.ManualFont ?? fonts.GetInfocardFont(src.FontIndex ?? style.FontIndex);
                string fontName = d.FontName;
                if (string.IsNullOrWhiteSpace(fontName))
                {
                    fontName = "Arial";
                }
                else if (fontName[0] == '$')
                {
                    fontName = fonts.ResolveNickname(fontName.Substring(1));
                }
                var tn = new RichTextTextNode()
                {
                    Bold = src.Bold ?? style.Bold,
                    Italic = src.Italic ?? style.Italic,
                    Underline = src.Underline ?? style.Underline,
                    Color = src.Color ?? style.Color,
                    Alignment = src.Alignment ?? style.Alignment,
                    Shadow = style.TextShadow,
                    Contents = src.Contents,
                    FontName = fontName,
                    FontSize = d.FontSize
                };
                rt.Add(tn);
            }
            return rt;
        }

		public string ExtractText()
		{
			var b = new StringBuilder();
			foreach (var n in Nodes)
            {
                switch (n)
                {
                    case InfocardParagraphNode:
                        b.AppendLine();
                        break;
                    case InfocardTextNode node:
                        b.Append(node.Contents);
                        break;
                }

            }

			return b.ToString();
		}

		public override string ToString()
		{
			return ExtractText();
		}
	}

    public class InfocardDisplayStyle
    {
        public static readonly InfocardDisplayStyle Default = new();

        public bool Bold;
        public bool Italic;
        public bool Underline;
        public int FontIndex;
        public Color4 Color = Color4.White;
        public TextAlignment Alignment = TextAlignment.Left;
        public OptionalColor TextShadow;
    }
}

