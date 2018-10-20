// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
namespace LibreLancer.XInt
{
    public static class Parser
    {
        public static string[] Tokens(string s) => s.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        public static float[] FloatArray(string s) => Tokens(s).Select((x) => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        public static int[] IntArray(string s) => Tokens(s).Select((x) => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        public static float[] FloatArray(dynamic arr) => FloatArray(arr.Text);
        static float InvariantFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        public static float Percentage(string s)
        {
            if (s.EndsWith("%"))
                return InvariantFloat(s.TrimEnd('%')) / 100;
            else
                return InvariantFloat(s);
        }
        static Dictionary<string, Color4> namedColors = new Dictionary<string, Color4>(StringComparer.InvariantCultureIgnoreCase);
        static Parser()
        {
            foreach(var f in typeof(Color4).GetProperties(BindingFlags.Public | BindingFlags.Static)) {
                namedColors.Add(f.Name, (Color4)f.GetValue(null));
            }
        }
        static int GetDigit(char c)
        {
            var i = (int)c;
            if (i >= '0' && i <= '9')
                return i - '0';
            if (i >= 'a' && i <= 'f')
                return 10 + (i - 'a');
            if (i >= 'A' && i <= 'F')
                return 10 + (i - 'A');
            throw new Exception("Invalid hex digit " + c);
        }
        public static Color4 Color(string s)
        {
            var text = s.Trim();
            Color4 t;
           
            if (text[0] == '#')
            {
                var hexDigits = text.Substring(1).Trim();
                if (hexDigits.Length == 3)
                {
                    int red = GetDigit(hexDigits[0]);
                    int green = GetDigit(hexDigits[1]);
                    int blue = GetDigit(hexDigits[2]);
                    red = red << 4 | red;
                    green = green << 4 | green;
                    blue = blue << 4 | blue;
                    return new Color4(red / 255f, green / 255f, blue / 255f, 1);
                }
                else if (hexDigits.Length == 6)
                {
                    int red = GetDigit(hexDigits[0]) << 4 | GetDigit(hexDigits[1]);
                    int green = GetDigit(hexDigits[2]) << 4 | GetDigit(hexDigits[3]);
                    int blue = GetDigit(hexDigits[4]) << 4 | GetDigit(hexDigits[5]);
                    return new Color4(red / 255f, green / 255f, blue / 255f, 1);
                }
                else if (hexDigits.Length == 8)
                {
                    int red = GetDigit(hexDigits[0]) << 4 | GetDigit(hexDigits[1]);
                    int green = GetDigit(hexDigits[2]) << 4 | GetDigit(hexDigits[3]);
                    int blue = GetDigit(hexDigits[4]) << 4 | GetDigit(hexDigits[5]);
                    int alpha = GetDigit(hexDigits[6]) << 4 | GetDigit(hexDigits[7]);
                    return new Color4(red / 255f, green / 255f, blue / 255f, alpha / 255f);
                }
            }
            else if (text.StartsWith("rgba", StringComparison.InvariantCultureIgnoreCase))
            {
                var val = text.Substring(4).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') throw new Exception("Invalid rgba color " + text);
                var split = val.Substring(1, val.Length - 2).Trim().Split(',');
                if (split.Length != 4) throw new Exception("Invalid rgba color " + text);
                var floats = split.Select((x) => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
                var alpha = Percentage(split[3].Trim());
                if (alpha > 1) alpha = (alpha / 255f); //out of spec but I'm allowed to ;)
                return new Color4(floats[0] / 255, floats[1] / 255, floats[2] / 255, alpha);
            }
            else if (text.StartsWith("rgb", StringComparison.InvariantCultureIgnoreCase))
            {
                var val = text.Substring(3).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') throw new Exception("Invalid rgb color " + text);
                var split = val.Substring(1, val.Length - 2).Trim().Split(',');
                if (split.Length != 3) throw new Exception("Invalid rgb color " + text);
                var floats = split.Select((x) => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
                return new Color4(floats[0] / 255, floats[1] / 255, floats[2] / 255, 1);
            }
            else if (text.StartsWith("hsla", StringComparison.InvariantCultureIgnoreCase)) {
                var val = text.Substring(4).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') throw new Exception("Invalid hsla color " + text);
                var split = val.Substring(1, val.Length - 2).Trim().Split(',');
                if (split.Length != 4) throw new Exception("Invalid hsla color " + text);
                var angle = MathHelper.DegreesToRadians(InvariantFloat(split[0].Trim()));
                var sat = Percentage(split[1].Trim());
                var lit = Percentage(split[2].Trim());
                var alpha = Percentage(split[3].Trim());
                if (alpha > 1) alpha /= 255f;
                return new Color4(new HSLColor(angle, sat, lit).ToRGB(), alpha);

            } else if (text.StartsWith("hsl", StringComparison.InvariantCultureIgnoreCase)) {
                var val = text.Substring(3).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') throw new Exception("Invalid hsl color " + text);
                var split = val.Substring(1, val.Length - 2).Trim().Split(',');
                if (split.Length != 3) throw new Exception("Invalid hsl color " + text);
                var angle = MathHelper.DegreesToRadians(InvariantFloat(split[0].Trim()));
                var sat = Percentage(split[1].Trim());
                var lit = Percentage(split[2].Trim());
                return new Color4(new HSLColor(angle, sat, lit).ToRGB(), 1f);
            }
            else if (namedColors.TryGetValue(text, out t))
                return t;
            throw new Exception("Invalid color" + s);
        }
    }
}
