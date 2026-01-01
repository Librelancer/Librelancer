// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Data.Ini;

public class SingleValue : ValueBase
{
    private float value;
    private long? longvalue;

    public SingleValue(BinaryReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        value = reader.ReadSingle();
    }

    public SingleValue(float value, long? templong)
    {
        longvalue = templong;
        this.value = value;
    }

    public static implicit operator float(SingleValue operand)
    {
        return operand.value;
    }

    public override bool TryToBoolean(out bool result)
    {
        result = value != 0;
        return true;
    }

    public override bool TryToInt32(out int result)
    {
        if (longvalue != null)
        {
            result = unchecked((int)longvalue.Value);
            return true;
        }
        result = (int)value;
        return true;
    }

    public override bool TryToInt64(out long result)
    {
        if (longvalue != null)
        {
            result = longvalue.Value;
            return true;
        }
        result = (int)value;
        return true;
    }

    public override bool TryToSingle(out float result)
    {
        result = value;
        return true;
    }

    public override string ToString()
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}