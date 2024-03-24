using System;

namespace LibreLancer.Thorn;

static class Conversion
{
    public static bool TryGetNumber(object o, out float num)
    {
        num = float.NaN;
        switch (o)
        {
            case double d:
                num = (float)d;
                return true;
            case float f:
                num = f;
                return true;
            case byte b:
                num = b;
                return true;
            case sbyte sb:
                num = sb;
                return true;
            case ushort us:
                num = us;
                return true;
            case uint ui:
                num = ui;
                return true;
            case ulong ul:
                num = ul;
                return true;
            case short s:
                num = s;
                return true;
            case int i:
                num = i;
                return true;
            case long l:
                num = l;
                return true;
            case decimal dl:
                num = (float)dl;
                return true;
        }
        var t = o.GetType();
        if (t.IsEnum)
        {
           num = (float)Convert.ChangeType(o, TypeCode.Single);
           return true;
        }
        return false;
    }
}
