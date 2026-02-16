// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Data.Ini;

public class StringValue : ValueBase
{
    protected readonly string value;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
    public StringValue(BinaryReader reader, BiniStringBlock stringBlock)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        if (stringBlock == null) throw new ArgumentNullException(nameof(stringBlock));
        value = stringBlock.Get(reader.ReadInt32());
    }

    public StringValue(string value)
    {
        this.value = value;
    }

    public static implicit operator string(StringValue operand) => operand.value;
    public override bool TryToBoolean(out bool result)
    {
        if (bool.TryParse(value, out result))
        {
            return true;
        }
        if ("yes".Equals(value, StringComparison.OrdinalIgnoreCase))
            return true;
        if ("no".Equals(value, StringComparison.OrdinalIgnoreCase))
            return false;
        result = !string.IsNullOrEmpty(value);
        return true;
    }

    public override bool TryToInt32(out int result)
    {
        if (int.TryParse(value, out result))
        {
            return true;
        }

        if (uint.TryParse(value, out var result2))
        {
            result = unchecked((int)result2);
            return true;
        }

        result = -1;
        return false;
    }

    public override bool TryToInt64(out long result)
    {
        if (!string.IsNullOrWhiteSpace(value) && long.TryParse(value, out result))
        {
            return true;
        }

        result = -1;
        return false;
    }

    public override bool TryToSingle(out float result)
    {
        if (!string.IsNullOrWhiteSpace(value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        var lineInfo = Line >= 0 ? ":" + Line : " (line not available)";
        var nameInfo = string.IsNullOrWhiteSpace(Entry?.Name) ? "" : $" for {Entry?.Name}";
        FLLog.Error("Ini", $"Failed to parse float '{value}'{nameInfo} in section {Entry?.Section.Name}: {Entry?.Section.File}{lineInfo}");
        result = 0;
        return false;
    }

    public override string ToString()
    {
        return value;
    }
}
