//
// This part adapted from KopiLua - https://github.com/NLua/KopiLua
//
// =========================================================================================================
//
// Kopi Lua License
// ----------------
// MIT License for KopiLua
// Copyright (c) 2012 LoDC
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ===============================================================================
// Lua License
// -----------
// Lua is licensed under the terms of the MIT license reproduced below.
// This means that Lua is free software and can be used for both academic
// and commercial purposes at absolutely no cost.
// For details and rationale, see http://www.lua.org/license.html .
// ===============================================================================
// Copyright (C) 1994-2008 Lua.org, PUC-Rio.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LibreLancer.Thorn.Libraries
{
    internal static class KopiLua
    {
        internal static Regex formatRegex = new Regex(@"\%(\d*\$)?([,\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?(.)");

        static string QuoteString(string input)
        {
            var builder = new StringBuilder();
            builder.Append('"');
            for (int i = 0; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '"':
                    case '\\':
                    case '\n':
                        builder.Append('\\').Append(input[i]);
                        break;
                    case '\r':
                        builder.Append(@"\r");
                        break;
                    default:
                        if (input[i] < (char) 16)
                        {
                            if (i + 1 < input.Length && char.IsNumber(input[i + 1]))
                                builder.AppendFormat("\\{0:000}", (int) input[i]);
                            else
                                builder.AppendFormat("\\{0}", (int) input[i]);
                        }
                        else
                        {
                            builder.Append(input[i]);
                        }

                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }

        public static string sprintf(string Format, ReadOnlySpan<object> args)
        {
            #region Variables

            StringBuilder f = new StringBuilder();
            //Regex r = new Regex( @"\%(\d*\$)?([\'\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfeEgGpn%])" );
            //"%[parameter][flags][width][.precision][length]type"
            Match m = null;
            string w = String.Empty;
            int defaultParamIx = 0;
            int paramIx;
            object o = null;

            bool flagLeft2Right = false;
            bool flagAlternate = false;
            bool flagPositiveSign = false;
            bool flagPositiveSpace = false;
            bool flagZeroPadding = false;
            bool flagGroupThousands = false;

            int fieldLength = 0;
            int fieldPrecision = 0;
            char shortLongIndicator = '\0';
            char formatSpecifier = '\0';
            char paddingCharacter = ' ';

            #endregion

            // find all format parameters in format string
            f.Append(Format);
            m = formatRegex.Match(f.ToString());
            while (m.Success)
            {
                #region parameter index

                paramIx = defaultParamIx;
                if (m.Groups[1] != null && m.Groups[1].Value.Length > 0)
                {
                    string val = m.Groups[1].Value.Substring(0, m.Groups[1].Value.Length - 1);
                    paramIx = Convert.ToInt32(val) - 1;
                }

                ;

                #endregion

                #region format flags

                // extract format flags
                flagAlternate = false;
                flagLeft2Right = false;
                flagPositiveSign = false;
                flagPositiveSpace = false;
                flagZeroPadding = false;
                flagGroupThousands = false;
                if (m.Groups[2] != null && m.Groups[2].Value.Length > 0)
                {
                    string flags = m.Groups[2].Value;

                    flagAlternate = (flags.IndexOf('#') >= 0);
                    flagLeft2Right = (flags.IndexOf('-') >= 0);
                    flagPositiveSign = (flags.IndexOf('+') >= 0);
                    flagPositiveSpace = (flags.IndexOf(' ') >= 0);
                    flagGroupThousands = (flags.IndexOf(',') >= 0);

                    // positive + indicator overrides a
                    // positive space character
                    if (flagPositiveSign && flagPositiveSpace)
                        flagPositiveSpace = false;
                }

                #endregion

                #region field length

                // extract field length and
                // pading character
                paddingCharacter = ' ';
                fieldLength = int.MinValue;
                if (m.Groups[3] != null && m.Groups[3].Value.Length > 0)
                {
                    fieldLength = Convert.ToInt32(m.Groups[3].Value);
                    if (m.Groups[3].Value.Length > 2)
                        throw new Exception("invalid format (width or precision too long)");
                    flagZeroPadding = (m.Groups[3].Value[0] == '0');
                }

                #endregion

                if (flagZeroPadding)
                    paddingCharacter = '0';

                // left2right allignment overrides zero padding
                if (flagLeft2Right && flagZeroPadding)
                {
                    flagZeroPadding = false;
                    paddingCharacter = ' ';
                }

                #region field precision

                // extract field precision
                fieldPrecision = int.MinValue;
                if (m.Groups[4] != null && m.Groups[4].Value.Length > 0)
                {
                    if (m.Groups[4].Value.Length > 2)
                        throw new Exception("invalid format (width or precision too long)");
                    fieldPrecision = Convert.ToInt32(m.Groups[4].Value);
                }

                #endregion

                #region short / long indicator

                // extract short / long indicator
                shortLongIndicator = Char.MinValue;
                if (m.Groups[5] != null && m.Groups[5].Value.Length > 0)
                    shortLongIndicator = m.Groups[5].Value[0];

                #endregion

                #region format specifier

                // extract format
                formatSpecifier = Char.MinValue;
                if (m.Groups[6] != null && m.Groups[6].Value.Length > 0)
                    formatSpecifier = m.Groups[6].Value[0];

                #endregion

                // default precision is 6 digits if none is specified except
                if (fieldPrecision == int.MinValue &&
                    formatSpecifier != 's' &&
                    formatSpecifier != 'c' &&
                    Char.ToUpper(formatSpecifier) != 'X' &&
                    formatSpecifier != 'o')
                    fieldPrecision = 6;

                #region get next value parameter

                // get next value parameter and convert value parameter depending on short / long indicator

                float nArg = 0;
                bool processed = false;

                void GetNumber()
                {
                    if (processed) return;
                    processed = true;
                    if (!Conversion.TryGetNumber(o, out nArg))
                        throw new Exception($"format bad argument #{paramIx}");
                }

                if (paramIx >= args.Length)
                    o = null;
                else
                {
                    o = args[paramIx];

                    if (shortLongIndicator == 'h')
                    {
                        GetNumber();
                        nArg = (short) nArg;
                    }
                    else if (shortLongIndicator == 'l')
                    {
                        GetNumber();
                        nArg = (long) nArg;
                    }
                }

                #endregion

                // convert value parameters to a string depending on the formatSpecifier
                w = String.Empty;
                switch (formatSpecifier)
                {
                    case '%': // % character
                        w = "%";
                        break;
                    case 'd': // integer
                        GetNumber();
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                            fieldLength, int.MinValue, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, (long) nArg);
                        defaultParamIx++;
                        break;
                    case 'i': // integer
                        goto case 'd';
                    case 'o': // octal integer - no leading zero
                        GetNumber();
                        w = FormatOct("o", flagAlternate,
                            fieldLength, int.MinValue, flagLeft2Right,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'x': // hex integer - no leading zero
                        GetNumber();
                        w = FormatHex("x", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            paddingCharacter, (ulong) (long) nArg);
                        defaultParamIx++;
                        break;
                    case 'X': // same as x but with capital hex characters
                        GetNumber();
                        w = FormatHex("X", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            paddingCharacter, (ulong) (long) nArg);
                        defaultParamIx++;
                        break;
                    case 'u': // unsigned integer
                        GetNumber();
                        w = FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate,
                            fieldLength, int.MinValue, flagLeft2Right,
                            false, false,
                            paddingCharacter, (ulong) (long) nArg);
                        defaultParamIx++;
                        break;
                    case 'c': // character
                        GetNumber();
                        w = Convert.ToChar((long) nArg).ToString();
                        defaultParamIx++;
                        break;
                    case 's': // string
                        //string t = "{0" + ( fieldLength != int.MinValue ? "," + ( flagLeft2Right ? "-" : String.Empty ) + fieldLength.ToString() : String.Empty ) + ":s}";
                        w = args[paramIx].ToString();
                        if (fieldPrecision >= 0)
                            w = w.Substring(0, fieldPrecision);

                        if (fieldLength != int.MinValue)
                            if (flagLeft2Right)
                                w = w.PadRight(fieldLength, paddingCharacter);
                            else
                                w = w.PadLeft(fieldLength, paddingCharacter);
                        defaultParamIx++;
                        break;
                    case 'q':
                        w = QuoteString(args[paramIx].ToString());
                        defaultParamIx++;
                        break;
                    case 'f': // double
                        GetNumber();
                        w = FormatNumber((flagGroupThousands ? "n" : "f"), flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'e': // double / exponent
                        GetNumber();
                        w = FormatNumber("e", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'E': // double / exponent
                        GetNumber();
                        w = FormatNumber("E", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'g': // double / exponent
                        GetNumber();
                        w = FormatNumber("g", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'G': // double / exponent
                        GetNumber();
                        w = FormatNumber("G", flagAlternate,
                            fieldLength, fieldPrecision, flagLeft2Right,
                            flagPositiveSign, flagPositiveSpace,
                            paddingCharacter, nArg);
                        defaultParamIx++;
                        break;
                    case 'p': // pointer
                        w = "0x0";
                        defaultParamIx++;
                        break;
                    default:
                        throw new Exception($"invalid option '{m.Value}' to 'format'");
                }

                // replace format parameter with parameter value
                // and start searching for the next format parameter
                // AFTER the position of the current inserted value
                // to prohibit recursive matches if the value also
                // includes a format specifier
                f.Remove(m.Index, m.Length);
                f.Insert(m.Index, w);
                m = formatRegex.Match(f.ToString(), m.Index + w.Length);
            }

            return f.ToString();
        }

        private static string FormatOct(string NativeFormat, bool Alternate,
            int FieldLength, int FieldPrecision,
            bool Left2Right,
            char Padding, double Value)
        {
            string w = String.Empty;
            string lengthFormat = "{0" + (FieldLength != int.MinValue
                ? "," + (Left2Right ? "-" : String.Empty) + FieldLength.ToString()
                : String.Empty) + "}";


            w = Convert.ToString((long) Value, 8);

            if (Left2Right || Padding == ' ')
            {
                if (Alternate && w != "0")
                    w = "0" + w;
                w = String.Format(lengthFormat, w);
            }
            else
            {
                if (FieldLength != int.MinValue)
                    w = w.PadLeft(FieldLength - (Alternate && w != "0" ? 1 : 0), Padding);
                if (Alternate && w != "0")
                    w = "0" + w;
            }


            return w;
        }

        private static string FormatHex(string NativeFormat, bool Alternate,
            int FieldLength, int FieldPrecision,
            bool Left2Right,
            char Padding, ulong Value)
        {
            string w = String.Empty;
            string lengthFormat = "{0" + (FieldLength != int.MinValue
                ? "," + (Left2Right ? "-" : String.Empty) + FieldLength.ToString()
                : String.Empty) + "}";
            string numberFormat = "{0:" + NativeFormat +
                                  (FieldPrecision != int.MinValue ? FieldPrecision.ToString() : String.Empty) + "}";
            w = String.Format(numberFormat, Value);

            if (Left2Right || Padding == ' ')
            {
                if (Alternate)
                    w = (NativeFormat == "x" ? "0x" : "0X") + w;
                w = String.Format(lengthFormat, w);
            }
            else
            {
                if (FieldLength != int.MinValue)
                    w = w.PadLeft(FieldLength - (Alternate ? 2 : 0), Padding);
                if (Alternate)
                    w = (NativeFormat == "x" ? "0x" : "0X") + w;
            }

            return w;
        }

        static bool IsPositive(object o)
        {
            if (!Conversion.TryGetNumber(o, out var n))
            {
                throw new InvalidCastException();
            }
            return n >= 0;
        }

        private static string FormatNumber(string NativeFormat, bool Alternate,
            int FieldLength, int FieldPrecision,
            bool Left2Right,
            bool PositiveSign, bool PositiveSpace,
            char Padding, object Value)
        {
            string w = String.Empty;
            string lengthFormat = "{0" + (FieldLength != int.MinValue
                ? "," + (Left2Right ? "-" : String.Empty) + FieldLength.ToString()
                : String.Empty) + "}";
            string numberFormat = "{0:" + NativeFormat +
                                  (FieldPrecision != int.MinValue ? FieldPrecision.ToString() : "0") + "}";

            w = String.Format(CultureInfo.InvariantCulture, numberFormat, Value);

            if (Left2Right || Padding == ' ')
            {
                if (IsPositive(Value))
                    w = (PositiveSign ? "+" : (PositiveSpace ? " " : String.Empty)) + w;
                w = String.Format(lengthFormat, w);
            }
            else
            {
                if (w.StartsWith("-"))
                    w = w.Substring(1);
                if (FieldLength != int.MinValue)
                    if (PositiveSign) // xan - change here
                        w = w.PadLeft(FieldLength - 1, Padding);
                    else
                        w = w.PadLeft(FieldLength, Padding);
                if (IsPositive(Value))
                    w = (PositiveSign ? "+" : "") + w; // xan - change here
                else
                    w = "-" + w;
            }

            return w;
        }

    }
}
