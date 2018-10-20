// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
			public int FontSize;
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

		public bool DropShadow = false;

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
			int size;
			Font fnt = fnts.GetInfocardFont(0, FontStyles.Regular, out size);
			foreach (var card in infocards)
			{
				for (int idx = 0; idx < card.Nodes.Count; idx++)
				{
					var node = card.Nodes[idx];
					if (node is InfocardParagraphNode)
					{
						dY += (int)fnt.LineHeight(size);
						dX = 0;
					}
					else
					{
						var n = (InfocardTextNode)node;
						var style = FontStyles.Regular;
						if (n.Bold) style |= FontStyles.Bold;
						if (n.Italic) style |= FontStyles.Italic;
						fnt = fnts.GetInfocardFont(n.FontIndex, style, out size);
						if (lastAlignment != n.Alignment)
							dX = 0;
						int origX = dX;
						int origY = dY;
						if (n.Alignment == TextAlignment.Left)
						{
							var text = WrapText(renderer, fnt, size, n.Contents, rectangle.Width, dX, out dX, ref dY);
							commands.Add(new TextCommand()
							{
								String = string.Join<string>("\n", text),
								X = origX,
								Y = origY,
								Color = n.Color,
								Underline = n.Underline,
								Align = n.Alignment,
								Font = fnt,
								FontSize = size,
							});
						}
						else if (n.Alignment == TextAlignment.Center)
						{
							var text = WrapText(renderer,fnt, size, n.Contents, rectangle.Width, dX, out dX, ref dY);
							var width0 = renderer.MeasureString(fnt, size, text[0]).X / 2f;
							var newX = (rectangle.Width / 2f) - origX - width0;
							if (commands.Count > 0 && origX != 0) //Don't shift if we're at the beginning
							{
								for (int i = commands.Count - 1; i >= 0; i--)
								{
									if (commands[i].Y != origY || commands[i].Align != n.Alignment) break;
									commands[i].X = (int)((float)commands[i].X - width0);
								}
								newX = commands[commands.Count - 1].X + renderer.MeasureString(commands[commands.Count - 1].Font, size, commands[commands.Count - 1].String).X;
							}
							//Append our text
							commands.Add(new TextCommand()
							{
								String = text[0],
								X = (int)newX,
								Y = origY,
								Color = n.Color,
								Underline = n.Underline,
								Align = n.Alignment,
								Font = fnt,
								FontSize = size
							});
							for (int i = 1; i < text.Count; i++)
							{
								commands.Add(new TextCommand()
								{
									String = text[i],
									X = (int)((rectangle.Width / 2f) - (renderer.MeasureString(fnt, size, text[i]).X / 2f)),
									Y = origY + (int)fnt.LineHeight(size) * i,
									Color = n.Color,
									Underline = n.Underline,
									Font = fnt,
									FontSize = size
								});
							}
						}
						else if (n.Alignment == TextAlignment.Right)
						{
							var text = WrapText(renderer, fnt, size, n.Contents, rectangle.Width, dX, out dX, ref dY);
							//Shift previous text left
							int width0 = renderer.MeasureString(fnt, size, text[0]).X;
							int newX = rectangle.Width - origX - width0;
							if (commands.Count > 0 && origX != 0) //Don't shift if we're at the beginning
							{
								for (int i = commands.Count - 1; i >= 0; i--)
								{
									if (commands[i].Y != origY || commands[i].Align != n.Alignment) break;
									commands[i].X -= width0;
								}
								newX = commands[commands.Count - 1].X + renderer.MeasureString(commands[commands.Count - 1].Font, size, commands[commands.Count - 1].String).X;
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
								Font = fnt,
								FontSize = size
							});
							for (int i = 1; i < text.Count; i++)
							{
								commands.Add(new TextCommand()
								{
									String = text[i],
									X = rectangle.Width - renderer.MeasureString(fnt, size, text[i]).X,
									Y = origY + (int)fnt.LineHeight(size) * i,
									Color = n.Color,
									Underline = n.Underline,
									Font = fnt,
									FontSize = size
								});
							}
						}
						lastAlignment = n.Alignment;
					}
				}
				dY += (int)fnt.LineHeight(size);
				dX = 0;
			}
			Height = commands[commands.Count - 1].Y + renderer.MeasureString(fnt, size, commands[commands.Count - 1].String).Y;
		}

		public static List<string> WrapText(Renderer2D renderer, Font font, int sz, string text, int maxLineWidth, int x, out int newX, ref int dY)
		{
			List<string> strings = new List<string>();
			string[] words = text.Split(' ');
			StringBuilder sb = new StringBuilder();
			int lineWidth = x;
			int spaceWidth = renderer.MeasureString(font, sz, " ").X;
			for (int i = 0; i < words.Length; i++)
			{
				var size = renderer.MeasureString(font, sz, words[i]);
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
					dY += (int)font.LineHeight(sz);
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
					if (DropShadow)
					{
						renderer.DrawStringBaseline(
							commands[i].Font,
							commands[i].FontSize,
							commands[i].String,
							commands[i].X + rectangle.X + 2,
							commands[i].Y + rectangle.Y - scrollOffset + 2,
							rectangle.X,
							Color4.Black,
							commands[i].Underline
						);
					}
					renderer.DrawStringBaseline(
						commands[i].Font,
						commands[i].FontSize,
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
