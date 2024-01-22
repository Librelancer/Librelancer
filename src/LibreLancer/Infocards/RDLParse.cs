// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using LibreLancer.Graphics.Text;

namespace LibreLancer.Infocards
{
	public static class RDLParse
	{
		//Constants and tables
		static readonly string[] SkipElements = new string[]
		{
			"RDL", "PUSH", "POP"
		};
		static readonly Dictionary<string, TextAlignment> Aligns = new Dictionary<string, TextAlignment>
		{
			{ "LEFT", TextAlignment.Left },
			{ "RIGHT", TextAlignment.Right },
			{ "CENTER", TextAlignment.Center }
		};
		static Dictionary<string, uint> NamedColors = new Dictionary<string, uint>
		{
			{ "fuchsia", 0xC2008800 },
			{ "gray", 0x80808000 },
			{ "blue", 0xE0484800 },
			{ "green", 0x13BF3B00 },
			{ "aqua", 0xE0C38700 },
			{ "red", 0x1D1DBF00 },
			{ "yellow", 0x52EAF500 },
			{ "white", 0xFFFFFF00 }
		};
		const uint TRA_bold = 0x01;
		const uint TRA_italic = 0x02;
		const uint TRA_underline = 0x04;
		const uint TRA_font = 0xF8;
		const uint TRA_color = 0xFFFFFF00;

		//Utility Functions
		static int ParseHexDigit(string colorstr, int index)
		{
			if (char.IsNumber(colorstr, index))
			{
				return (int)colorstr[index] - (int)'0';
			}
			return 10 + (int)colorstr[index] - (int)'a';
		}

		static uint GetColor(string str)
		{
			if (NamedColors.ContainsKey(str.ToLowerInvariant()))
			{
				return NamedColors[str.ToLowerInvariant()];
			}
			if (str.StartsWith("#", StringComparison.InvariantCulture))
			{
				byte r, g, b;
				if (str.Length == 4)
				{
					r = (byte)ParseHexDigit(str, 1);
					g = (byte)ParseHexDigit(str, 2);
					b = (byte)ParseHexDigit(str, 3);
					r = (byte)((r << 4) | r);
					g = (byte)((g << 4) | g);
					b = (byte)((b << 4) | b);
				}
				else if (str.Length == 7)
				{
					r = (byte)(ParseHexDigit(str, 1) * 16 + ParseHexDigit(str, 2));
					g = (byte)(ParseHexDigit(str, 3) * 16 + ParseHexDigit(str, 4));
					b = (byte)(ParseHexDigit(str, 5) * 16 + ParseHexDigit(str, 6));
				}
				else
					throw new Exception("Invalid color " + str);
				var bytes = new byte[] { 0x00, r, g, b };
				return BitConverter.ToUInt32(bytes, 0);
			}
			else if (str.StartsWith("0x", StringComparison.InvariantCulture))
				return (uint)int.Parse(str.Substring(2), NumberStyles.HexNumber);
			else
				return (uint)int.Parse(str);
		}

		static RichTextTextNode CopyAttributes(RichTextTextNode src)
		{
			return new RichTextTextNode()
			{
				Bold = src.Bold,
				Italic = src.Italic,
				Underline = src.Underline,
				FontName = src.FontName,
                FontSize = src.FontSize,
				Color = src.Color,
				Alignment = src.Alignment
			};
		}

		//Main Parsing
        public static Infocard Parse(string input, FontManager fonts, int defaultFont = 0)
        {
            try
            {
                return ParseInternal(input, fonts, defaultFont);
            }
            catch (Exception)
            {
                return new Infocard()
                {
                    Nodes = new List<RichTextNode>()
                    {
                        new RichTextTextNode() { FontName = "Arial", Color = Color4.Red, FontSize = 20, Contents = "PARSE FAILED" },
                        new RichTextParagraphNode(),
                        new RichTextParagraphNode(),
                        new RichTextTextNode() { FontName = "Arial", FontSize = 14, Contents = input }
                    }
                };
            }
        }
        static Infocard ParseInternal(string input, FontManager fonts, int defaultFont)
		{
            var fn = fonts.GetInfocardFont(defaultFont); //default font

            if (input == null)
                return new Infocard() { Nodes = new List<RichTextNode>() { new RichTextTextNode() { FontName = fn.FontName, FontSize = fn.FontSize, Contents = "IDS??" } } };
            if (string.IsNullOrWhiteSpace(input))
                return new Infocard() {Nodes = new List<RichTextNode>() { new RichTextTextNode() { FontName = fn.FontName, FontSize = fn.FontSize, Contents = "" }}};
            var nodes = new List<RichTextNode>();
			var current = new RichTextTextNode();
            current.FontName = fn.FontName;
            current.FontSize = fn.FontSize;
			using (var reader = XmlReader.Create(new StringReader(input)))
			{
				while (reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							string elemname = reader.Name.ToUpperInvariant();
							if (SkipElements.Contains(elemname))
								continue;
							Dictionary<string, string> attrs = null;
							if (reader.HasAttributes)
							{
								attrs = new Dictionary<string, string>();
								for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
								{
									reader.MoveToAttribute(attInd);
									attrs.Add(reader.Name.ToUpper(), reader.Value);
								}
								reader.MoveToElement();
							}
							switch (elemname)
							{
								case "PARA":
									nodes.Add(new RichTextParagraphNode());
									break;
								case "JUST":
									TextAlignment v;
									if (Aligns.TryGetValue(attrs["LOC"].ToUpperInvariant(), out v))
										current.Alignment = v;
									break;
								case "TRA":
                                    ParseTextRenderAttributes(attrs, current, fonts, defaultFont);
									break;
								case "TEXT":
									break;
								default:
									throw new Exception("Unexpected element " + elemname);
							}
							break;
						case XmlNodeType.Text:
							current.Contents = reader.Value;
							nodes.Add(current);
							current = CopyAttributes(current);
							break;
					}
				}
			}
			return new Infocard() { Nodes = nodes };
		}

        static uint ParseTRANumber(string num)
        {
            num = num.Trim();
            if (num.StartsWith("-")) return (uint) int.Parse(num);
            if (num.StartsWith("0x")) return uint.Parse(num.Substring(2), NumberStyles.HexNumber);
            return uint.Parse(num);
        }
		static void ParseTextRenderAttributes(Dictionary<string, string> attrs, RichTextTextNode node, FontManager fonts, int defaultFont)
		{
			uint data = 0;
			uint mask = 0;
			uint def = 0;
			string temp;
            if (attrs.TryGetValue("DATA", out temp))
                data = ParseTRANumber(temp);
			if (attrs.TryGetValue("MASK", out temp))
				mask = ParseTRANumber(temp);
			if (attrs.TryGetValue("DEF", out temp))
				def = ParseTRANumber(temp);
			if (attrs.TryGetValue("COLOR", out temp))
			{
				mask |= TRA_color;
				if (temp.ToUpperInvariant() == "DEFAULT")
				{
					def |= TRA_color;
				}
				else
				{
					data &= ~TRA_color;
					data |= GetColor(temp);
				}
			}
			if (attrs.TryGetValue("FONT", out temp))
			{
				mask |= TRA_font;
				if (temp.ToUpperInvariant() == "DEFAULT")
				{
					def |= TRA_font;
				}
				else
				{
					data &= ~TRA_font;
					data |= (uint.Parse(temp)) << 3;
				}
			}
			if (attrs.TryGetValue("BOLD", out temp))
			{
				mask |= TRA_bold;
				if (temp.ToUpperInvariant() == "DEFAULT")
					def |= TRA_bold;
				else if (temp.ToUpperInvariant() == "TRUE")
					data |= TRA_bold;
				else
					data &= ~TRA_bold;
			}
			if (attrs.TryGetValue("ITALIC", out temp))
			{
				mask |= TRA_italic;
				if (temp.ToUpperInvariant() == "DEFAULT")
					def |= TRA_italic;
				else if (temp.ToUpperInvariant() == "TRUE")
					data |= TRA_italic;
				else
					data &= ~TRA_italic;
			}
			if (attrs.TryGetValue("UNDERLINE", out temp))
			{
				mask |= TRA_underline;
				if (temp.ToUpperInvariant() == "DEFAULT")
					def |= TRA_underline;
				else if (temp.ToUpperInvariant() == "TRUE")
					data |= TRA_underline;
				else
					data &= ~TRA_underline;
			}
			if ((def & TRA_bold) != 0)
				node.Bold = false;
			else if ((mask & TRA_bold) != 0)
				node.Bold = (data & TRA_bold) != 0;

			if ((def & TRA_italic) != 0)
				node.Italic = false;
			else if ((mask & TRA_italic) != 0)
				node.Italic = (data & TRA_italic) != 0;

            if ((def & TRA_font) != 0)
            {
                var d = fonts.GetInfocardFont(defaultFont);
                node.FontName = d.FontName;
                node.FontSize = d.FontSize;
                //node.FontIndex = 0;
            }
            else if ((mask & TRA_font) != 0)
            {
                int fnt = (int)((data & TRA_font) >> 3);
                var d = fonts.GetInfocardFont(fnt);
                node.FontName = d.FontName;
                node.FontSize = d.FontSize;
            }
			if ((def & TRA_underline) != 0)
				node.Underline = false;
			else if ((mask & TRA_underline) != 0)
				node.Underline = (data & TRA_underline) != 0;

			if ((def & TRA_color) != 0)
				node.Color = Color4.White;
			else if ((mask & TRA_color) != 0)
			{
				var bytes = BitConverter.GetBytes((data & TRA_color));
				node.Color = new Color4((float)bytes[1] / 255f, (float)bytes[2] / 255f, (float)bytes[3] / 255f, 1f);
			}
		}
	}
}

