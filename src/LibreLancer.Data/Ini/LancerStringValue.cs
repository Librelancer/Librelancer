// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace LibreLancer.Data.Ini;

public partial class LancerStringValue : StringValue
{
    public LancerStringValue(string value) : base(value)
    {
    }

    public LancerStringValue(BinaryReader reader, BiniStringBlock stringBlock)
        : base(reader, stringBlock)
    {
    }

    [GeneratedRegex(@"^\d+")]
    private static partial Regex FindNumberRegex();

    private static string FindNumber(string value)
    {
        return FindNumberRegex().Match(value).Value;
    }

    public override bool TryToBoolean(out bool result)
    {
        if (bool.TryParse(value, out result)) return true;
        result = value == "1";
        return true;
    }

    public override bool TryToInt32(out int result)
    {
        if (base.TryToInt32(out result)) return true;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
        {
            result = (int)result2;
            return true;
        }
        if (int.TryParse(FindNumber(value), out result)) return true;
        result = 0;
        return false;
    }

    public override bool TryToInt64(out long result)
    {
        if (base.TryToInt64(out result)) return true;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
        {
            result = (int)result2;
            return true;
        }
        if (long.TryParse(FindNumber(value), out result)) return true;
        result = 0;
        return false;
    }

    public override bool TryToSingle(out float result)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return true;
            if (float.TryParse(FindNumber(value), NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return true;
        }
        var lineInfo = Line >= 0 ? ":" + Line : " (line not available)";
        var nameInfo = string.IsNullOrWhiteSpace(Entry?.Name) ? "" : $" for {Entry?.Name}";
        FLLog.Error("Ini",
            $"Failed to parse float '{value}'{nameInfo} in section {Entry?.Section.Name}: {Entry?.Section.File}{lineInfo}");
        result = 0;
        return false;
    }
}