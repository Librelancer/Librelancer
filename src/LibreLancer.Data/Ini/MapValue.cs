// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Data.Ini;

public class MapValue : ValueBase
{
    public readonly StringKeyValue value;

    public MapValue(string k, string v)
    {
        value = new StringKeyValue(k, v);
    }

    public override bool TryToBoolean(out bool result)
    {
        throw new InvalidCastException();
    }

    public override bool TryToInt32(out int result)
    {
        throw new InvalidCastException();
    }

    public override bool TryToInt64(out long result)
    {
        throw new InvalidCastException();
    }

    public override bool TryToSingle(out float result)
    {
        throw new InvalidCastException ();
    }

    public override string ToString() => value?.ToString() ?? "";

    public override StringKeyValue ToKeyValue()
    {
        return value;
    }
}
