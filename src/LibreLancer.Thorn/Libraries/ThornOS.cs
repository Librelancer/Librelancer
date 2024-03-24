using System;
using System.Collections.Generic;
using System.Text;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn.Libraries;

static class ThornOS
{
    static DateTime Time0 = DateTime.UtcNow;

    public static object clock(object[] args)
    {
        var x = (DateTime.UtcNow - Time0).TotalSeconds;
        if ((int)x <= 0.0)
            return 0f;
        else
            return (float)(int)x;
    }

    public static object date(object[] args)
    {
        return StrFTime(args[0].ToString(), DateTime.Now);
    }

    private static string StrFTime(string format, DateTime d)
    {
        // ref: http://www.cplusplus.com/reference/ctime/strftime/

        Dictionary<char, string> STANDARD_PATTERNS = new Dictionary<char, string>()
        {
            { 'a', "ddd" },
            { 'A', "dddd" },
            { 'b', "MMM" },
            { 'B', "MMMM" },
            { 'c', "f" },
            { 'd', "dd" },
            { 'D', "MM/dd/yy" },
            { 'F', "yyyy-MM-dd" },
            { 'g', "yy" },
            { 'G', "yyyy" },
            { 'h', "MMM" },
            { 'H', "HH" },
            { 'I', "hh" },
            { 'm', "MM" },
            { 'M', "mm" },
            { 'p', "tt" },
            { 'r', "h:mm:ss tt" },
            { 'R', "HH:mm" },
            { 'S', "ss" },
            { 'T', "HH:mm:ss" },
            { 'y', "yy" },
            { 'Y', "yyyy" },
            { 'x', "d" },
            { 'X', "T" },
            { 'z', "zzz" },
            { 'Z', "zzz" },
        };


        StringBuilder sb = new StringBuilder();

        bool isEscapeSequence = false;

        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];

            if (c == '%')
            {
                if (isEscapeSequence)
                {
                    sb.Append('%');
                    isEscapeSequence = false;
                }
                else
                    isEscapeSequence = true;

                continue;
            }

            if (!isEscapeSequence)
            {
                sb.Append(c);
                continue;
            }

            if (c == 'O' || c == 'E') continue; // no modifiers

            isEscapeSequence = false;

            if (STANDARD_PATTERNS.ContainsKey(c))
            {
                sb.Append(d.ToString(STANDARD_PATTERNS[c]));
            }
            else if (c == 'e')
            {
                string s = d.ToString("%d");
                if (s.Length < 2) s = " " + s;
                sb.Append(s);
            }
            else if (c == 'n')
            {
                sb.Append('\n');
            }
            else if (c == 't')
            {
                sb.Append('\t');
            }
            else if (c == 'C')
            {
                sb.Append((int)(d.Year / 100));
            }
            else if (c == 'j')
            {
                sb.Append(d.DayOfYear.ToString("000"));
            }
            else if (c == 'u')
            {
                int weekDay = (int)d.DayOfWeek;
                if (weekDay == 0)
                    weekDay = 7;

                sb.Append(weekDay);
            }
            else if (c == 'w')
            {
                int weekDay = (int)d.DayOfWeek;
                sb.Append(weekDay);
            }
            else if (c == 'U')
            {
                // Week number with the first Sunday as the first day of week one (00-53)
                sb.Append("??");
            }
            else if (c == 'V')
            {
                // ISO 8601 week number (00-53)
                sb.Append("??");
            }
            else if (c == 'W')
            {
                // Week number with the first Monday as the first day of week one (00-53)
                sb.Append("??");
            }
            else
            {
                throw new Exception($"bad argument #1 to 'date' (invalid conversion specifier '{format}')");
            }
        }

        return sb.ToString();
    }


    public static void SetBuiltins(Dictionary<string, object> Env, ThornRuntime runtime)
    {
        Env["clock"] = (ThornRuntimeFunction)clock;
        Env["date"] = (ThornRuntimeFunction)date;
    }
}
