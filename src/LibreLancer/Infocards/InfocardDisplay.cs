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
using System.Collections.Generic;
using System.Text;

namespace LibreLancer.Infocards
{
	public class InfocardDisplay
	{
		class TextCommand
		{
			public string String;
			public int X;
			public int Y;
			public Color4 Color;
			public bool Underline;
			public TextAlignment Align;
			public Font Font;
		}

		Infocard[] infocards;
		Rectangle rectangle;
		Renderer2D renderer;
		FontManager fnts;
		List<TextCommand> commands;

		public int Height
		{
			get; private set; 
		}

		public InfocardDisplay(FreelancerGame g, Rectangle rect, params Infocard[] card)
		{
			infocards = card;
			rectangle = rect;
			renderer = g.Renderer2D;
			fnts = g.Fonts;
			UpdateLayout();
		}

		public void SetRectangle(Rectangle rect)
		{
			rectangle = rect;
			UpdateLayout();
		}

		public void SetInfocard(params Infocard[] card)
		{
			infocards = card;
			UpdateLayout();
		}

		public void UpdateLayout()
		{
			commands = new List<TextCommand>();
			int dX = 0, dY = 0;
			TextAlignment lastAlignment = TextAlignment.Left;
			Font fnt = fnts.GetInfocardFont(0, FontStyles.Regular);
			foreach (var card in infocards)
			{
				foreach (var node in card.Nodes)
				{
					if (node is InfocardParagraphNode)
					{
						dY += (int)fnt.LineHeight;
						dX = 0;
					}
					else
					{
						var n = (InfocardTextNode)node;
						var style = FontStyles.Regular;
						if (n.Bold) style |= FontStyles.Bold;
						if (n.Italic) style |= FontStyles.Italic;
						fnt = fnts.GetInfocardFont(n.FontIndex, style);
						if (lastAlignment != n.Alignment)
							dX = 0;
						int origX = dX;
						int origY = dY;
						if (n.Alignment == TextAlignment.Left || n.Alignment == TextAlignment.Center)
						{
							var text = WrapText(fnt, n.Contents, rectangle.Width, dX, out dX, ref dY);
							commands.Add(new TextCommand()
							{
								String = string.Join<string>("\n", text),
								X = origX,
								Y = origY,
								Color = n.Color,
								Underline = n.Underline,
								Align = n.Alignment,
								Font = fnt
							});
						}
						else if (n.Alignment == TextAlignment.Right)
						{
							var text = WrapText(fnt, n.Contents, rectangle.Width, dX, out dX, ref dY);
							//Shift previous text left
							int width0 = renderer.MeasureString(fnt, text[0]).X;
							int newX = rectangle.Width - origX - width0;
							if (commands.Count > 0 && origX != 0) //Don't shift if we're at the beginning
							{
								for (int i = commands.Count - 1; i >= 0; i--)
								{
									if (commands[i].Y != origY || commands[i].Align != n.Alignment) break;
									commands[i].X -= width0;
								}
								newX = commands[commands.Count - 1].X + renderer.MeasureString(commands[commands.Count - 1].Font, commands[commands.Count - 1].String).X;
							}
							//Append our text
							commands.Add(new TextCommand()
							{
								String = text[0],
								X = newX,
								Y = origY,
								Color = n.Color,
								Underline = n.Underline,
								Align = n.Alignment,
								Font = fnt
							});
							for (int i = 1; i < text.Count; i++)
							{
								commands.Add(new TextCommand()
								{
									String = text[i],
									X = rectangle.Width - renderer.MeasureString(fnt, text[i]).X,
									Y = origY + (int)fnt.LineHeight * i,
									Color = n.Color,
									Underline = n.Underline,
									Font = fnt
								});
							}
						}
						lastAlignment = n.Alignment;
					}
				}
				dY += (int)fnt.LineHeight;
				dX = 0;
			}
			Height = commands[commands.Count - 1].Y + renderer.MeasureString(fnt, commands[commands.Count - 1].String).Y;
		}

		List<string> WrapText(Font font, string text, int maxLineWidth, int x, out int newX, ref int dY)
		{
			List<string> strings = new List<string>();
			string[] words = text.Split(' ');
			StringBuilder sb = new StringBuilder();
			int lineWidth = x;
			int spaceWidth = renderer.MeasureString(font, " ").X;
			for (int i = 0; i < words.Length; i++)
			{
				var size = renderer.MeasureString(font, words[i]);
				if (lineWidth + size.X < maxLineWidth)
				{
					lineWidth += size.X + spaceWidth;
				}
				else
				{
					if (sb.Length > 0)
					{
						strings.Add(sb.ToString());
						sb.Clear();
					}
					dY += (int)font.LineHeight;
					lineWidth = size.X + spaceWidth;
				}
				sb.Append(words[i]);
				if(i != words.Length - 1)
					sb.Append(" ");
			}
			newX = lineWidth;
			if (sb.Length > 0)
			{
				strings.Add(sb.ToString());
				sb.Clear();
			}
			return strings;
		}

		public void Draw(Renderer2D renderer, int scrollOffset = 0)
		{
			renderer.DrawWithClip(rectangle, () =>
			{
				for (int i = 0; i < commands.Count; i++)
				{
					renderer.DrawStringBaseline(
						commands[i].Font,
						commands[i].String,
						commands[i].X + rectangle.X,
						commands[i].Y + rectangle.Y - scrollOffset,
						rectangle.X,
						commands[i].Color,
						commands[i].Underline
					);
				}
			});
		}
	}
}
