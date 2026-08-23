using System;
using System.Globalization;
using WattleScript.Interpreter;

namespace LibreLancer.Interface;

public enum MetricUnit
{
    Point,
    Percent,
    PercentWidth,
    PercentHeight
}

public enum MetricAxis
{
    X,
    Y
}

public readonly record struct Metric(MetricUnit Unit, float Value, float Constant)
{
    public static readonly Metric HundredPercent = new(MetricUnit.Percent, 1, 0);

    public float ToPoint(RectangleF parent, MetricAxis axis) => Constant + Unit switch
    {
        MetricUnit.Percent => axis == MetricAxis.X ? Value * parent.Width : Value * parent.Height,
        MetricUnit.PercentWidth => Value * parent.Width,
        MetricUnit.PercentHeight => Value * parent.Height,
        _ => Value
    };

    public static implicit operator Metric(float v) => new(MetricUnit.Point, v, 0);
    public static implicit operator Metric(string s) => Parse(s);

    private static bool IsDigit(int c)
    {
        return c >= '0' && c <= '9';
    }

    static ReadOnlySpan<char> GetNumber(ReadOnlySpan<char> str)
    {
        int i = 0;

        if (i < str.Length && str[i] == '-')
            i++;

        bool hasDecimal = false;

        while (i < str.Length)
        {
            if (IsDigit(str[i]))
            {
                i++;
            }
            else if (!hasDecimal && str[i] == '.')
            {
                hasDecimal = true;
                i++;
            }
            else
            {
                break;
            }
        }

        var res = str[..i];
        if (res is "." || res is "-")
            throw new FormatException($"Unexpected character '{res[0]}'");
        return res;
    }

    static string LiteralFloat(float f)
    {
        return f.ToString("0.###############", CultureInfo.InvariantCulture);
    }

    public override string ToString()
    {
        var str = Unit switch
        {
            MetricUnit.Percent => $"{LiteralFloat(Value * 100)}%",
            MetricUnit.PercentWidth => $"{LiteralFloat(Value * 100)}%w",
            MetricUnit.PercentHeight => $"{LiteralFloat(Value * 100)}%h",
            _ => LiteralFloat(Value + Constant),
        };
        if (Unit != MetricUnit.Point && Constant != 0)
        {
            str += Constant < 0 ? LiteralFloat(Constant) : $"+{LiteralFloat(Constant)}";
        }
        return str;
    }

    public static Metric Parse(ReadOnlySpan<char> str)
    {
        str = str.Trim();
        if (str.IsEmpty)
            return new(MetricUnit.Percent, 1, 0);
        // Get value
        var numberStr = GetNumber(str);
        int i = numberStr.Length;
        float value = float.Parse(numberStr, CultureInfo.InvariantCulture);
        // Skip whitespace
        while (i < str.Length && char.IsWhiteSpace(str[i]))
            i++;
        // No unit
        if (i == str.Length)
        {
            return new Metric(MetricUnit.Point, value, 0);
        }
        // Unit
        value /= 100.0f;
        if (str[i] != '%')
            throw new FormatException($"Unexpected character '{str[i]}'");
        i++;
        if(i == str.Length)
            return new Metric(MetricUnit.Percent, value, 0);
        var unit = MetricUnit.Percent;
        if (str[i] == 'h')
        {
            unit = MetricUnit.PercentHeight;
            i++;
        }
        else if (str[i] == 'w')
        {
            unit = MetricUnit.PercentWidth;
            i++;
        }
        // Skip whitespace
        while (i < str.Length && char.IsWhiteSpace(str[i]))
            i++;
        if(i == str.Length)
            return new Metric(unit, value, 0);
        bool neg = true;
        if (str[i] == '+')
        {
            neg = false;
        }
        else if (str[i] != '-')
        {
            throw new FormatException($"Unexpected character '{str[i]}'");
        }
        i++;
        // Skip whitespace
        while (i < str.Length && char.IsWhiteSpace(str[i]))
            i++;
        var constantStr = GetNumber(str.Slice(i));
        i += constantStr.Length;
        // Skip whitespace
        while (i < str.Length && char.IsWhiteSpace(str[i]))
            i++;
        if(i != str.Length)
            throw new FormatException($"Unexpected character '{str[i]}'");
        if (constantStr.IsEmpty)
            throw new FormatException("Expected constant term");
        var constant = float.Parse(constantStr, CultureInfo.InvariantCulture);
        return new Metric(unit, value, neg ? -constant : constant);
    }
}

