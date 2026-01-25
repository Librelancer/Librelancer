// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Data.Ini;

public class StringKeyValue : ValueBase
{
    public string Key { get; init; }
    public string Value { get; init; }

    public StringKeyValue (string key, string value)
    {
        Key = key;
        Value = value;
    }

    public override bool TryToBoolean(out bool result)
    {
        throw new InvalidCastException();
    }

    public override bool TryToInt32(out int result)
    {
        result = 0;
        return false;
    }

    public override bool TryToInt64(out long result)
    {
        throw new InvalidCastException();
    }

    public override bool TryToSingle(out float result)
    {
        throw new InvalidCastException();
    }

    public override StringKeyValue ToKeyValue()
    {
        return this;
    }

    public override string ToString() => $"{Key} = {Value}";
}
