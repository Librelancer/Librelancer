// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;

namespace LibreLancer.Data.Ini;

public class Int32Value : ValueBase
{
    private readonly int value;
    private readonly bool unsigned;

    public Int32Value(BinaryReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        value = reader.ReadInt32();
    }

    public Int32Value(int value, bool unsigned = false)
    {
        this.value = value;
        this.unsigned = unsigned;
    }

    public static implicit operator int(Int32Value operand)
    {
        if (operand == null) return -1;
        else return operand.value;
    }

    public override bool TryToBoolean(out bool result)
    {
        result = value != 0;
        return true;
    }

    public override bool TryToInt32(out int result)
    {
        result = value;
        return true;
    }

    public override bool TryToInt64(out long result)
    {
        result = value;
        return true;
    }

    public override bool TryToSingle(out float result)
    {
        result = value;
        return true;
    }

    public override string ToString() => unsigned
        ? ((uint)value).ToString(CultureInfo.InvariantCulture)
        : value.ToString(CultureInfo.InvariantCulture);
}