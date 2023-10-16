// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
namespace LibreLancer.Interface
{
    public static class Parser
    {
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

        public static bool TryParseColor(ReadOnlySpan<char> s, out Color4 t)
        {
            var text = s.Trim();
            t = Color4.Transparent;

            if (text[0] == '#')
            {
                var hexDigits = text.Slice(1).Trim();
                if (hexDigits.Length == 3)
                {
                    int red = GetDigit(hexDigits[0]);
                    int green = GetDigit(hexDigits[1]);
                    int blue = GetDigit(hexDigits[2]);
                    red = red << 4 | red;
                    green = green << 4 | green;
                    blue = blue << 4 | blue;
                    t = new Color4(red / 255f, green / 255f, blue / 255f, 1);
                    return true;
                }
                else if (hexDigits.Length == 6)
                {
                    int red = GetDigit(hexDigits[0]) << 4 | GetDigit(hexDigits[1]);
                    int green = GetDigit(hexDigits[2]) << 4 | GetDigit(hexDigits[3]);
                    int blue = GetDigit(hexDigits[4]) << 4 | GetDigit(hexDigits[5]);
                    t = new Color4(red / 255f, green / 255f, blue / 255f, 1);
                    return true;
                }
                else if (hexDigits.Length == 8)
                {
                    int red = GetDigit(hexDigits[0]) << 4 | GetDigit(hexDigits[1]);
                    int green = GetDigit(hexDigits[2]) << 4 | GetDigit(hexDigits[3]);
                    int blue = GetDigit(hexDigits[4]) << 4 | GetDigit(hexDigits[5]);
                    int alpha = GetDigit(hexDigits[6]) << 4 | GetDigit(hexDigits[7]);
                    t = new Color4(red / 255f, green / 255f, blue / 255f, alpha / 255f);
                    return true;
                }
            }
            else if (text.StartsWith("rgba", StringComparison.InvariantCultureIgnoreCase))
            {
                var val = text.Slice(4).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') return false;
                var split = val.Slice(1, val.Length - 2).Trim().ToString().Split(',');
                if (split.Length != 4) return false;
                var floats = split.Select((x) => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
                var alpha = Percentage(split[3].Trim());
                if (alpha > 1) alpha = (alpha / 255f); //out of spec but I'm allowed to ;)
                t = new Color4(floats[0] / 255, floats[1] / 255, floats[2] / 255, alpha);
                return true;
            }
            else if (text.StartsWith("rgb", StringComparison.InvariantCultureIgnoreCase))
            {
                var val = text.Slice(3).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') return false;
                var split = val.Slice(1, val.Length - 2).Trim().ToString().Split(',');
                if (split.Length != 3) return false;
                var floats = split.Select((x) => float.Parse(x.Trim(), CultureInfo.InvariantCulture)).ToArray();
                t = new Color4(floats[0] / 255, floats[1] / 255, floats[2] / 255, 1);
                return true;
            }
            else if (text.StartsWith("hsla", StringComparison.InvariantCultureIgnoreCase)) {
                var val = text.Slice(4).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') return false;
                var split = val.Slice(1, val.Length - 2).Trim().ToString().Split(',');
                if (split.Length != 4) return false;
                var angle = MathHelper.DegreesToRadians(InvariantFloat(split[0].Trim()));
                var sat = Percentage(split[1].Trim());
                var lit = Percentage(split[2].Trim());
                var alpha = Percentage(split[3].Trim());
                if (alpha > 1) alpha /= 255f;
                t = new Color4(new HSLColor(angle, sat, lit).ToRGB(), alpha);
                return true;

            } else if (text.StartsWith("hsl", StringComparison.InvariantCultureIgnoreCase)) {
                var val = text.Slice(3).Trim();
                if (val[0] != '(' || val[val.Length - 1] != ')') return false;
                var split = val.Slice(1, val.Length - 2).Trim().ToString().Split(',');
                if (split.Length != 3) return false;
                var angle = MathHelper.DegreesToRadians(InvariantFloat(split[0].Trim()));
                var sat = Percentage(split[1].Trim());
                var lit = Percentage(split[2].Trim());
                t =  new Color4(new HSLColor(angle, sat, lit).ToRGB(), 1f);
                return true;
            }
            else if (namedColors.TryGetValue(text.ToString(), out t))
                return true;
            t = Color4.White;
            return false;

        }

        public static Color4 Color(string s)
        {
            if(!TryParseColor(s, out var col))
                throw new Exception("Invalid color" + s);
            return col;
        }
    }
}
