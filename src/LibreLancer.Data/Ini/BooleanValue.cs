// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Data.Ini;

public class BooleanValue : ValueBase
{
    private readonly bool value;

    public BooleanValue(BinaryReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));
        value = reader.ReadBoolean();
    }

    public BooleanValue(bool value)
    {
        this.value = value;
    }

    public static implicit operator bool(BooleanValue operand)
    {
        if (operand == null) return false;
        else return operand.value;
    }

    public override bool TryToBoolean(out bool result)
    {
        result = value;
        return true;
    }

    public override bool TryToInt32(out int result)
    {
        result = value ? 1 : 0;
        return true;
    }

    public override bool TryToInt64(out long result)
    {
        result = value ? 1 : 0;
        return true;
    }

    public override bool TryToSingle(out float result)
    {
        result = value ? 1 : 0;
        return true;
    }

    public override string ToString()
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}