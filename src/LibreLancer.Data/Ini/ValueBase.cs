// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Ini
{
    public abstract class ValueBase : IValue
    {
        public Entry Entry { get; init; }

        public int Line { get; init; } = -1;

        public abstract bool TryToBoolean(out bool result);

        public abstract bool TryToInt32(out int result);

        public abstract bool TryToInt64(out long result);

        public abstract bool TryToSingle(out float result);

        public bool ToBoolean()
        {
            TryToBoolean(out var result);
            return result;
        }

        public int ToInt32()
        {
            TryToInt32(out var result);
            return result;
        }

        public long ToInt64()
        {
            TryToInt64(out var result);
            return result;
        }

        public float ToSingle()
        {
            TryToSingle(out var result);
            return result;
        }

        public virtual StringKeyValue ToKeyValue()
        {
            throw new InvalidCastException();
        }
    }
}
