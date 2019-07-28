// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Text;
namespace LibreLancer
{
    public class TextUtils
    {
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
                if (i != words.Length - 1)
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
    }
}
