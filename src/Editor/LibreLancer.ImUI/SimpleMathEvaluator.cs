using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LibreLancer.ImUI;
static class NumericExpression
{
    public static bool TryEval(string text, out double value)
    {
        value = 0;

        // Fast path: normal number
        if (double.TryParse(
            text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value))
            return true;

        // Reject unsafe characters
        foreach (char c in text)
        {
            if (!"0123456789+-*/(). ".Contains(c))
                return false;
        }

        try
        {
            value = SimpleMathEvaluator.Evaluate(text);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static string FormatFloat(float value, string format)
    {
        // Handle common ImGui formats like "%.3f"
        if (format.StartsWith("%.") && format.EndsWith("f"))
        {
            var digitsStr = format.Substring(2, format.Length - 3);
            if (int.TryParse(digitsStr, out int digits))
            {
                return value.ToString($"F{digits}", CultureInfo.InvariantCulture);
            }
        }

        // Fallback
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
static class SimpleMathEvaluator
{
    static string _text;
    static int _pos;

    public static double Evaluate(string text)
    {
        _text = text;
        _pos = 0;

        double value = ParseExpression();
        SkipWhitespace();

        if (_pos < _text.Length)
            throw new Exception("Unexpected character");

        return value;
    }

    static double ParseExpression()
    {
        double value = ParseTerm();

        while (true)
        {
            SkipWhitespace();

            if (Match('+'))
                value += ParseTerm();
            else if (Match('-'))
                value -= ParseTerm();
            else
                break;
        }

        return value;
    }

    static double ParseTerm()
    {
        double value = ParseFactor();

        while (true)
        {
            SkipWhitespace();

            if (Match('*'))
                value *= ParseFactor();
            else if (Match('/'))
                value /= ParseFactor();
            else
                break;
        }

        return value;
    }

    static double ParseFactor()
    {
        SkipWhitespace();

        if (Match('+')) return ParseFactor();
        if (Match('-')) return -ParseFactor();

        if (Match('('))
        {
            double value = ParseExpression();
            if (!Match(')'))
                throw new Exception("Missing ')'");
            return value;
        }

        return ParseNumber();
    }

    static double ParseNumber()
    {
        SkipWhitespace();
        int start = _pos;

        while (_pos < _text.Length &&
              (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
            _pos++;

        if (start == _pos)
            throw new Exception("Number expected");

        return double.Parse(
            _text.Substring(start, _pos - start),
            CultureInfo.InvariantCulture);
    }

    static bool Match(char c)
    {
        if (_pos < _text.Length && _text[_pos] == c)
        {
            _pos++;
            return true;
        }
        return false;
    }

    static void SkipWhitespace()
    {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
            _pos++;
    }

    

}

